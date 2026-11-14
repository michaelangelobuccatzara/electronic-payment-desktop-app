using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace ElectronicPaymentApp.Services;

public sealed class ReceiptPrintException : Exception
{
    public ReceiptPrintException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Sends a receipt image to a Windows printer (a thermal roll printer such as the KPOS_58).
/// </summary>
public sealed class ReceiptPrinter
{
    // Most 58 mm thermal printers are 203 dpi. Used when the driver doesn't report a resolution.
    private const int FallbackDpi = 203;

    // Extra blank paper after the receipt (1/100 inch) so the cutter doesn't cut through the last line.
    private const int TrailingFeed = 40;

    // Pixels darker than this are printed black, the rest white. Raise it for bolder text, lower it for lighter.
    private const int BlackThreshold = 255;

    private readonly string? _printerName;

    /// <param name="printerName">Windows printer name. Null or empty uses the default printer.</param>
    public ReceiptPrinter(string? printerName = null)
    {
        _printerName = printerName;
    }

    /// <exception cref="ReceiptPrintException">The receipt could not be printed.</exception>
    public Task PrintAsync(byte[] png)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new ReceiptPrintException("Printing is only supported on Windows.");
        }

        // Printing is blocking work, so keep it off the UI thread.
        return Task.Run(() => Print(png));
    }

    [SupportedOSPlatform("windows")]
    private void Print(byte[] png)
    {
        try
        {
            using var stream = new MemoryStream(png);
            using var source = Image.FromStream(stream);
            using var document = new PrintDocument();

            if (!string.IsNullOrWhiteSpace(_printerName))
            {
                document.PrinterSettings.PrinterName = _printerName;
            }

            if (!document.PrinterSettings.IsValid)
            {
                throw new ReceiptPrintException("The receipt printer is not available.");
            }

            document.DocumentName = "Payment Receipt";
            document.PrintController = new StandardPrintController(); // no "Printing..." dialog
            document.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            // Resize the receipt to exactly the printer's dot width, so the driver doesn't have to resample it.
            var settings = document.DefaultPageSettings;
            var resolution = settings.PrinterResolution;
            var dpi = resolution.Kind == PrinterResolutionKind.Custom && resolution.X > 0 ? resolution.X : FallbackDpi;

            var widthPx = Math.Max(1, (int)Math.Round(settings.PrintableArea.Width / 100f * dpi));
            var heightPx = Math.Max(1, (int)Math.Round(source.Height * (widthPx / (float)source.Width)));

            using var receipt = BuildBlackAndWhite(source, widthPx, heightPx);

            // Make the paper as tall as the receipt. If the driver ignores this, the slicing below still prints it all.
            var receiptHeight = heightPx * 100f / dpi;
            document.DefaultPageSettings.PaperSize = new PaperSize(
                "Receipt", settings.PaperSize.Width, (int)Math.Ceiling(receiptHeight) + TrailingFeed);

            var printedPx = 0;

            document.PrintPage += (_, e) =>
            {
                if (e.Graphics is null)
                {
                    return;
                }

                // The graphics origin is the printer's printable area, not the physical page edge.
                var area = e.PageSettings.PrintableArea;
                var pageHeightPx = Math.Max(1, (int)(area.Height / 100f * dpi));
                var slicePx = Math.Min(pageHeightPx, heightPx - printedPx);

                // Draw 1:1 with no smoothing, so every pixel becomes one printer dot.
                e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
                e.Graphics.DrawImage(
                    receipt,
                    new RectangleF(0, 0, widthPx * 100f / dpi, slicePx * 100f / dpi),
                    new RectangleF(0, printedPx, widthPx, slicePx),
                    GraphicsUnit.Pixel);

                printedPx += slicePx;
                e.HasMorePages = printedPx < heightPx;
            };

            document.Print();
        }
        catch (ReceiptPrintException)
        {
            throw;
        }
        catch (Exception ex) when (ex is InvalidPrinterException or Win32Exception or OutOfMemoryException or ArgumentException)
        {
            // OutOfMemory/Argument are what GDI+ throws for data that isn't a valid image.
            throw new ReceiptPrintException("The receipt could not be printed.", ex);
        }
    }

    [SupportedOSPlatform("windows")]
    private static Bitmap BuildBlackAndWhite(Image source, int width, int height)
    {
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);

        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White); // transparent areas of the PNG print as white
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, width, height));
        }

        // Thermal heads only burn or don't burn a dot. Choosing black or white here gives crisp text
        // instead of the driver's grainy gray dithering.
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                var brightness = (pixel.R * 299 + pixel.G * 587 + pixel.B * 114) / 1000;
                bitmap.SetPixel(x, y, brightness < BlackThreshold ? Color.Black : Color.White);
            }
        }

        return bitmap;
    }
}
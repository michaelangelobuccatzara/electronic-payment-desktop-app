using System;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace ElectronicPaymentApp.Services;

/// <summary>
/// Bill acceptor (ESP32) configuration, read from bill-acceptor-port.txt next to the executable.
/// The file is created on first launch so the port can be set per computer without rebuilding.
/// </summary>
public sealed class BillAcceptorSettings
{
    private const string FileName = "bill-acceptor-port.txt";
    private const string FallbackPort = "COM3";

    private BillAcceptorSettings(string portName)
    {
        PortName = portName;
    }

    /// <summary>Serial port the ESP32 is connected to, e.g. COM4.</summary>
    public string PortName { get; }

    public static BillAcceptorSettings Load()
    {
        var path = Path.Combine(GetAppDirectory(), FileName);

        if (!File.Exists(path))
        {
            var detected = GetAvailablePorts();
            var port = detected.Length == 1 ? detected[0] : FallbackPort;
            TryCreateFile(path, port, detected);
            return new BillAcceptorSettings(port);
        }

        // The port is the first line that isn't empty and isn't a comment.
        var configured = File.ReadLines(path)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0 && !line.StartsWith('#'));

        return new BillAcceptorSettings(configured ?? FallbackPort);
    }

    // Folder of the .exe itself. When started with "dotnet app.dll" the process is dotnet.exe, so use the app's base folder.
    private static string GetAppDirectory()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(processPath) &&
            !Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(processPath)!;
        }

        return AppContext.BaseDirectory;
    }

    private static string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    private static void TryCreateFile(string path, string port, string[] detectedPorts)
    {
        var detectedText = detectedPorts.Length > 0 ? string.Join(", ", detectedPorts) : "none found";

        var lines = new[]
        {
            "# Serial port of the bill acceptor (ESP32), for example COM4.",
            "# Change the value below, save, and restart the app.",
            $"# Ports detected on this computer when this file was created: {detectedText}",
            port,
        };

        try
        {
            File.WriteAllLines(path, lines);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The app folder may be read-only (e.g. Program Files). The detected port is still used for this run.
        }
    }
}
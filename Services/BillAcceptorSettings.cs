using System;
using System.IO;
using System.Text.Json;

namespace ElectronicPaymentApp.Services;

/// <summary>
/// Bill acceptor (ESP32) configuration read from appsettings.json.
/// </summary>
public sealed class BillAcceptorSettings
{
    private BillAcceptorSettings(string portName)
    {
        PortName = portName;
    }

    /// <summary>Serial port the ESP32 is connected to, e.g. COM3 or /dev/ttyUSB0.</summary>
    public string PortName { get; }

    public static BillAcceptorSettings Load()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Configuration file not found: {path}");
        }

        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);

        if (document.RootElement.TryGetProperty("BillAcceptor", out var section) &&
            section.TryGetProperty("PortName", out var port) &&
            port.GetString() is { } name &&
            !string.IsNullOrWhiteSpace(name))
        {
            return new BillAcceptorSettings(name);
        }

        throw new InvalidOperationException("appsettings.json must contain BillAcceptor:PortName.");
    }
}
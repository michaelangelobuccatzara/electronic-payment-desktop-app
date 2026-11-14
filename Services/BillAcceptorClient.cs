using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ElectronicPaymentApp.Services;

/// <summary>A bill was accepted. SessionTotal is the ESP32's running total since the last fresh ACCEPTOR,ON.</summary>
public sealed record BillInsertedInfo(int Value, decimal SessionTotal);

/// <summary>A bill acceptor problem. The message is safe to show to the user.</summary>
public sealed class BillAcceptorException : Exception
{
    public BillAcceptorException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Talks to the ESP32 that controls the bill acceptor over a serial port.
/// Protocol (one text line per message):
///   app   -> ESP32: ACCEPTOR,ON | ACCEPTOR,OFF
///   ESP32 -> app  : OK,ACCEPTOR,ON | OK,ACCEPTOR,OFF | BILL,&lt;value&gt;,&lt;total&gt; | ERR,UNKNOWN_PULSES,&lt;n&gt;
/// After ACCEPTOR,OFF the ESP32 waits for a bill that is still going in, so any BILL line
/// arrives before OK,ACCEPTOR,OFF. Events are raised on the UI thread.
/// </summary>
public sealed class BillAcceptorClient : IDisposable
{
    private const int BaudRate = 115200; // matches Serial.begin(115200) in the ESP32 firmware
    private const int EnableAttempts = 4;
    private const int DisableAttempts = 3;
    private const string NotRespondingMessage = "The bill acceptor is not responding. Please contact the staff.";

    private static readonly TimeSpan EnableConfirmTimeout = TimeSpan.FromSeconds(2);

    // The firmware waits about 1.5 s for an in-flight bill before confirming OFF, and every
    // new OFF restarts that wait, so don't resend sooner than this.
    private static readonly TimeSpan DisableConfirmTimeout = TimeSpan.FromSeconds(4);

    private readonly string _portName;
    private readonly object _lock = new();
    private readonly StringBuilder _incomingLine = new();

    private SerialPort? _port;
    private volatile TaskCompletionSource? _enableConfirmation;
    private volatile TaskCompletionSource? _disableConfirmation;

    public BillAcceptorClient(string portName)
    {
        _portName = portName;
    }

    public event EventHandler<BillInsertedInfo>? BillInserted;

    /// <summary>A bill was taken by the acceptor but its value was not recognized (so it was not counted).</summary>
    public event EventHandler? UnrecognizedBill;

    /// <summary>Powers the bill acceptor and waits until the ESP32 confirms it.</summary>
    /// <exception cref="BillAcceptorException">The ESP32 could not be reached or did not confirm.</exception>
    public async Task EnableAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < EnableAttempts; attempt++)
        {
            var confirmed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _enableConfirmation = confirmed;

            Send("ACCEPTOR,ON");

            var winner = await Task.WhenAny(confirmed.Task, Task.Delay(EnableConfirmTimeout, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();

            if (winner == confirmed.Task)
            {
                return;
            }
        }

        throw new BillAcceptorException(NotRespondingMessage);
    }

    /// <summary>
    /// Switches the acceptor off and waits until the ESP32 confirms. Any bill that was still going in
    /// is reported through BillInserted before this method returns.
    /// </summary>
    /// <exception cref="BillAcceptorException">The ESP32 could not be reached or did not confirm.</exception>
    public async Task DisableAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < DisableAttempts; attempt++)
        {
            var confirmed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _disableConfirmation = confirmed;

            Send("ACCEPTOR,OFF");

            var winner = await Task.WhenAny(confirmed.Task, Task.Delay(DisableConfirmTimeout, cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();

            if (winner == confirmed.Task)
            {
                return;
            }
        }

        throw new BillAcceptorException(NotRespondingMessage);
    }

    /// <summary>Sends ACCEPTOR,OFF without waiting for the confirmation (best effort).</summary>
    public void Disable() => TrySend("ACCEPTOR,OFF");

    public void Dispose()
    {
        SerialPort? port;
        lock (_lock)
        {
            port = _port;
            _port = null;
        }

        if (port is null)
        {
            return;
        }

        // Closing the port happens outside the lock: the receive handler also takes the lock,
        // and closing a port can wait for a running handler.
        try
        {
            if (port.IsOpen)
            {
                port.Write("ACCEPTOR,OFF\n");
                port.BaseStream.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException)
        {
            // Best effort while shutting down.
        }

        port.DataReceived -= OnDataReceived;
        port.Dispose();
    }

    private SerialPort EnsureOpen()
    {
        lock (_lock)
        {
            if (_port is { IsOpen: true })
            {
                return _port;
            }

            _port?.Dispose();
            _incomingLine.Clear();

            var port = new SerialPort(_portName, BaudRate)
            {
                Encoding = Encoding.ASCII,
                WriteTimeout = 5000,
                DtrEnable = false, // avoid resetting the ESP32 where the driver allows it
                RtsEnable = false,
            };
            port.DataReceived += OnDataReceived;

            try
            {
                port.Open();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
            {
                port.Dispose();
                throw new BillAcceptorException("The bill acceptor is not available. Please contact the staff.", ex);
            }

            _port = port;
            return port;
        }
    }

    private void Send(string command)
    {
        lock (_lock)
        {
            try
            {
                EnsureOpen().Write(command + "\n");
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or TimeoutException or UnauthorizedAccessException)
            {
                throw new BillAcceptorException(NotRespondingMessage, ex);
            }
        }
    }

    private void TrySend(string command)
    {
        try
        {
            Send(command);
        }
        catch (BillAcceptorException)
        {
            // Best effort: nothing more can be done if the ESP32 is unreachable.
        }
    }

    private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
    {
        if (sender is not SerialPort port)
        {
            return;
        }

        string text;
        try
        {
            text = port.ReadExisting();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            return; // the port was closed while reading
        }

        lock (_lock)
        {
            foreach (var character in text)
            {
                if (character is '\r' or '\n')
                {
                    if (_incomingLine.Length > 0)
                    {
                        var line = _incomingLine.ToString();
                        _incomingLine.Clear();
                        HandleLine(line);
                    }
                }
                else
                {
                    _incomingLine.Append(character);
                }
            }
        }
    }

    private void HandleLine(string line)
    {
        if (line is "OK,ACCEPTOR,ON" or "STATE,ACCEPTOR,ON")
        {
            _enableConfirmation?.TrySetResult();
            return;
        }

        if (line == "OK,ACCEPTOR,OFF")
        {
            _disableConfirmation?.TrySetResult();
            return;
        }

        var parts = line.Split(',');
        switch (parts[0])
        {
            case "BILL" when parts.Length == 3
                             && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                             && decimal.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var runningTotal):
                Raise(() => BillInserted?.Invoke(this, new BillInsertedInfo(value, runningTotal)));
                break;

            case "ERR" when parts.Length >= 2 && parts[1] == "UNKNOWN_PULSES":
                Raise(() => UnrecognizedBill?.Invoke(this, EventArgs.Empty));
                break;
        }
    }

    // Serial data arrives on a background thread; the UI must be updated on the UI thread.
    private static void Raise(Action action) => Dispatcher.UIThread.Post(action);
}
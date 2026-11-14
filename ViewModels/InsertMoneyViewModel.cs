using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ElectronicPaymentApp.Services;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Insert your money into the bill acceptor" page of the Pay Tuition flow.
/// Powers the bill acceptor, shows the running total, and ends the payment when the student
/// stops inserting bills.
/// </summary>
public partial class InsertMoneyViewModel : ViewModelBase
{
    // How long without a new bill (after at least one bill) before the payment is considered finished.
    private static readonly TimeSpan PaymentIdleTimeout = TimeSpan.FromSeconds(30);

    // How long to wait for the first bill before giving up and returning to the main menu.
    private static readonly TimeSpan NoBillTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ErrorDisplayDuration = TimeSpan.FromSeconds(5);

    private readonly BillAcceptorClient _acceptor;

    private decimal _total;
    private long _lastActivityTicks;

    public InsertMoneyViewModel(string studentName, string schoolId, BillAcceptorClient acceptor)
    {
        _acceptor = acceptor;

        StudentNameText = $"Student Name: {studentName}";
        StudentIdText = $"Student ID: {schoolId}";

        _acceptor.BillInserted += OnBillInserted;
        _acceptor.UnrecognizedBill += OnUnrecognizedBill;

        _ = RunPaymentAsync();
    }

    public string StudentNameText { get; }

    public string StudentIdText { get; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = FormatPeso(0m);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;

    /// <summary>Raised when the payment has finished and money was received.</summary>
    public event EventHandler? PaymentCompleted;

    /// <summary>Raised when the page ends without a payment (bill acceptor unavailable, or nothing inserted).</summary>
    public event EventHandler? BackToHomeRequested;

    private async Task RunPaymentAsync()
    {
        try
        {
            await _acceptor.EnableAsync();
        }
        catch (BillAcceptorException ex)
        {
            ErrorMessage = ex.Message;
            _acceptor.Disable();
            Detach();

            await Task.Delay(ErrorDisplayDuration);
            BackToHomeRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        MarkActivity();

        while (!HasTimedOut())
        {
            await Task.Delay(IdleCheckInterval);
        }

        await EndPaymentAsync();
    }

    private async Task EndPaymentAsync()
    {
        try
        {
            // A bill that was still going in is reported (BillInserted) before this returns,
            // so keep listening until the acceptor has confirmed it is off.
            await _acceptor.DisableAsync();
        }
        catch (BillAcceptorException)
        {
            // The acceptor may still be powered, but any money it reported has already been counted.
        }

        Detach();

        if (_total > 0)
        {
            PaymentCompleted?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            BackToHomeRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnBillInserted(object? sender, BillInsertedInfo info)
    {
        _total += info.Value;
        AmountText = FormatPeso(_total);
        ErrorMessage = null;
        MarkActivity();
    }

    private void OnUnrecognizedBill(object? sender, EventArgs e)
    {
        ErrorMessage = "A bill could not be recognized and was not counted. Please contact the staff.";
        MarkActivity();
    }

    private void MarkActivity() => _lastActivityTicks = Environment.TickCount64;

    private bool HasTimedOut()
    {
        var idle = TimeSpan.FromMilliseconds(Environment.TickCount64 - _lastActivityTicks);
        return idle >= (_total > 0 ? PaymentIdleTimeout : NoBillTimeout);
    }

    // Stop listening so this page can't react to events after the user has moved on.
    private void Detach()
    {
        _acceptor.BillInserted -= OnBillInserted;
        _acceptor.UnrecognizedBill -= OnUnrecognizedBill;
    }

    private static string FormatPeso(decimal amount) =>
        "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
}
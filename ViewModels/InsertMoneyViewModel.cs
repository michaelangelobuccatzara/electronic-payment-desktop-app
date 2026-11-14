using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicPaymentApp.Services;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Insert your money into the bill acceptor" page of the Pay Tuition flow.
/// Powers the bill acceptor and shows the running total. The payment is closed either when the
/// student presses Proceed or when they stop inserting bills; the amount is then sent to the backend.
/// </summary>
public partial class InsertMoneyViewModel : ViewModelBase
{
    // How long without a new bill (after at least one bill) before the payment is closed automatically.
    private static readonly TimeSpan PaymentIdleTimeout = TimeSpan.FromSeconds(30);

    // How long to wait for the first bill before giving up and returning to the main menu.
    private static readonly TimeSpan NoBillTimeout = TimeSpan.FromSeconds(60);

    private static readonly TimeSpan IdleCheckInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan ErrorDisplayDuration = TimeSpan.FromSeconds(5);

    private readonly string _rfid;
    private readonly BillAcceptorClient _acceptor;
    private readonly PaymentApiClient _payments;

    private long _lastActivityTicks;
    private bool _collecting = true;   // still waiting for bills; false once the payment is being closed
    private bool _acceptorStopped;

    public InsertMoneyViewModel(
        string studentName,
        string schoolId,
        string rfid,
        BillAcceptorClient acceptor,
        PaymentApiClient payments)
    {
        _rfid = rfid;
        _acceptor = acceptor;
        _payments = payments;

        StudentNameText = $"Student Name: {studentName}";
        StudentIdText = $"Student ID: {schoolId}";

        _acceptor.BillInserted += OnBillInserted;
        _acceptor.UnrecognizedBill += OnUnrecognizedBill;

        _ = RunPaymentAsync();
    }

    public string StudentNameText { get; }

    public string StudentIdText { get; }

    /// <summary>Total of the bills inserted so far.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AmountText))]
    [NotifyPropertyChangedFor(nameof(HasPayment))]
    [NotifyCanExecuteChangedFor(nameof(ProceedCommand))]
    public partial decimal Total { get; set; }

    public string AmountText => FormatPeso(Total);

    /// <summary>True once at least one bill has been inserted (the Proceed button is shown).</summary>
    public bool HasPayment => Total > 0;

    /// <summary>True while the payment is being closed and sent to the backend.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ProceedCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;

    /// <summary>Raised when the payment has been recorded. Carries the id of the payment.</summary>
    public event EventHandler<Guid>? PaymentCompleted;

    /// <summary>Raised when the page ends without a payment (bill acceptor unavailable, or nothing inserted).</summary>
    public event EventHandler? BackToHomeRequested;

    private bool CanProceed() => HasPayment && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanProceed))]
    private Task ProceedAsync() => FinishAsync();

    private async Task RunPaymentAsync()
    {
        try
        {
            await _acceptor.EnableAsync();
        }
        catch (BillAcceptorException ex)
        {
            _collecting = false;
            ErrorMessage = ex.Message;
            _acceptor.Disable();
            Detach();

            await Task.Delay(ErrorDisplayDuration);
            BackToHomeRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        MarkActivity();

        while (_collecting && !HasTimedOut())
        {
            await Task.Delay(IdleCheckInterval);
        }

        if (_collecting)
        {
            await FinishAsync();
        }
    }

    /// <summary>
    /// Closes the payment: switches the acceptor off (so the last bill is counted), then records the
    /// payment on the backend. If recording fails, Proceed can be pressed again to retry.
    /// </summary>
    private async Task FinishAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        _collecting = false;
        ErrorMessage = null;

        try
        {
            if (!_acceptorStopped)
            {
                await StopAcceptorAsync();
            }

            if (Total <= 0)
            {
                BackToHomeRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            var paymentId = await _payments.PayAsync(_rfid, Total);
            PaymentCompleted?.Invoke(this, paymentId);
        }
        catch (ApiException ex)
        {
            ErrorMessage = $"{ex.Message} Press Proceed to try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task StopAcceptorAsync()
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
        _acceptorStopped = true;
    }

    private void OnBillInserted(object? sender, BillInsertedInfo info)
    {
        Total += info.Value;
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
        return idle >= (Total > 0 ? PaymentIdleTimeout : NoBillTimeout);
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
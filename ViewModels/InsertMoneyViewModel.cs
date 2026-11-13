using System;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ElectronicPaymentApp.Services;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Insert your money into the bill acceptor" page of the Pay Tuition flow.
/// Powers the bill acceptor, shows the running total, and finishes when the acceptor reports the session ended.
/// </summary>
public partial class InsertMoneyViewModel : ViewModelBase
{
    private static readonly TimeSpan ErrorDisplayDuration = TimeSpan.FromSeconds(5);

    private readonly BillAcceptorClient _acceptor;

    public InsertMoneyViewModel(string studentName, string schoolId, BillAcceptorClient acceptor)
    {
        _acceptor = acceptor;

        StudentNameText = $"Student Name: {studentName}";
        StudentIdText = $"Student ID: {schoolId}";

        _acceptor.BillInserted += OnBillInserted;
        _acceptor.SessionEnded += OnSessionEnded;
        _acceptor.UnrecognizedBill += OnUnrecognizedBill;

        _ = StartAcceptingAsync();
    }

    public string StudentNameText { get; }

    public string StudentIdText { get; }

    [ObservableProperty]
    public partial string AmountText { get; set; } = FormatPeso(0m);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;

    /// <summary>Raised when the payment has been received.</summary>
    public event EventHandler? PaymentCompleted;

    /// <summary>Raised when the page can't continue (for example the bill acceptor is unavailable).</summary>
    public event EventHandler? BackToHomeRequested;

    private async Task StartAcceptingAsync()
    {
        try
        {
            await _acceptor.EnableAsync();
        }
        catch (BillAcceptorException ex)
        {
            Detach();
            ErrorMessage = ex.Message;

            await Task.Delay(ErrorDisplayDuration);
            BackToHomeRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnBillInserted(object? sender, BillInsertedInfo info)
    {
        AmountText = FormatPeso(info.SessionTotal);
    }

    private void OnUnrecognizedBill(object? sender, EventArgs e)
    {
        ErrorMessage = "A bill could not be recognized and was not counted. Please contact the staff.";
    }

    private void OnSessionEnded(object? sender, SessionEndedInfo info)
    {
        Detach();
        _acceptor.Acknowledge(info.Id);
        _acceptor.Disable();
        PaymentCompleted?.Invoke(this, EventArgs.Empty);
    }

    // Stop listening so this page can't react to events after the user has moved on.
    private void Detach()
    {
        _acceptor.BillInserted -= OnBillInserted;
        _acceptor.SessionEnded -= OnSessionEnded;
        _acceptor.UnrecognizedBill -= OnUnrecognizedBill;
    }

    private static string FormatPeso(decimal amount) =>
        "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
}
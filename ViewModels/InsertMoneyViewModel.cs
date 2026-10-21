using System;
using System.Globalization;
using System.Threading.Tasks;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Insert your money into the bill acceptor" page of the Pay Tuition flow.
/// </summary>
public class InsertMoneyViewModel : ViewModelBase
{
    // TEMPORARY: stands in for the bill acceptor finishing until its integration is known.
    private static readonly TimeSpan SimulatedPaymentDelay = TimeSpan.FromSeconds(5);

    public InsertMoneyViewModel(string studentName, string schoolId, decimal amount)
    {
        StudentNameText = $"Student Name: {studentName}";
        StudentIdText = $"Student ID: {schoolId}";
        AmountText = "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);

        _ = SimulatePaymentAsync();
    }

    public string StudentNameText { get; }

    public string StudentIdText { get; }

    public string AmountText { get; }

    /// <summary>Raised when the payment has been received.</summary>
    public event EventHandler? PaymentCompleted;

    private async Task SimulatePaymentAsync()
    {
        await Task.Delay(SimulatedPaymentDelay);
        PaymentCompleted?.Invoke(this, EventArgs.Empty);
    }
}
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ElectronicPaymentApp.Services;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Payment Successful!" page. Prints the receipt in the background, shows itself for a few
/// seconds, then asks the shell to go home.
/// </summary>
public class PaymentSuccessViewModel : ViewModelBase
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(5);

    private readonly PaymentApiClient _payments;
    private readonly ReceiptPrinter _printer;

    public PaymentSuccessViewModel(Guid paymentId, PaymentApiClient payments, ReceiptPrinter printer)
    {
        PaymentId = paymentId;
        _payments = payments;
        _printer = printer;

        _ = PrintReceiptAsync();
        _ = FinishAfterDelayAsync();
    }

    public Guid PaymentId { get; }

    /// <summary>Raised when the page has been shown long enough.</summary>
    public event EventHandler? Finished;

    private async Task PrintReceiptAsync()
    {
        try
        {
            var png = await _payments.GetReceiptAsync(PaymentId);
            await _printer.PrintAsync(png);
        }
        catch (Exception ex) when (ex is ApiException or ReceiptPrintException)
        {
            // The payment is already recorded; a missing printout must not break the flow.
            Debug.WriteLine($"Receipt for payment {PaymentId} was not printed: {ex.Message}");
        }
    }

    private async Task FinishAfterDelayAsync()
    {
        await Task.Delay(DisplayDuration);
        Finished?.Invoke(this, EventArgs.Empty);
    }
}
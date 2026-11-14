using System;
using System.Threading.Tasks;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Payment Successful!" page. Shows itself for a few seconds, then asks the shell to go home.
/// </summary>
public class PaymentSuccessViewModel : ViewModelBase
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(5);

    public PaymentSuccessViewModel(Guid paymentId)
    {
        PaymentId = paymentId;
        _ = FinishAfterDelayAsync();
    }

    /// <summary>Id of the recorded payment. The receipt image is fetched with it: GET payments/{id}/receipt.png.</summary>
    public Guid PaymentId { get; }

    /// <summary>Raised when the page has been shown long enough.</summary>
    public event EventHandler? Finished;

    private async Task FinishAfterDelayAsync()
    {
        await Task.Delay(DisplayDuration);
        Finished?.Invoke(this, EventArgs.Empty);
    }
}
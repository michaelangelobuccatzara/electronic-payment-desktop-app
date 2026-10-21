using System;
using System.Threading.Tasks;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Payment Successful!" page. Shows itself for a few seconds, then asks the shell to go home.
/// </summary>
public class PaymentSuccessViewModel : ViewModelBase
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(5);

    public PaymentSuccessViewModel()
    {
        _ = FinishAfterDelayAsync();
    }

    /// <summary>Raised when the page has been shown long enough.</summary>
    public event EventHandler? Finished;

    private async Task FinishAfterDelayAsync()
    {
        await Task.Delay(DisplayDuration);
        Finished?.Invoke(this, EventArgs.Empty);
    }
}
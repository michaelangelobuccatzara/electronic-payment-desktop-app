using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>Why the user is being asked to tap their ID (decides where to go after the tap).</summary>
public enum TapPurpose
{
    PayTuition,
    CheckAccount,
}

/// <summary>
/// "Waiting to tap your ID on the reader..." page, shown after a main menu choice.
/// </summary>
public partial class TapIdViewModel : ViewModelBase
{
    // TEMPORARY: stands in for a real ID tap until the card reader integration is known.
    private static readonly TimeSpan SimulatedTapDelay = TimeSpan.FromSeconds(5);

    private readonly CancellationTokenSource _cancellation = new();

    public TapIdViewModel(TapPurpose purpose)
    {
        Purpose = purpose;

        if (purpose == TapPurpose.CheckAccount)
        {
            _ = SimulateTapAsync();
        }
    }

    public TapPurpose Purpose { get; }

    /// <summary>Raised when an ID has been tapped on the reader.</summary>
    public event EventHandler? IdTapped;

    public event EventHandler? BackToHomeRequested;

    [RelayCommand]
    private void BackToHome()
    {
        // Stop waiting so the page doesn't navigate away after the user has already left.
        _cancellation.Cancel();
        BackToHomeRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task SimulateTapAsync()
    {
        try
        {
            await Task.Delay(SimulatedTapDelay, _cancellation.Token);
            IdTapped?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
            // The user pressed Back to Home before the delay finished.
        }
    }
}
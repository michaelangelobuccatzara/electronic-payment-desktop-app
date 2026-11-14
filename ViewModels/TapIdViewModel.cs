using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ElectronicPaymentApp.Models;
using ElectronicPaymentApp.Services;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>Why the user is being asked to tap their ID (decides what happens after the tap).</summary>
public enum TapPurpose
{
    PayTuition,
    CheckAccount,
}

/// <summary>What the Pay Tuition tap hands to the next page: the tapped ID and the student it belongs to.</summary>
public sealed record PaymentTapInfo(string Rfid, StudentAccount Account);

/// <summary>
/// "Waiting to tap your ID on the reader..." page. Receives the tapped ID and,
/// for Check Account, looks the account up through the API.
/// </summary>
public partial class TapIdViewModel : ViewModelBase
{
    private const string WaitingMessage = "Waiting to tap your ID on the reader...";
    private const string CheckingMessage = "Checking your account...";
    private const string NoBalanceMessage = "You have no balance. You cannot perform payment at this moment.";
    private static readonly TimeSpan NoBalanceDisplayDuration = TimeSpan.FromSeconds(5);

    private readonly AccountApiClient _api;
    private readonly CancellationTokenSource _cancellation = new();

    public TapIdViewModel(TapPurpose purpose, AccountApiClient api)
    {
        Purpose = purpose;
        _api = api;
    }

    public TapPurpose Purpose { get; }

    [ObservableProperty]
    public partial string Message { get; set; } = WaitingMessage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => ErrorMessage is not null;

    private bool IsBusy { get; set; }

    /// <summary>Raised when the account for the tapped ID has been loaded (Check Account).</summary>
    public event EventHandler<StudentAccount>? AccountLoaded;

    /// <summary>Raised when an ID has been tapped for a tuition payment (Pay Tuition) by a student with a balance.</summary>
    public event EventHandler<PaymentTapInfo>? PaymentIdTapped;

    public event EventHandler? BackToHomeRequested;

    [RelayCommand]
    private void BackToHome()
    {
        // Stop any lookup in progress so the page doesn't navigate after the user has left.
        _cancellation.Cancel();
        BackToHomeRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called by the view with the ID text sent by the RFID reader.</summary>
    [RelayCommand]
    private async Task SubmitRfidAsync(string? rfid)
    {
        if (IsBusy || string.IsNullOrWhiteSpace(rfid))
        {
            return;
        }

        rfid = rfid.Trim();

        IsBusy = true;
        ErrorMessage = null;
        Message = CheckingMessage;

        try
        {
            var account = await _api.GetAccountAsync(rfid, _cancellation.Token);

            if (Purpose == TapPurpose.CheckAccount)
            {
                AccountLoaded?.Invoke(this, account);
                return;
            }

            // Pay Tuition is only for students who still owe something.
            if (account.Balance <= 0)
            {
                Message = NoBalanceMessage;
                await Task.Delay(NoBalanceDisplayDuration, _cancellation.Token);
                BackToHomeRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            PaymentIdTapped?.Invoke(this, new PaymentTapInfo(rfid, account));
        }
        catch (ApiException ex)
        {
            // Show the problem and keep waiting so the student can tap again.
            ErrorMessage = ex.Message;
            Message = WaitingMessage;
        }
        catch (OperationCanceledException)
        {
            // The user pressed Back to Home while the request (or the 5-second message) was running.
        }
        finally
        {
            IsBusy = false;
        }
    }
}
using System;
using CommunityToolkit.Mvvm.Input;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// Main menu: lets the user choose between paying tuition and checking an account.
/// The view model only reports the user's choice; the shell decides which page to show.
/// </summary>
public partial class MainMenuViewModel : ViewModelBase
{
    public event EventHandler? PayTuitionRequested;

    public event EventHandler? CheckAccountRequested;

    [RelayCommand]
    private void PayTuition() => PayTuitionRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void CheckAccount() => CheckAccountRequested?.Invoke(this, EventArgs.Empty);
}
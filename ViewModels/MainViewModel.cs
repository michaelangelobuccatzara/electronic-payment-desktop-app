using CommunityToolkit.Mvvm.ComponentModel;
using ElectronicPaymentApp.Models;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// Application shell: owns the page that is currently on screen and
/// decides which page comes next when a page reports a user choice.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; } = null!;

    public MainViewModel()
    {
        ShowMainMenu();
    }

    private void ShowMainMenu()
    {
        var menu = new MainMenuViewModel();
        menu.PayTuitionRequested += (_, _) => ShowTapId(TapPurpose.PayTuition);
        menu.CheckAccountRequested += (_, _) => ShowTapId(TapPurpose.CheckAccount);
        CurrentPage = menu;
    }

    private void ShowTapId(TapPurpose purpose)
    {
        var page = new TapIdViewModel(purpose);
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        page.IdTapped += (_, _) => ShowPageAfterTap(purpose);
        CurrentPage = page;
    }

    private void ShowPageAfterTap(TapPurpose purpose)
    {
        switch (purpose)
        {
            case TapPurpose.CheckAccount:
                ShowAccountSummary();
                break;
            case TapPurpose.PayTuition:
                ShowInsertMoney();
                break;
        }
    }

    private void ShowAccountSummary()
    {
        // TODO: replace PlaceholderData with the account returned by the API for the tapped ID.
        var page = new AccountSummaryViewModel(PlaceholderData.Student);
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        page.PayTuitionRequested += (_, _) => ShowTapId(TapPurpose.PayTuition);
        CurrentPage = page;
    }

    private void ShowInsertMoney()
    {
        // TODO: replace PlaceholderData with the tapped student's details and the real inserted amount.
        var page = new InsertMoneyViewModel(
            PlaceholderData.PayerName,
            PlaceholderData.PayerSchoolId,
            PlaceholderData.InsertedAmount);
        page.PaymentCompleted += (_, _) => ShowPaymentSuccess();
        CurrentPage = page;
    }

    private void ShowPaymentSuccess()
    {
        var page = new PaymentSuccessViewModel();
        page.Finished += (_, _) => ShowMainMenu();
        CurrentPage = page;
    }
}
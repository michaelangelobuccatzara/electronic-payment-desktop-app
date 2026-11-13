using CommunityToolkit.Mvvm.ComponentModel;
using ElectronicPaymentApp.Models;
using ElectronicPaymentApp.Services;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// Application shell: owns the page that is currently on screen and
/// decides which page comes next when a page reports a user choice.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly AccountApiClient _api;
    private readonly BillAcceptorClient _billAcceptor;

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; } = null!;

    public MainViewModel(AccountApiClient api, BillAcceptorClient billAcceptor)
    {
        _api = api;
        _billAcceptor = billAcceptor;
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
        var page = new TapIdViewModel(purpose, _api);
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        page.AccountLoaded += (_, account) => ShowAccountSummary(account);
        page.PaymentIdTapped += (_, _) => ShowInsertMoney();
        CurrentPage = page;
    }

    private void ShowAccountSummary(StudentAccount account)
    {
        var page = new AccountSummaryViewModel(account);
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        page.PayTuitionRequested += (_, _) => ShowTapId(TapPurpose.PayTuition);
        CurrentPage = page;
    }

    private void ShowInsertMoney()
    {
        // TODO: replace the placeholder student with the tapped student's real details.
        var page = new InsertMoneyViewModel(
            PlaceholderData.PayerName,
            PlaceholderData.PayerSchoolId,
            _billAcceptor);
        page.PaymentCompleted += (_, _) => ShowPaymentSuccess();
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        CurrentPage = page;
    }

    private void ShowPaymentSuccess()
    {
        var page = new PaymentSuccessViewModel();
        page.Finished += (_, _) => ShowMainMenu();
        CurrentPage = page;
    }
}
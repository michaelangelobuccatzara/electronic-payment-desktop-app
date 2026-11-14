using System;
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
    private readonly PaymentApiClient _payments;
    private readonly BillAcceptorClient _billAcceptor;
    private readonly ReceiptPrinter _printer = new();

    [ObservableProperty]
    public partial ViewModelBase CurrentPage { get; set; } = null!;

    public MainViewModel(AccountApiClient api, PaymentApiClient payments, BillAcceptorClient billAcceptor)
    {
        _api = api;
        _payments = payments;
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
        page.PaymentIdTapped += (_, rfid) => ShowInsertMoney(rfid);
        CurrentPage = page;
    }

    private void ShowAccountSummary(StudentAccount account)
    {
        var page = new AccountSummaryViewModel(account);
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        page.PayTuitionRequested += (_, _) => ShowTapId(TapPurpose.PayTuition);
        CurrentPage = page;
    }

    private void ShowInsertMoney(string rfid)
    {
        // TODO: replace the placeholder student name and ID with the tapped student's real details.
        var page = new InsertMoneyViewModel(
            PlaceholderData.PayerName,
            PlaceholderData.PayerSchoolId,
            rfid,
            _billAcceptor,
            _payments);
        page.PaymentCompleted += (_, paymentId) => ShowPaymentSuccess(paymentId);
        page.BackToHomeRequested += (_, _) => ShowMainMenu();
        CurrentPage = page;
    }

    private void ShowPaymentSuccess(Guid paymentId)
    {
        var page = new PaymentSuccessViewModel(paymentId, _payments, _printer);
        page.Finished += (_, _) => ShowMainMenu();
        CurrentPage = page;
    }
}
using System;
using System.Globalization;
using CommunityToolkit.Mvvm.Input;
using ElectronicPaymentApp.Models;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// Check Account page: shows the student's personal and financial information.
/// </summary>
public partial class AccountSummaryViewModel : ViewModelBase
{
    private readonly StudentAccount _account;

    public AccountSummaryViewModel(StudentAccount account)
    {
        _account = account;
    }

    public string SchoolId => _account.SchoolId;
    public string FirstName => _account.FirstName;
    public string MiddleName => _account.MiddleName;
    public string LastName => _account.LastName;

    public string BalanceText => FormatPeso(_account.Balance);
    public string CreditText => FormatPeso(_account.Credit);
    public string MonthlyPaymentText => FormatPeso(_account.MonthlyPayment);
    public string ApprovedAmountText => FormatPeso(_account.ApprovedAmount);
    public string PromissoryAppliedText => _account.PromissoryApplied ? "Yes" : "No";

    public event EventHandler? BackToHomeRequested;

    public event EventHandler? PayTuitionRequested;

    [RelayCommand]
    private void BackToHome() => BackToHomeRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void PayTuition() => PayTuitionRequested?.Invoke(this, EventArgs.Empty);

    private static string FormatPeso(decimal amount) =>
        "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
}
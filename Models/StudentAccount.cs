namespace ElectronicPaymentApp.Models;

/// <summary>
/// Account details shown on the Check Account page.
/// Will be filled from the API response once the contract is available.
/// </summary>
public sealed record StudentAccount(
    string SchoolId,
    string FirstName,
    string MiddleName,
    string LastName,
    decimal Balance,
    decimal Credit,
    decimal MonthlyPayment,
    bool PromissoryApplied,
    decimal ApprovedAmount);
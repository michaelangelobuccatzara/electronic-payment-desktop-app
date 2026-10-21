namespace ElectronicPaymentApp.Models;

/// <summary>
/// Sample data used until the real API is connected. Delete once the API client exists.
/// </summary>
public static class PlaceholderData
{
    public static StudentAccount Student { get; } = new(
        SchoolId: "2023-01055",
        FirstName: "Bea",
        MiddleName: "Angela",
        LastName: "Soriano",
        Balance: 14200m,
        Credit: 0m,
        MonthlyPayment: 3550m,
        PromissoryApplied: true,
        ApprovedAmount: 5000m);

    // Values shown on the Insert Money page in the design.
    public static string PayerSchoolId => "2024-00123";

    public static decimal InsertedAmount => 500m;

    public static string PayerName => "Juan Dela Cruz";
}
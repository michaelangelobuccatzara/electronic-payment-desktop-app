using System.Globalization;

namespace ElectronicPaymentApp.ViewModels;

/// <summary>
/// "Insert your money into the bill acceptor" page of the Pay Tuition flow.
/// </summary>
public class InsertMoneyViewModel : ViewModelBase
{
    public InsertMoneyViewModel(string studentName, string schoolId, decimal amount)
    {
        StudentNameText = $"Student Name: {studentName}";
        StudentIdText = $"Student ID: {schoolId}";
        AmountText = "₱" + amount.ToString("N2", CultureInfo.InvariantCulture);
    }

    public string StudentNameText { get; }

    public string StudentIdText { get; }

    public string AmountText { get; }
}
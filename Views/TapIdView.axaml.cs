using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using ElectronicPaymentApp.ViewModels;

namespace ElectronicPaymentApp.Views;

public partial class TapIdView : UserControl
{
    // RFID readers usually "type" the card number followed by Enter, so collect it here.
    private readonly StringBuilder _typedText = new();

    public TapIdView()
    {
        InitializeComponent();

        // The view must hold keyboard focus to receive the reader's input.
        Focusable = true;
        Loaded += (_, _) => Focus();
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        if (!string.IsNullOrEmpty(e.Text))
        {
            _typedText.Append(e.Text);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        var rfid = _typedText.ToString();
        _typedText.Clear();

        if (DataContext is TapIdViewModel viewModel)
        {
            viewModel.SubmitRfidCommand.Execute(rfid);
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LogAlligator.App.Controls;

public partial class GrepDialog : Window
{
    public GrepDialog()
    {
        InitializeComponent();
        Activated += (_, _) => GrepTextBox.Focus();
    }

    public void OnEscape()
    {
        Close(null);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(GrepTextBox.Text);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

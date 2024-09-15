using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace LogAlligator.App.Controls;

public partial class BookmarkDialog : Window
{
    public BookmarkDialog()
    {
        InitializeComponent();
        Activated += (_, _) => BookmarkTextBox.Focus();
    }

    public void OnEscape()
    {
        Close(null);
    }

    public void OnOkClick()
    {
        Close(BookmarkTextBox.Text);
    }

    public void OnCancelClick()
    {
        Close(null);
    }
}

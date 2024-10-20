using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Controls;

public partial class EditBookmarkDialog : Window
{
    Bookmark? _bookmarkToEdit = null;
    public EditBookmarkDialog()
    {
        InitializeComponent();
        Activated += (_, _) => BookmarkTextBox.Focus();
    }

    public void Initialize(Bookmark bookmark)
    {
        _bookmarkToEdit = bookmark;
        BookmarkTextBox.Text = bookmark.Name;
        BookmarkTextBox.SelectAll();
    }

    public void OnEscape()
    {
        Close();
    }

    public void OnOkClick()
    {
        if (!string.IsNullOrEmpty(BookmarkTextBox.Text))
        {
            _bookmarkToEdit!.Name = BookmarkTextBox.Text;
        }
        Close();
    }

    public void OnCancelClick()
    {
        Close();
    }
}

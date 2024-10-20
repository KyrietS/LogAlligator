using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Controls;

public partial class EditHighlightDialog : Window
{
    Highlight? _highlightToEdit = null;

    public EditHighlightDialog()
    {
        InitializeComponent();
    }

    public void Initialize(Highlight highlight)
    {
        _highlightToEdit = highlight;
        HighlightPatternTextBox.Text = highlight.Pattern.Pattern.ToString();
    }

    public void OnEscape()
    {
        Close();
    }

    public void OnOkClick()
    {
        if (!string.IsNullOrEmpty(HighlightPatternTextBox.Text))
        {
            _highlightToEdit!.Pattern.Pattern = HighlightPatternTextBox.Text.AsMemory();
        }

        Close();
    }

    public void OnCancelClick()
    {
        Close();
    }
}

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Controls;

public partial class GrepDialog : Window
{
    private bool Regex => RegexCheckBox.IsChecked ?? false;
    private bool CaseSensitive => (!CaseInsensitiveCheckBox.IsChecked) ?? false;
    private bool Inverted => InvertedCheckBox.IsChecked ?? false;

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
        if (string.IsNullOrEmpty(GrepTextBox.Text))
            Close(null);

        SearchPattern pattern = new(GrepTextBox.Text.AsMemory(), caseSensitive: CaseSensitive, regex: Regex);
        Close(pattern);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }
}

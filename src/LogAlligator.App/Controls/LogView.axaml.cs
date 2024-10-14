using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using LogAlligator.App.LineProvider;
using LogAlligator.App.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class LogView : UserControl
{
    private ILineProvider _lineProvider = new EmptyLineProvider();
    private Highlights? _highlights = null;
    private Bookmarks? _bookmarks = null;
    private bool SearchHighlightEnabled => SearchHighlightButton.IsChecked ?? false;

    public LogView()
    {
        this.DataContext = this;
        InitializeComponent();
    }
    
    public void FocusOnSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }
    
    internal void Initialize(ILineProvider lineProvider, Highlights highlights, Bookmarks bookmarks)
    {
        _lineProvider = lineProvider;
        _highlights = highlights;
        _bookmarks = bookmarks;
        TextView.Initialize(lineProvider, highlights, bookmarks);
    }

    internal void AddGrep(string pattern)
    {
        var logView = new LogView();
        var newLineProvider = _lineProvider.Grep(line => line.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        logView.Initialize(newLineProvider, _highlights!, _bookmarks!);
        Tabs.Items.Add(new TabItem { Header = pattern, Content = logView });
    }

    internal LogView? GetSelectedView()
    {
        if (Tabs.SelectedIndex == 0)
        {
            return this;
        }
        else if (Tabs.SelectedIndex > 0 && Tabs.SelectedItem is TabItem { Content: LogView view })
        {
            return view.GetSelectedView();
        }
        return null;
    }

    // TODO: FIXME: This needs A LOT of optimization. It's much slower than it should be.
    private void Search(string searchPhrase, bool down = true)
    {
        var (lineIndex, character) = TextView.CaretPosition ?? (0, 0);

        var numOfLines = _lineProvider.Count;
        var endLineIndex = down ? numOfLines : -1;
        var nextLineDiff = down ? 1 : -1;
        lineIndex += nextLineDiff;

        if (lineIndex < 0 || lineIndex >= _lineProvider.Count)
            return;
        if (searchPhrase.Length == 0)
            return;

        while (lineIndex != endLineIndex)
        {
            if (SearchInLine(lineIndex, searchPhrase))
                return;

            lineIndex += nextLineDiff;
        }

        Log.Debug("Search phrase not found");
        var box = MessageBoxManager
            .GetMessageBoxStandard("Not found", "Search phrase not found", ButtonEnum.Ok,
                Icon.Info, WindowStartupLocation.CenterOwner);
        box.ShowWindowDialogAsync(this.VisualRoot as Window);
    }

    private bool SearchInLine(int lineIndex, string searchPhrase)
    {
        var line = _lineProvider[lineIndex];
        if (!line.Contains(searchPhrase, StringComparison.InvariantCultureIgnoreCase))
            return false;

        int begin = line.IndexOf(searchPhrase, StringComparison.InvariantCultureIgnoreCase);
        Log.Debug("Found \"{searchPhrase}\" at line: {lineIndex}", searchPhrase, lineIndex);
        TextView.ScrollToLine(lineIndex);
        TextView.SelectText(lineIndex, begin, searchPhrase.Length);
        TextView.Refresh();
        return true;
    }

    public void SearchDown()
    {
        Search(SearchBox.Text ?? "", down: true);
    }

    public void SearchUp()
    {
        Search(SearchBox.Text ?? "", down: false);
    }

    private void SearchBoxChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (SearchHighlightEnabled)
        {
            TextView.SearchHighlight = SearchBox.Text;
        }
    }

    private void SearchHighlightButtonPressed(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ToggleButton button = (ToggleButton)sender!;

        if (SearchHighlightEnabled)
        {
            TextView.SearchHighlight = SearchBox.Text;
        }
        else
        {
            TextView.SearchHighlight = null;
        }
    }
}

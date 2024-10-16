using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
    private bool RegexEnabled => RegexButton.IsChecked ?? false;
    private bool CaseSensitiveEnabled => CaseSensitiveButton.IsChecked ?? false;


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

    internal void AddGrep(SearchPattern pattern, bool inverted = false)
    {
        bool ShouldKeepLine(string line) => pattern.Match(line.AsMemory()) != inverted;

        var logView = new LogView();
        var newLineProvider = _lineProvider.Grep(ShouldKeepLine);
        logView.Initialize(newLineProvider, _highlights!, _bookmarks!);
        var tabItem = new TabItem { Header = pattern, Content = logView };
        tabItem.ContextMenu = BuildContextMenu(tabItem);

        Tabs.Items.Add(tabItem);
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
    private void Search(SearchPattern pattern, bool down = true)
    {
        var (lineIndex, _) = TextView.CaretPosition ?? (0, 0);

        var numOfLines = _lineProvider.Count;
        var endLineIndex = down ? numOfLines : -1;
        var nextLineDiff = down ? 1 : -1;
        lineIndex += nextLineDiff;

        if (lineIndex < 0 || lineIndex >= _lineProvider.Count)
            return;
        if (pattern.Pattern.Length == 0)
            return;

        while (lineIndex != endLineIndex)
        {
            if (SearchInLine(lineIndex, pattern))
                return;

            lineIndex += nextLineDiff;
        }

        Log.Debug("Search phrase not found");
        var box = MessageBoxManager
            .GetMessageBoxStandard("Not found", "Search phrase not found", ButtonEnum.Ok,
                Icon.Info, WindowStartupLocation.CenterOwner);
        box.ShowWindowDialogAsync(this.VisualRoot as Window);
    }

    private bool SearchInLine(int lineIndex, SearchPattern pattern)
    {
        // TODO: What about searching in the same line?
        var line = _lineProvider[lineIndex];

        var results = pattern.MatchAll(line.AsMemory());
        if (results.Count == 0)
            return false;

        var (begin, end) = results[0];
        Log.Debug("Found \"{searchPhrase}\" at line: {lineIndex}", pattern.Pattern, lineIndex);
        TextView.ScrollToLine(lineIndex);
        TextView.SelectText(lineIndex, begin, end - begin);
        TextView.Refresh();
        return true;
    }

    public void SearchDown()
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
            return;

        Search(PrepareSearchPattern(), down: true);
    }

    public void SearchUp()
    {
        if (string.IsNullOrEmpty(SearchBox.Text))
            return;

        Search(PrepareSearchPattern(), down: false);
    }

    private SearchPattern PrepareSearchPattern()
    {
        return new SearchPattern(SearchBox.Text.AsMemory(), caseSensitive: CaseSensitiveEnabled, regex: RegexEnabled);
    }

    private void SearchBoxChanged(object? sender, TextChangedEventArgs e)
    {
        if (SearchHighlightEnabled)
        {
            TextView.SearchHighlight = PrepareSearchPattern();
        }
    }

    private void CaseSensitiveButtonClicked(object? sender, RoutedEventArgs e)
    {
        UpdateHighlight();
    }

    private void RegexButtonClicked(object? sender, RoutedEventArgs e)
    {
        UpdateHighlight();
    }

    private void SearchHighlightButtonClicked(object? sender, RoutedEventArgs e)
    {
        UpdateHighlight();
    }

    private void UpdateHighlight()
    {
        TextView.SearchHighlight = SearchHighlightEnabled ? PrepareSearchPattern() : null;
    }

    private ContextMenu BuildContextMenu(TabItem tabItem)
    {
        var delete = new MenuItem { Header = "Close" };
        delete.Click += (_, _) => Tabs.Items.Remove(tabItem);

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(delete);
        return contextMenu;
    }
}

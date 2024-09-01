using System;
using Avalonia.Controls;
using LogAlligator.App.LineProvider;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class LogView : UserControl
{
    private ILineProvider _lineProvider = new EmptyLineProvider();

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
    
    public void SetLineProvider(ILineProvider lineProvider)
    {
        _lineProvider = lineProvider;
        TextView.SetLineProvider(lineProvider);
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
        Log.Debug("Found search phrase at line: {lineIndex}", lineIndex);
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
}

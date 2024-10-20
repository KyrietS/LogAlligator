using System;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Utilities;
using LogAlligator.App.Context;
using LogAlligator.App.LineProvider;
using LogAlligator.App.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls.TextView;

public partial class TextAreaView : UserControl
{
    private ILineProvider _lines = new EmptyLineProvider();
    private int _topLineIndex = 0;
    private int _numberOfLines = 10;
    private double _maxLineWidth = 0;

    private TextSelection _selection = new();
    private bool _selectionOngoing = false;
    private (int Line, int Char)? _caretPosition = null;

    private FileViewContext? _context;
    private SearchPattern? _searchHighlight;

    private static readonly StyledProperty<IBrush> HighlightBackgroundProperty =
    AvaloniaProperty.Register<TextAreaView, IBrush>(nameof(HighlightBackground), new SolidColorBrush(Color.FromRgb(0, 120, 215)));
    public IBrush HighlightBackground
    {
        get => GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    private static readonly StyledProperty<IBrush> HighlightForegroundProperty =
    AvaloniaProperty.Register<TextAreaView, IBrush>(nameof(HighlightForeground), new SolidColorBrush(Colors.Black));
    public IBrush HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public class BookmarkEventArgs : RoutedEventArgs
    {
        public required int LineNumber { get; init; }
        public required string LineText { get; init; }
        public required string SelectedText { get; init; }

        public BookmarkEventArgs() : base(TextAreaView.AddBookmarkEvent)
        {
        }
    }

    public static readonly RoutedEvent<BookmarkEventArgs> AddBookmarkEvent =
        RoutedEvent.Register<TextAreaView, BookmarkEventArgs>(nameof(AddBookmark2), RoutingStrategies.Bubble);

    public event EventHandler<BookmarkEventArgs> AddBookmark2
    {
        add => AddHandler(AddBookmarkEvent, value);
        remove => RemoveHandler(AddBookmarkEvent, value);
    }

    public SearchPattern? SearchHighlight
    {
        set
        {
            _searchHighlight = value;
            Refresh();
        }
    }

    public (int Line, int Char)? CaretPosition
    {
        get => _caretPosition;
        private set
        {
            if (value == null)
            {
                _caretPosition = null;
                return;
            }

            if (value.Value.Line < 0 || value.Value.Line >= _lines.Count)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Caret line position out of range");
            if (value.Value.Char > _lines[value.Value.Line].Length)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Caret char position out of range");

            _caretPosition = value;
        }
    }

    public TextAreaView()
    {
        InitializeComponent();

        this.Focusable = true;
        this.AttachedToVisualTree += (_, _) => Refresh();
        this[!HighlightBackgroundProperty] = new DynamicResourceExtension("HighlightBrush");
        this[!HighlightForegroundProperty] = new DynamicResourceExtension("ThemeBackgroundBrush");


        LineNumbers.NumberOfLines = _numberOfLines;
        LineNumbers.PointerPressed += LineNumbers_PointerPressed;
        LineNumbers.PointerReleased += LineNumbers_PointerReleased;
        LineNumbers.PointerMoved += LineNumbers_PointerMoved;

        TextArea.PointerPressed += TextArea_PointerPressed;
        TextArea.PointerReleased += TextArea_PointerReleased;
        TextArea.PointerMoved += TextArea_PointerMoved;

        WeakEventHandlerManager.Subscribe<Application, EventArgs, TextAreaView>(
            Application.Current!, nameof(Application.ActualThemeVariantChanged), OnActualThemeVariantChanged);

        if (Design.IsDesignMode)
        {
            var designLineProvider = new DesignLineProvider();
            for (int i = 0; i < _numberOfLines; i++)
            {
                designLineProvider.AddLine($"Sample text in line {i + 1}");
            }

            _lines = designLineProvider;
        }
    }

    public void Initialize(ILineProvider lines, FileViewContext context)
    {
        _lines = lines;
        _context = context;
        _topLineIndex = 0;
        LoadData();
    }

    public void GoToLineNumber(int lineNumber)
    {
        int lineIndex = _lines.GetLineIndex(lineNumber);
        if (lineIndex == -1)
            return;

        // TODO: If line is already visible, do not scroll - just set caret position
        ScrollToLine(lineIndex);
        _caretPosition = (lineIndex, 0);
    }

    public void ScrollToLine(int lineIndex)
    {
        VerticalScrollBar.Value = Math.Max(0, lineIndex - TextArea.NumberOfLines / 2);
        UpdateVerticalScroll();
    }

    public void SelectText(int lineIndex, int begin, int length)
    {
        CaretPosition = (lineIndex, begin);
        _selection.SetBegin(lineIndex, begin);
        _selection.SetEnd(lineIndex, begin + length);
    }

    public void Refresh()
    {
        LoadData();
    }

    public async Task CopyToClipboard()
    {
        try
        {
            var selectedText = GetSelectedText();

            if (selectedText.Length > 1_000_000)
            {
                var box = MessageBoxManager
                    .GetMessageBoxStandard("Error",
                        "Selected more than 1 million characters.\nCannot copy that many to clipboard", ButtonEnum.Ok,
                        Icon.Error, WindowStartupLocation.CenterOwner);
                await box.ShowWindowDialogAsync(this.VisualRoot as Window);
                return;
            }

            var window = this.VisualRoot as Window ?? throw new InvalidOperationException("Cannot get window");
            var clipboard = window.Clipboard ?? throw new InvalidOperationException("Cannot get clipboard");

            await clipboard.SetTextAsync(selectedText);
            Log.Debug("Copied selected text to clipboard: {selectedText}", selectedText);
        }
        catch (Exception e)
        {
            Log.Warning("Error when tried to copy selected text to clipboard");
            Log.Warning("Exception: {Exception}", e);
        }
    }

    public void AddBookmark()
    {
        if (_caretPosition is null)
            return;

        int currentLineNumber = _lines.GetLineNumber(_caretPosition.Value.Line);
        string currentLineText = _lines[currentLineNumber];
        string? selectedText = GetSelectedText();

        RaiseEvent(new BookmarkEventArgs { 
            LineNumber = currentLineNumber,
            LineText = currentLineText,
            SelectedText = selectedText
        });
    }


    public string GetSelectedText()
    {
        if (_selection.Start == _selection.Stop)
            return string.Empty;

        StringBuilder sb = new();
        for (int lineIndex = _selection.Start.LineIndex; lineIndex <= _selection.Stop.LineIndex; lineIndex++)
        {
            var line = _lines[lineIndex];

            if (lineIndex == _selection.Start.LineIndex && lineIndex == _selection.Stop.LineIndex)
            {
                sb.Append(line[_selection.Start.CharIndex.._selection.Stop.CharIndex]);
            }
            else if (lineIndex == _selection.Start.LineIndex)
            {
                sb.Append(line[_selection.Start.CharIndex..]);
            }
            else if (lineIndex == _selection.Stop.LineIndex)
            {
                sb.Append(line[.._selection.Stop.CharIndex]);
            }
            else
            {
                sb.Append(line);
            }

            sb.Append(Environment.NewLine);
        }

        sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
        return sb.ToString();
    }

    private void LoadData()
    {
        _numberOfLines = Math.Min(_lines.Count - _topLineIndex, TextArea.NumberOfLinesThatCanFit);
        TextArea.NumberOfLines = _numberOfLines;
        LineNumbers.NumberOfLines = _numberOfLines;

        for (int viewLineIndex = 0; viewLineIndex < _numberOfLines; viewLineIndex++)
        {
            int fileLineIndex = GetFileLineIndex(viewLineIndex);
            var line = _lines[fileLineIndex].AsMemory();

            LineNumbers[viewLineIndex] = _lines.GetLineNumber(fileLineIndex);
            TextArea[viewLineIndex] = line;

            SetBackgroundToLineWithCaret(viewLineIndex);
            SetLineHighlight(viewLineIndex, line);
            SetLineSelection(viewLineIndex, line);
            SetLineSearchHighlight(viewLineIndex, line);

        }
        TextArea.ShapeAllLines();
        _maxLineWidth = Math.Max(_maxLineWidth, TextArea.MaxLineWidth);

        UpdateVerticalScroll();
        UpdateHorizontalScroll();

        LineNumbers.InvalidateVisual();
        TextArea.InvalidateVisual();
    }

    private void SetBackgroundToLineWithCaret(int viewLineIndex)
    {
        // Background of line where the caret is
        if (GetFileLineIndex(viewLineIndex) == _caretPosition?.Line)
        {
            var textAreaFontColor = TextArea.Foreground as SolidColorBrush;
            TextArea.SetLineBackground(viewLineIndex, new SolidColorBrush(textAreaFontColor!.Color, 0.1));
            LineNumbers.SetLineBackground(viewLineIndex, new SolidColorBrush(textAreaFontColor.Color, 0.1));
        }
    }

    private void SetLineHighlight(int viewLineIndex, ReadOnlyMemory<char> line)
    {
        foreach (var highlight in _context!.Highlights ?? [])
        {
            var matches = highlight.Pattern.MatchAll(line);
            foreach (var (matchBegin, matchEnd) in matches)
            {
                TextArea.ApplyStyleToLine(viewLineIndex, (matchBegin..matchEnd),
                    new Style
                    {
                        Foreground = highlight.HasEnoughContrastWith(TextArea.ForegroundColor) ? TextArea.Foreground : TextArea.Background,
                        Background = new SolidColorBrush(highlight.Background),
                        Typeface = new Typeface(TextArea.SecondaryFontFamily)
                    });
            }
        }
    }

    private void SetLineSelection(int viewLineIndex, ReadOnlyMemory<char> line)
    {
        var fileLineIndex = GetFileLineIndex(viewLineIndex);
        if (_selection.GetSelectionAtLine(fileLineIndex) is var (begin, end))
        {
            int selectionBegin = begin ?? 0;
            int selectionEnd = end ?? line.Length;
            TextArea.ApplyStyleToLine(viewLineIndex, (selectionBegin..selectionEnd),
                new Style { Foreground = HighlightForeground, Background = HighlightBackground });
        }
    }

    private void SetLineSearchHighlight(int viewLineIndex, ReadOnlyMemory<char> line)
    {
        if (_searchHighlight != null)
        {
            var matches = _searchHighlight.MatchAll(line);
            foreach (var (searchBegin, searchEnd) in matches)
            {
                TextArea.ApplyStyleToLine(viewLineIndex, (searchBegin..searchEnd),
                    new Style { Border = new Pen(new SolidColorBrush(Colors.GreenYellow), thickness: 1) });
            }
        }
    }


    private int GetFileLineIndex(int viewLineIndex)
    {
        return _topLineIndex + viewLineIndex;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        LoadData();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        if (e.Delta.Y < 0)
        {
            _topLineIndex -= (int)e.Delta.Y * 3;
            _topLineIndex = Math.Min(_topLineIndex, _lines.Count - 1);
            _topLineIndex = Math.Max(_topLineIndex, 0);
        }
        if (e.Delta.Y > 0)
        {
            _topLineIndex -= (int)e.Delta.Y * 3;
            _topLineIndex = Math.Max(_topLineIndex, 0);
        }

        VerticalScrollBar.Value = _topLineIndex;
        HorizontalScrollBar.Value -= e.Delta.X * 10;

        LoadData();
    }

    private void TextArea_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled || _lines.Count == 0)
            return;

        var pointer = e.GetCurrentPoint(TextArea);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            _selectionOngoing = true;
            var cursor = pointer.Position;
            var (lineIndex, charIndex) = TextArea.GetCharIndexAtPosition(cursor);
            lineIndex += _topLineIndex;
            _caretPosition = (lineIndex, charIndex);

            if (e.ClickCount == 1)
                _selection.SetBegin(lineIndex, charIndex);
            else if (e.ClickCount == 2)
                SelectWord(lineIndex, charIndex);
            else if (e.ClickCount == 3)
                SelectLine(lineIndex);

            LoadData();
        }
    }

    private void TextArea_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            _selectionOngoing = false;
        }
    }
    private void TextArea_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Handled)
            return;

        var pointer = e.GetCurrentPoint(TextArea);
        if (pointer.Properties.IsLeftButtonPressed && _selectionOngoing)
        {
            var cursor = pointer.Position;
            var (lineIndex, charIndex) = TextArea.GetCharIndexAtPosition(cursor);
            lineIndex += _topLineIndex;
            _caretPosition = (lineIndex, charIndex);

            _selection.SetEnd(lineIndex, charIndex);
            LoadData();
        }
    }

    private void SelectLine(int lineIndex)
    {
        _selection.SetBeginLine(lineIndex);
    }

    private void SelectWord(int lineIndex, int charIndex)
    {
        lineIndex = Math.Clamp(lineIndex, 0, _lines.Count - 1);
        var line = _lines[lineIndex];

        if (line.Length == 0)
            return;

        charIndex = Math.Clamp(charIndex, 0, line.Length - 1);
        int begin = charIndex;
        int end = charIndex;
        char originChar = line[charIndex];

        while (begin > 0 && IsSameWord(originChar, line[begin - 1]))
            begin--;

        while (end < line.Length - 1 && IsSameWord(originChar, line[end + 1]))
            end++;

        _selection.SetBegin(lineIndex, begin);
        _selection.SetEnd(lineIndex, end + 1);

        bool IsSameWord(char origin, char c)
        {
            return IsWordChar(c) == IsWordChar(origin) &&
                char.IsWhiteSpace(origin) == char.IsWhiteSpace(c);
        }

        bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }

    private void OnVerticalScroll(object sender, ScrollEventArgs args)
    {
        UpdateVerticalScroll();
        LoadData();
    }

    private void OnHorizontalScroll(object sender, ScrollEventArgs args)
    {
        UpdateHorizontalScroll();
    }

    private void UpdateVerticalScroll()
    {
        VerticalScrollBar.Minimum = 0;
        VerticalScrollBar.Maximum = _lines.Count - 1;
        VerticalScrollBar.ViewportSize = _numberOfLines;

        _topLineIndex = (int)VerticalScrollBar.Value;
    }

    private void UpdateHorizontalScroll()
    {
        HorizontalScrollBar.Minimum = 0;
        HorizontalScrollBar.Maximum = _maxLineWidth - TextAreaContainer.Bounds.Width + TextArea.Padding.Left + TextArea.Padding.Right;
        HorizontalScrollBar.ViewportSize = TextAreaContainer.Bounds.Width;
        HorizontalScrollBar.Value = Math.Clamp(HorizontalScrollBar.Value, HorizontalScrollBar.Minimum, HorizontalScrollBar.Maximum);

        double offset = HorizontalScrollBar.Value;
        TextArea.Margin = new Thickness(-offset, TextArea.Margin.Top, TextArea.Margin.Right, TextArea.Margin.Bottom);
    }

    private void LineNumbers_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(LineNumbers);
        if (pointer.Properties.IsLeftButtonPressed && LineNumbers.NumberOfLines > 0)
        {
            _selectionOngoing = true;
            var cursor = pointer.Position;
            int lineNumber = LineNumbers.GetLineNumberAtPosition(cursor);
            int lineIndex = lineNumber - 1;
            _caretPosition = (lineIndex, 0);
            _selection.SetBeginLine(lineIndex);
            LoadData();
            e.Handled = true;
        }
    }

    private void LineNumbers_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            _selectionOngoing = false;
        }
    }

    private void LineNumbers_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pointer = e.GetCurrentPoint(LineNumbers);
        if (pointer.Properties.IsLeftButtonPressed && _selectionOngoing)
        {
            var cursor = pointer.Position;
            int lineNumber = LineNumbers.GetLineNumberAtPosition(cursor);
            int lineIndex = lineNumber - 1;
            _caretPosition = (lineIndex, 0);
            _selection.SetEndLine(lineIndex);
            LoadData();
            e.Handled = true;
        }
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        LoadData();
    }
}

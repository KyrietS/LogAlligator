using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Utilities;
using LogAlligator.App.LineProvider;
using LogAlligator.App.Utils;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class TextView : UserControl
{
    private ILineProvider _lines = new EmptyLineProvider();
    private int _topLineIndex = 0;
    private int _numberOfLines = 10;
    private double _maxLineWidth = 0;

    private TextSelection _selection = new();
    private bool _selectionOngoing = false;
    private (int Line, int Char)? _caretPosition = null;

    private List<string> _highlights = new();

    private static readonly StyledProperty<IBrush> HighlightBackgroundProperty =
    AvaloniaProperty.Register<TextView, IBrush>(nameof(HighlightBackground), new SolidColorBrush(Color.FromRgb(0, 120, 215)));
    public IBrush HighlightBackground
    {
        get => GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    private static readonly StyledProperty<IBrush> HighlightForegroundProperty =
    AvaloniaProperty.Register<TextView, IBrush>(nameof(HighlightForeground), new SolidColorBrush(Colors.Black));
    public IBrush HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public (int Line, int Char)? CaretPosition
    {
        get => _caretPosition;
        set
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

    public TextView()
    {
        InitializeComponent();

        this.Focusable = true;
        this[!HighlightBackgroundProperty] = new DynamicResourceExtension("HighlightBrush");
        this[!HighlightForegroundProperty] = new DynamicResourceExtension("ThemeBackgroundBrush");

        LineNumbers.NumberOfLines = _numberOfLines;
        LineNumbers.PointerPressed += LineNumbers_PointerPressed;
        LineNumbers.PointerReleased += LineNumbers_PointerReleased;
        LineNumbers.PointerMoved += LineNumbers_PointerMoved;

        TextArea.PointerPressed += TextArea_PointerPressed;
        TextArea.PointerReleased += TextArea_PointerReleased;
        TextArea.PointerMoved += TextArea_PointerMoved;

        WeakEventHandlerManager.Subscribe<Application, EventArgs, TextView>(
            Application.Current!, nameof(Application.ActualThemeVariantChanged), OnActualThemeVariantChanged);
        
        if (Design.IsDesignMode)
        {
            var designLineProvider = new DesignLineProvider();
            for (int i = 0; i < _numberOfLines; i++)
            {
                designLineProvider.AddLine($"Sample text in line {i+1}");
            }

            _lines = designLineProvider;
        }
    }

    public void SetLineProvider(ILineProvider lines)
    {
        this._lines = lines;
        _topLineIndex = 0;
        LoadData();
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

    public void AddHighlight(string text)
    {
        _highlights.Add(text);
        Refresh();
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
        
        for (int i = 0; i < _numberOfLines; i++)
        {
            int lineIndex = _topLineIndex + i;
            var line = _lines[lineIndex];

            LineNumbers[i] = lineIndex + 1;
            TextArea[i] = line;

            if (lineIndex == _caretPosition?.Line)
            {
                var textAreaFontColor = TextArea.Foreground as SolidColorBrush;
                TextArea.SetLineBackground(i, new SolidColorBrush(textAreaFontColor!.Color, 0.1));
                LineNumbers.SetLineBackground(i, new SolidColorBrush(textAreaFontColor!.Color, 0.1));
            }

            foreach (var highlight in _highlights)
            {
                int index = 0;
                while (index < line.Length)
                {
                    index = line.IndexOf(highlight, index, StringComparison.OrdinalIgnoreCase);
                    if (index == -1)
                        break;

                    TextArea.AppendFormattingToLine(i, (index..(index + highlight.Length)), background: new SolidColorBrush(Colors.Yellow));
                    index += highlight.Length;
                }
            }

            if (_selection.GetSelectionAtLine(lineIndex) is var (begin, end))
            {
                int selectionBegin = begin ?? 0;
                int selectionEnd = end ?? line.Length;
                TextArea.AppendFormattingToLine(i, (selectionBegin .. selectionEnd), HighlightForeground, HighlightBackground);
            }
        }
        _maxLineWidth = Math.Max(_maxLineWidth, TextArea.MaxLineWidth);

        UpdateVerticalScroll();
        UpdateHorizontalScroll();

        LineNumbers.InvalidateVisual();
        TextArea.InvalidateVisual();
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
            _topLineIndex += 3;
            _topLineIndex = Math.Min(_topLineIndex, _lines.Count - 1);
            _topLineIndex = Math.Max(_topLineIndex, 0);
        }
        if (e.Delta.Y > 0)
        {
            _topLineIndex -= 3;
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
        if (pointer.Properties.IsLeftButtonPressed)
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

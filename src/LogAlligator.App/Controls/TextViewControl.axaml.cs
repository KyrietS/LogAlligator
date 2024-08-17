using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using LogAlligator.App.Utils;
using System;

namespace LogAlligator.App.Controls;

public partial class TextViewControl : UserControl
{
    private string[] _lines = [];
    private int _topLineIndex = 0;
    private int _numberOfLines = 10;
    private double _maxLineWidth = 0;

    private TextSelection _selection = new();
    private bool _selectionOngoing = false;


    public static readonly StyledProperty<IBrush> HighlightBackgroundProperty =
    AvaloniaProperty.Register<TextViewControl, IBrush>(nameof(HighlightBackground), new SolidColorBrush(Color.FromRgb(0, 120, 215)));
    public IBrush HighlightBackground
    {
        get => GetValue(HighlightBackgroundProperty);
        set => SetValue(HighlightBackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush> HighlightForegroundProperty =
    AvaloniaProperty.Register<TextViewControl, IBrush>(nameof(HighlightForeground), new SolidColorBrush(Colors.Black));
    public IBrush HighlightForeground
    {
        get => GetValue(HighlightForegroundProperty);
        set => SetValue(HighlightForegroundProperty, value);
    }

    public TextViewControl()
    {
        InitializeComponent();
        LineNumbers.NumberOfLines = _numberOfLines;
        LineNumbers.PointerPressed += LineNumbers_PointerPressed;
        LineNumbers.PointerReleased += LineNumbers_PointerReleased;
        LineNumbers.PointerMoved += LineNumbers_PointerMoved;

        TextArea.PointerPressed += TextArea_PointerPressed;
        TextArea.PointerReleased += TextArea_PointerReleased;
        TextArea.PointerMoved += TextArea_PointerMoved;

        Application.Current!.ActualThemeVariantChanged += (_, _) => LoadData();

        if (Design.IsDesignMode)
        {
            _lines = new string[_numberOfLines];
            for (int i = 0; i < _numberOfLines; i++)
            {
                _lines[i] = "Sample text in line " + (i+1).ToString();
            }
        }
    }

    public void SetText(string[] lines)
    {
        this._lines = lines;
        _topLineIndex = 0;
        LoadData();
    }

    private void LoadData()
    {
        _numberOfLines = Math.Min(_lines.Length, TextArea.NumberOfLinesThatCanFit);
        TextArea.Clear();
        for (int i = 0; i < _numberOfLines; i++)
        {
            int lineIndex = _topLineIndex + i;

            if (lineIndex >= _lines.Length)
                break;

            TextArea.AppendLine(_lines[lineIndex]);
            if (i == 0)
            {
                TextArea.AppendFormattingToLastLine(7, 4, background: new SolidColorBrush(Colors.GreenYellow));
                TextArea.AppendFormattingToLastLine(5, 3, background: new SolidColorBrush(Colors.Magenta));
                TextArea.AppendFormattingToLastLine(20, 15, background: HighlightBackground, foreground: HighlightForeground);
            }

            if (_selection.GetSelectionAtLine(lineIndex) is var (begin, end))
            {
                int selectionBegin = begin ?? 0;
                int selectionEnd = end ?? _lines[lineIndex].Length;
                int selectionLength = selectionEnd - selectionBegin;
                TextArea.AppendFormattingToLastLine(selectionBegin, selectionLength, HighlightForeground, HighlightBackground);
            }

            _maxLineWidth = Math.Max(_maxLineWidth, TextArea.MaxLineWidth);
        }
        LineNumbers.FirstLineNumber = _topLineIndex + 1;
        LineNumbers.NumberOfLines = _numberOfLines;

        UpdateVerticalScroll();
        UpdateHorizontalScroll();

        LineNumbers.InvalidateVisual();
        TextArea.InvalidateVisual();

        _maxLineWidth = Math.Max(_maxLineWidth, TextArea.MaxLineWidth);
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
            _topLineIndex = Math.Min(_topLineIndex, _lines.Length - 1);
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
        if (e.Handled || _lines.Length == 0)
            return;

        var pointer = e.GetCurrentPoint(TextArea);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            _selectionOngoing = true;
            var cursor = pointer.Position;
            var (lineIndex, charIndex) = TextArea.GetCharIndexAtPosition(cursor);
            lineIndex += _topLineIndex;
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
    private void TextArea_PointerMoved(object? sender,PointerEventArgs e)
    {
        if (e.Handled)
            return;

        var pointer = e.GetCurrentPoint(TextArea);
        if (pointer.Properties.IsLeftButtonPressed && _selectionOngoing)
        {
            var cursor = pointer.Position;
            var (lineIndex, charIndex) = TextArea.GetCharIndexAtPosition(cursor);
            lineIndex += _topLineIndex;
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
        if (_lines[lineIndex].Length == 0)
            return;

        var line = _lines[lineIndex];
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
        VerticalScrollBar.Maximum = _lines.Length - 1;
        VerticalScrollBar.ViewportSize = _numberOfLines;

        _topLineIndex = (int)VerticalScrollBar.Value;
    }

    private void UpdateHorizontalScroll()
    {
        HorizontalScrollBar.Minimum = 0;
        HorizontalScrollBar.Maximum = _maxLineWidth - TextAreaContainer.Bounds.Width + 5;
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
            _selection.SetEndLine(lineIndex);
            LoadData();
            e.Handled = true;
        }
    }
}
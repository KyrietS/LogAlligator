using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using LogAlligator.App.Utils;
using System;
using System.Diagnostics;

namespace LogAlligator.App.Controls;

public partial class TextViewControl : UserControl
{
    private string[] lines = Array.Empty<string>();
    private int topLineIndex = 0;
    private int numberOfLines = 10;
    private double maxLineWidth = 0;

    private TextSelection selection = new();


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
        LineNumbers.NumberOfLines = numberOfLines;
        LineNumbers.PointerPressed += LineNumbers_PointerPressed;
        LineNumbers.PointerMoved += LineNumbers_PointerMoved;

        TextArea.PointerPressed += TextArea_PointerPressed;
        TextArea.PointerMoved += TextArea_PointerMoved;

        Application.Current!.ActualThemeVariantChanged += (_, _) => LoadData();

        if (Design.IsDesignMode)
        {
            lines = new string[numberOfLines];
            for (int i = 0; i < numberOfLines; i++)
            {
                lines[i] = "Sample text in line " + (i+1).ToString();
            }
        }

    }

    public void SetText(string[] lines)
    {
        this.lines = lines;
        topLineIndex = 0;
        LoadData();
    }

    private void LoadData()
    {
        numberOfLines = Math.Min(lines.Length, TextArea.NumberOfLinesThatCanFit);
        TextArea.Clear();
        for (int i = 0; i < numberOfLines; i++)
        {
            int lineIndex = topLineIndex + i;

            if (lineIndex >= lines.Length)
                break;

            TextArea.AppendLine(lines[lineIndex]);
            if (i == 0)
            {
                TextArea.AppendFormattingToLastLine(7, 4, background: new SolidColorBrush(Colors.GreenYellow));
                TextArea.AppendFormattingToLastLine(5, 3, background: new SolidColorBrush(Colors.Magenta));
                TextArea.AppendFormattingToLastLine(20, 15, background: HighlightBackground, foreground: HighlightForeground);
            }

            if (selection.GetSelectionAtLine(lineIndex) is var (begin, end))
            {
                int selectionBegin = begin ?? 0;
                int selectionEnd = end ?? lines[lineIndex].Length;
                int selectionLength = selectionEnd - selectionBegin;
                TextArea.AppendFormattingToLastLine(selectionBegin, selectionLength, HighlightForeground, HighlightBackground);
            }

            maxLineWidth = Math.Max(maxLineWidth, TextArea.MaxLineWidth);
        }
        LineNumbers.FirstLineNumber = topLineIndex + 1;
        LineNumbers.NumberOfLines = numberOfLines;

        UpdateVerticalScroll();
        UpdateHorizontalScroll();

        LineNumbers.InvalidateVisual();
        TextArea.InvalidateVisual();

        maxLineWidth = Math.Max(maxLineWidth, TextArea.MaxLineWidth);
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
            topLineIndex += 3;
            topLineIndex = Math.Min(topLineIndex, lines.Length - 1);
            topLineIndex = Math.Max(topLineIndex, 0);
        }
        if (e.Delta.Y > 0)
        {
            topLineIndex -= 3;
            topLineIndex = Math.Max(topLineIndex, 0);
        }

        VerticalScrollBar.Value = topLineIndex;
        HorizontalScrollBar.Value -= e.Delta.X * 10;

        LoadData();
    }

    private void TextArea_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Handled || lines.Length == 0)
            return;

        var pointer = e.GetCurrentPoint(TextArea);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            var cursor = pointer.Position;
            var (lineIndex, charIndex) = TextArea.GetCharIndexAtPosition(cursor);
            lineIndex += topLineIndex;
            if (e.ClickCount == 1)
                selection.SetBegin(lineIndex, charIndex);
            else if (e.ClickCount == 2)
                SelectWord(lineIndex, charIndex);
            else if (e.ClickCount == 3)
                SelectLine(lineIndex);

            LoadData();
        }
    }

    private void TextArea_PointerMoved(object? sender,PointerEventArgs e)
    {
        if (e.Handled)
            return;

        var pointer = e.GetCurrentPoint(TextArea);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            var cursor = pointer.Position;
            var (lineIndex, charIndex) = TextArea.GetCharIndexAtPosition(cursor);
            lineIndex += topLineIndex;
            selection.SetEnd(lineIndex, charIndex);
            LoadData();
        }
    }

    private void SelectLine(int lineIndex)
    {
        selection.SetBeginLine(lineIndex);
    }

    private void SelectWord(int lineIndex, int charIndex)
    {
        if (lines[lineIndex].Length == 0)
            return;

        var line = lines[lineIndex];
        int begin = charIndex;
        int end = charIndex;
        char originChar = line[charIndex];

        while (begin > 0 && IsSameWord(originChar, line[begin - 1]))
            begin--;

        while (end < line.Length - 1 && IsSameWord(originChar, line[end + 1]))
            end++;

        selection.SetBegin(lineIndex, begin);
        selection.SetEnd(lineIndex, end + 1);

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
        VerticalScrollBar.Maximum = lines.Length - 1;
        VerticalScrollBar.ViewportSize = numberOfLines;

        topLineIndex = (int)VerticalScrollBar.Value;
    }

    private void UpdateHorizontalScroll()
    {
        HorizontalScrollBar.Minimum = 0;
        HorizontalScrollBar.Maximum = maxLineWidth - TextAreaContainer.Bounds.Width + 5;
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
            var cursor = pointer.Position;
            int lineNumber = LineNumbers.GetLineNumberAtPosition(cursor);
            int lineIndex = lineNumber - 1;
            selection.SetBeginLine(lineIndex);
            LoadData();
            e.Handled = true;
        }
    }

    private void LineNumbers_PointerMoved(object? sender, PointerEventArgs e)
    {
        var pointer = e.GetCurrentPoint(LineNumbers);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            var cursor = pointer.Position;
            int lineNumber = LineNumbers.GetLineNumberAtPosition(cursor);
            int lineIndex = lineNumber - 1;
            selection.SetEndLine(lineIndex);
            LoadData();
            e.Handled = true;
        }
    }
}
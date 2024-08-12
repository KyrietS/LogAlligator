using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using System;
using System.Diagnostics;

namespace LogAlligator.Controls;

public partial class TextViewControl : UserControl
{
    private string[] lines = Array.Empty<string>();
    private int topLineIndex = 0;
    private int numberOfLines = 10;
    private double maxLineWidth = 0;

    public TextViewControl()
    {
        InitializeComponent();
        LineNumbers.NumberOfLines = numberOfLines;

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
        Debug.WriteLine(numberOfLines);
        TextArea.Clear();
        for (int i = 0; i < numberOfLines; i++)
        {
            int lineIndex = topLineIndex + i;

            if (lineIndex >= lines.Length)
                break;

            TextArea.AppendLine(lines[lineIndex]);
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

        LoadData();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        lines = new string[1];
        lines[0] = "Mouse clicked!";
        LoadData();
        InvalidateVisual();
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
}
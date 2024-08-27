using System;
using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace LogAlligator.App.Controls;

internal class LineNumbers : Control
{
    public static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<LineNumbers, IBrush>(nameof(Background),
            new SolidColorBrush(Colors.Transparent));

    public IBrush Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<LineNumbers, IBrush>(nameof(Foreground), new SolidColorBrush(Colors.Black));

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<LineNumbers>();
    
    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }
    public FontFamily FontFamily { get; set; } = FontFamily.Default;
    public double FontSize { get; set; }

    public int NumberOfLines
    {
        get => _lineNumbers.Length;
        set
        {
            if (value != _lineNumbers.Length)
            {
                _lineNumbers = new LineNumber[value];
            }
        }
    }

    private LineNumber[] _lineNumbers = new LineNumber[20]; // Some default value

    public int this[int index]
    {
        get => _lineNumbers[index].Number;
        set => _lineNumbers[index] = new LineNumber(value);
    }

    public int GetLineNumberAtPosition(Point position)
    {
        Debug.Assert(_lineNumbers.Length > 0);
        
        int lineIndex = (int)(position.Y / GetLineHeight());
        return GetLineNumber(Math.Clamp(lineIndex, 0, _lineNumbers.Length - 1));
    }

    public void SetLineBackground(int index, IBrush? background)
    {
        _lineNumbers[index].Background = background;
    }
    public override void Render(DrawingContext dc)
    {
        dc.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));

        double lineHeight = GetLineHeight();
        var cursor = new Point(0, 0);

        for (int i = 0; i < NumberOfLines; i++)
        {
            DrawLineNumberBackground(dc, i, cursor);
            
            var lineNumberText = FormatText(GetLineNumber(i).ToString());
            var xOffset = Width - lineNumberText.Width;
            xOffset -= Padding.Right;

            dc.DrawText(lineNumberText, cursor.WithX(xOffset));
            cursor = new Point(cursor.X, cursor.Y + lineHeight);
        }
    }

    private void DrawLineNumberBackground(DrawingContext dc, int index, Point cursor)
    {
        if (_lineNumbers[index].Background is { } background)
        {
            const double widthOverflow = 10; // Draw background wider than the control to cover the gap between line numbers and text area
            dc.FillRectangle(background, new Rect(cursor, new Size(Bounds.Width + widthOverflow, GetLineHeight())));
        }
    }
    
    private int GetLineNumber(Index index)
    {
        return _lineNumbers[index].Number;
    }

    private double GetLineHeight()
    {
        return FormatText(".").Height;
    }

    private FormattedText FormatText(string text)
    {
        var typeface = new Typeface(FontFamily);
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize,
            Foreground);
    }

    record struct LineNumber(int Number = 0, IBrush? Background = null);
}

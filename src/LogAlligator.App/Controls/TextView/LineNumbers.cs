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

    public FontFamily FontFamily { get; set; } = FontFamily.Default;
    public double FontSize { get; set; }
    public int NumberOfLines { get; set; } = 20;
    public int FirstLineNumber { get; set; } = 1;

    public LineNumbers()
    {
        ClipToBounds = true;
        Application.Current!.ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }


    public override void Render(DrawingContext dc)
    {
        dc.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));

        double lineHeight = GetLineHeight();
        var cursor = new Point(0, 0);

        for (int i = 0; i < NumberOfLines; i++)
        {
            var lineNumberText = FormatText(GetLineNumber(i).ToString());
            var xOffset = Width - lineNumberText.Width;

            dc.DrawText(lineNumberText, cursor.WithX(xOffset));
            cursor = new Point(cursor.X, cursor.Y + lineHeight);
        }
    }

    public int GetLineNumberAtPosition(Point position)
    {
        int lineIndex = (int)(position.Y / GetLineHeight());
        return GetLineNumber(lineIndex);
    }

    private int GetLineNumber(int index)
    {
        return FirstLineNumber + index;
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
}

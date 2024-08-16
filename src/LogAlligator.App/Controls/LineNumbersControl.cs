using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace LogAlligator.App.Controls;

class LineNumbersControl : Control
{
    private int? selectedLineIndex = null;

    public static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<LineNumbersControl, IBrush>(nameof(Background), new SolidColorBrush(Colors.Transparent));
    public IBrush Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }        
    
    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<LineNumbersControl, IBrush>(nameof(Foreground), new SolidColorBrush(Colors.Black));
    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public FontFamily FontFamily { get; set; } = FontFamily.Default;
    public double FontSize { get; set; }
    public int NumberOfLines { get; set; } = 20;
    public int FirstLineNumber { get; set; } = 1;

    public LineNumbersControl() : base()
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


    public event EventHandler<(int First, int Last)>? LinesSelected;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this);
        if (pointer.Properties.IsLeftButtonPressed)
        {
            var cursor = pointer.Position;
            int lineIndex = (int)(cursor.Y / GetLineHeight());
            if (lineIndex >= 0 && lineIndex < NumberOfLines)
            {
                int lineNumber = GetLineNumber(lineIndex);
                selectedLineIndex = lineIndex;
                LinesSelected?.Invoke(this, (lineNumber, lineNumber));
                e.Handled = true;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            selectedLineIndex = null;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var pointer = e.GetCurrentPoint(this);
        var cursor = pointer.Position;
        int pointedLineIndex = (int)(cursor.Y / GetLineHeight());
        pointedLineIndex = Math.Clamp(pointedLineIndex, 0, NumberOfLines - 1);
        if (selectedLineIndex != null)
        {
            int selectedLineNumber = GetLineNumber(selectedLineIndex.Value);
            int pointedLineNumber = GetLineNumber(pointedLineIndex);
            int firstSelectedLine = Math.Min(selectedLineNumber, pointedLineNumber);
            int lastSelectedLine = Math.Max(selectedLineNumber, pointedLineNumber);
            LinesSelected?.Invoke(this, (firstSelectedLine, lastSelectedLine));
            e.Handled = true;
        }

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
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize, Foreground);
    }
}

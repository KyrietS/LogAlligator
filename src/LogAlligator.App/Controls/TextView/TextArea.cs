using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Serilog;

namespace LogAlligator.App.Controls;

internal class TextArea : Control
{
    private TextAreaLine[] _lines = [];
    private double _lineHeight = 12;

    public static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<TextArea, IBrush>(nameof(Background), new SolidColorBrush(Colors.Transparent));

    public IBrush Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<TextArea, IBrush>(nameof(Foreground), new SolidColorBrush(Colors.Black));

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextArea, double>(nameof(FontSize), 12);

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<TextArea>();

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextArea, FontFamily>(nameof(FontFamily), FontFamily.Default);

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }
    public FontFamily SecondaryFontFamily { get; set; } = new FontFamily("Courier New");

    public int NumberOfLines
    {
        get => _lines.Length;
        set
        {
            if (value != _lines.Length)
            {
                foreach (var line in _lines)
                    line.Dispose();
                _lines = new TextAreaLine[value];
            }
        }
    }
    public double MaxLineWidth => _lines.Length > 0 ? _lines.Max(l => l.Width) : 0;

    public int NumberOfLinesThatCanFit => (int)Math.Ceiling(Bounds.Height / _lineHeight);

    static TextArea()
    {
        FontSizeProperty.Changed.AddClassHandler<TextArea>((o, _) => o.OnFontSizeChanged());
        FontFamilyProperty.Changed.AddClassHandler<TextArea>((o, _) => o.OnFontFamilyChanged());
    }

    public TextArea()
    {
        _lineHeight = GetInitialLineHeight(FontFamily, FontSize);
    }

    public ReadOnlyMemory<char> this[int index]
    {
        get => _lines[index].Text;
        set
        {
            if (_lines[index] != null)
                _lines[index].Dispose();
            _lines[index] = new TextAreaLine(value, Foreground, this.FontFamily, FontSize);
        }
    }

    public void AppendFormattingToLine(
        int lineIndex, Range range, IBrush? foreground = null, IBrush? background = null, Typeface? typeface = null)
    {
        var line = _lines[lineIndex];
        try
        {
            line.AddFormatting(range, foreground, background, typeface);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning(ex, "Applied invalid range to formatting line: {Range}", range);
        }
    }

    public void SetLineBackground(int lineIndex, IBrush? background)
    {
        _lines[lineIndex].Background = background;
    }
    
    /// <summary>
    /// Gets position of the character in the text pointed by <paramref name="position"/> (usually mouse cursor)
    /// </summary>
    /// <param name="position">Position (X,Y) relative to <see cref="TextArea"/>.</param>
    /// <returns>
    /// <para>A tuple containing the index of pointed line and index of pointer character in that line.</para>
    /// <para>If <paramref name="position"/> points above the first line, then <c>(0, 0)</c> is returned.</para>
    /// <para>If <paramref name="position"/> points below the last line, then <c>(lastL, afterLastC)</c> is returned,
    /// <para>If <paramref name="position"/> points before first character of the line, then <c>(L, 0)</c> is returned.</para>
    /// <para>If <paramref name="position"/> points after the last character of the line, then <c>(L, afterLastC)</c> is returned,
    /// where <c>lastL</c> is the index of the last line,
    /// where <c>afterLastC</c> is the index of the last character of the line + 1.</para>
    /// </returns>
    /// <remarks>
    /// Given a <paramref name="position"/> this function finds a character <b>before</b> which a caret should be placed. 
    /// Let's take two letters: <c>AB</c>. Each letter is split in half. Now, if you point at:
    /// <list type="bullet">
    /// <item>Left side of <c>A</c> or before <c>A</c>, the index <c>0</c> will be returned</item>
    /// <item>Right side of <c>A</c> or left side of <c>B</c>, the index <c>1</c> will be returned</item>
    /// <item>Right side of <c>B</c> or after <c>B</c>, the index <c>3</c> will be returned</item>
    /// </list>
    /// </remarks>
    public (int LineIndex, int CharIndex) GetCharIndexAtPosition(Point position)
    {
        var lineIndex = (int)(position.Y / _lineHeight);

        if (lineIndex < 0 || _lines.Length == 0)
            return (0, 0);
        if (lineIndex >= _lines.Length)
            return (_lines.Length - 1, _lines.Last().Text.Length);

        var line = _lines[lineIndex];
        double cursor = Padding.Left;
        var charIndex = 0;
        double previousWidth = 0;

        do
        {
            charIndex++;

            // When clicked after last character, return index after last character
            if (charIndex > line.Text.Length)
            {
                return (lineIndex, line.Text.Length);
            }

            var textWidth = line.GetWidthSpan(length: charIndex);
            var letterWidth = textWidth - previousWidth;
            cursor = Padding.Left + textWidth - letterWidth / 2;
            previousWidth = textWidth;
        } while (position.X > cursor);

        return (lineIndex, charIndex - 1);
    }

    public void ShapeLine(int lineIndex)
    {
        _lines[lineIndex].Shape();
    }
    public void ShapeAllLines()
    {
        foreach (var line in _lines)
        {
            line.Shape();
        }
    }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        dc.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));

        if (_lines.Length > 0)
            RenderAllLines(dc);
    }

    private void RenderAllLines(DrawingContext dc)
    {
        var cursor = new Point(Padding.Left, Padding.Top);
        foreach(var line in _lines)
        {
            RenderLine(dc, line, cursor);
            cursor = new Point(cursor.X, cursor.Y + line.Height);
        }
    }

    private void RenderLine(DrawingContext dc, TextAreaLine line, Point cursor)
    {
        RenderLineBackground(dc, cursor, line);
        using var _ = ClipTextAreaToPadding(dc);
        line.Draw(dc, cursor);
    }

    private void RenderLineBackground(DrawingContext dc, Point cursor, TextAreaLine line)
    {
        if (line.Background == null)
            return;

        dc.FillRectangle(line.Background, new Rect(0, cursor.Y, Bounds.Width, line.Height));
    }

    private IDisposable ClipTextAreaToPadding(DrawingContext dc)
    {
        Rect clipRect = new(
            Padding.Left, Padding.Top,
            Bounds.Width - Padding.Left - Padding.Right,
            Bounds.Height - Padding.Top - Padding.Bottom);
        return dc.PushClip(clipRect);
    }
    private void OnFontSizeChanged()
    {
        _lineHeight = GetInitialLineHeight(FontFamily, FontSize);
        InvalidateVisual();
    }

    private void OnFontFamilyChanged()
    {
        _lineHeight = GetInitialLineHeight(FontFamily, FontSize);
        InvalidateVisual();
    }

    private double GetInitialLineHeight(FontFamily fontFamily, double fontSize)
    {
        using TextAreaLine dummyLine = new("A".AsMemory(), Brushes.Black, fontFamily, fontSize);
        dummyLine.Shape();
        return dummyLine.Height;
    }
}

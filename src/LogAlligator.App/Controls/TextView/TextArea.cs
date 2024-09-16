using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LogAlligator.App.Utils;
using Serilog;

namespace LogAlligator.App.Controls;

internal class TextArea : Control
{
    private Line[] _lines = [];

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
    
    public FontFamily FontFamily { get; set; } = FontFamily.Default;

    public int NumberOfLines
    {
        get => _lines.Length;
        set
        {
            if (value != _lines.Length)
            {
                _lines = new Line[value];
            }
        }
    }
    public double MaxLineWidth { get; private set; }
    public int NumberOfLinesThatCanFit => (int)Math.Ceiling(Bounds.Height / GetLineHeight());

    static TextArea()
    {
        FontSizeProperty.Changed.AddClassHandler<TextArea>((o, _) => o.InvalidateVisual());
    }

    public string this[int index]
    {
        get => _lines[index].Text;
        set
        {
            _lines[index] = new Line(value);
            MaxLineWidth = Math.Max(MaxLineWidth, GetLineWidth(value));
        }
    }

    public void AppendFormattingToLine(int lineIndex, Range range, IBrush? foreground = null, IBrush? background = null)
    {
        Debug.Assert(_lines.Length > 0);

        var line = _lines[lineIndex];
        try
        {
            var (begin, length) = range.GetOffsetAndLength(line.Text.Length);
            
            if (length > 0)
                line.AddFormatting(begin, length, foreground, background);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Log.Warning(ex, "Applied invalid range to formatting line: {Range}", range);
        }
    }

    public void SetLineBackground(int lineIndex, IBrush? background)
    {
        Debug.Assert(_lines.Length > 0);
        _lines[lineIndex].Background = background;
    }
    
    /// <summary>
    /// Gets position of the character in the text pointed by <paramref name="position"/> (usually mouse cursor)
    /// </summary>
    /// <param name="position">Position (X,Y) relative to <see cref="TextArea"/>.</param>
    /// <returns>
    /// <para>A tuple containing the index of pointed line and index of pointer character in that line.</para>
    /// <para>If <paramref name="position"/> points above the first line, then <c>(0, 0)</c> is returned.</para>
    /// <para>If <paramref name="position"/> points below the last line, then <c>(lastL, lastC)</c> is returned,
    /// where <c>lastL</c> is the index of the last line and <c>lastC</c> is the index of the last character of this line.</para>
    /// <para>If <paramref name="position"/> points before first character of the line, then <c>(L, 0)</c> is returned.</para>
    /// <para>If <paramref name="position"/> points after the last character of the line, then <c>(L, lastC)</c> is returned,
    /// where <c>lastC</c> is the index of the last character of this line.</para>
    /// </returns>
    /// <remarks>
    /// Given a <paramref name="position"/> this function finds a character before which a caret should be placed. 
    /// Let's take two letters: <c>AB</c>. Each letter is split in half. Now, if you point at:
    /// <list type="bullet">
    /// <item>Left side of <c>A</c> or before <c>A</c>, the index <c>0</c> will be returned</item>
    /// <item>Right side of <c>A</c> or left side of <c>B</c>, the index <c>1</c> will be returned</item>
    /// <item>Right side of <c>B</c> or after <c>B</c>, the index <c>3</c> will be returned</item>
    /// </list>
    /// </remarks>
    public (int LineIndex, int CharIndex) GetCharIndexAtPosition(Point position)
    {
        var lineIndex = (int)(position.Y / GetLineHeight());

        if (lineIndex < 0 || _lines.Length == 0)
            return (0, 0);
        if (lineIndex >= _lines.Length)
            return (_lines.Length - 1, _lines.Last().Text.Length);

        var line = _lines[lineIndex].Text;
        double cursor = Padding.Left;
        var charIndex = 0;
        double previousWidth = 0;

        do
        {
            charIndex++;

            // When clicked after last character, return index after last character
            if (charIndex > line.Length)
            {
                return (lineIndex, line.Length);
            }

            var textFragment = line.Substring(0, charIndex);
            var textWidth = GetLineWidth(textFragment);
            var letterWidth = textWidth - previousWidth;
            cursor = Padding.Left + textWidth - letterWidth / 2;
            previousWidth = textWidth;
        } while (position.X > cursor);

        return (lineIndex, charIndex - 1);
    }

    public override void Render(DrawingContext dc)
    {
        base.Render(dc);

        dc.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));

        RenderAllLines(dc);
    }

    private void RenderAllLines(DrawingContext dc)
    {
        var lineHeight = GetLineHeight();
        var cursor = new Point(Padding.Left, Padding.Top);
        for (int lineIndex = 0; lineIndex < _lines.Length; lineIndex++)
        {
            RenderLine(dc, lineIndex, cursor);
            cursor = new Point(cursor.X, cursor.Y + lineHeight);
        }
    }

    private void RenderLine(DrawingContext dc, int lineIndex, Point cursor)
    {
        var line = _lines[lineIndex];
        RenderLineBackground(dc, cursor, line.Background);
        using var _ = ClipTextAreaToPadding(dc);
        
        for (int formattingIndex = 0; formattingIndex < line.Formattings.Count; formattingIndex++)
        {
            var (text, formatting) = line.GetTextSpan(formattingIndex);
            double width = RenderTextWithFormatting(dc, text, formatting, cursor);
            cursor = cursor.WithX(cursor.X + width);
        }
    }

    private IDisposable ClipTextAreaToPadding(DrawingContext dc)
    {
        Rect clipRect = new(
            Padding.Left, Padding.Top,
            Bounds.Width - Padding.Left - Padding.Right,
            Bounds.Height - Padding.Top - Padding.Bottom);
        return dc.PushClip(clipRect);
    }
    private double RenderTextWithFormatting(DrawingContext dc, string text, Line.Formatting formatting, Point cursor)
    {
        var formattedText = FormatText(text);
        double textWidth = formattedText.WidthIncludingTrailingWhitespace;

        if (formatting.Foreground != null)
            formattedText.SetForegroundBrush(formatting.Foreground);
        if (formatting.Background != null) // TODO: Instead of 0.5px padding try snapping the Rect to the middle of px
            dc.FillRectangle(formatting.Background,
                new Rect(cursor - new Point(0.5, 0.5), new Size(textWidth + 1, formattedText.Height + 1)));

        dc.DrawText(formattedText, cursor);

        return textWidth;
    }

    private void RenderLineBackground(DrawingContext dc, Point cursor, IBrush? background)
    {
        if (background == null)
            return;
        
        dc.FillRectangle(background, new Rect(0, cursor.Y, Bounds.Width, GetLineHeight()));
    }

    private double GetLineWidth(string line)
    {
        return FormatText(line).WidthIncludingTrailingWhitespace;
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

    private struct Line
    {
        public readonly string Text;
        public readonly OverlappingRanges<Formatting> Formattings = new();
        public IBrush? Background { get; set; }
        
        public Line(string text)
        {
            Text = text;
            Formattings.AddRange(0, Text.Length, Formatting.Default);
        }

        public (string, Formatting) GetTextSpan(int formattingIndex)
        {
            Debug.Assert(formattingIndex >= 0 && formattingIndex < Formattings.Count);

            var formatting = Formattings.GetRangeAtIndex(formattingIndex);
            var textSpan = Text.Substring(formatting.Begin, formatting.End - formatting.Begin);

            return (textSpan, formatting.Value);
        }

        public void AddFormatting(int begin, int length, IBrush? foreground = null, IBrush? background = null)
        {
            AddFormatting(begin, length, new Formatting() { Foreground = foreground, Background = background });
        }

        public void AddFormatting(int begin, int length, Formatting formatting)
        {
            Debug.Assert(begin >= 0 && begin < Text.Length);
            Debug.Assert(length >= 0);

            if (length == 0)
                return;

            int end = begin + length;
            Formattings.AddRange(begin, end, formatting);
        }

        public struct Formatting
        {
            public static readonly Formatting Default = new Formatting();

            public IBrush? Foreground;
            public IBrush? Background;
        }
    }
}

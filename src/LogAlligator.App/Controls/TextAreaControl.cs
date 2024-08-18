using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Controls;

internal class TextAreaControl : Control
{
    private readonly List<Line> _lines = [];

    public static readonly StyledProperty<IBrush> BackgroundProperty =
        AvaloniaProperty.Register<TextAreaControl, IBrush>(nameof(Background), new SolidColorBrush(Colors.Transparent));

    public IBrush Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<IBrush> ForegroundProperty =
        AvaloniaProperty.Register<TextAreaControl, IBrush>(nameof(Foreground), new SolidColorBrush(Colors.Black));

    public IBrush Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextAreaControl, double>(nameof(FontSize), 12);

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public FontFamily FontFamily { get; set; } = FontFamily.Default;

    public double MaxLineWidth { get; private set; }
    public int NumberOfLinesThatCanFit => (int)Math.Ceiling(Bounds.Height / GetLineHeight());

    public TextAreaControl()
    {
        ClipToBounds = true;
        Application.Current!.ActualThemeVariantChanged += (_, _) => InvalidateVisual();

        if (Design.IsDesignMode)
        {
            _lines.Add(new Line("Sample text"));
        }
    }

    static TextAreaControl()
    {
        FontSizeProperty.Changed.AddClassHandler<TextAreaControl>((o, _) => o.InvalidateVisual());
    }

    public void AppendLine(string line)
    {
        _lines.Add(new Line(line));
        MaxLineWidth = Math.Max(MaxLineWidth, GetLineWidth(line));
    }

    public void AppendFormattingToLastLine(int charIndex, int length, IBrush? foreground = null,
        IBrush? background = null)
    {
        Debug.Assert(_lines.Count > 0);

        var line = _lines.Last();
        length = Math.Min(length, line.Text.Length - charIndex);
        if (length <= 0)
            return;

        line.AddFormatting(charIndex, length, foreground, background);
    }

    /// <summary>
    /// Gets position of the character in the text pointed by <paramref name="position"/> (usually mouse cursor)
    /// </summary>
    /// <param name="position">Position (X,Y) relative to <see cref="TextAreaControl"/>.</param>
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

        if (lineIndex < 0 || _lines.Count == 0)
            return (0, 0);
        if (lineIndex >= _lines.Count)
            return (_lines.Count - 1, _lines.Last().Text.Length);

        var line = _lines[lineIndex].Text;
        double cursor = 0;
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
            cursor = textWidth - letterWidth / 2;
            previousWidth = textWidth;
        } while (position.X > cursor);

        return (lineIndex, charIndex - 1);
    }

    public void Clear()
    {
        _lines.Clear();
        MaxLineWidth = 0;
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
        var cursor = new Point(0, 0);
        for (int lineIndex = 0; lineIndex < _lines.Count; lineIndex++)
        {
            RenderLine(dc, lineIndex, cursor);
            cursor = new Point(cursor.X, cursor.Y + lineHeight);
        }
    }

    private void RenderLine(DrawingContext dc, int lineIndex, Point cursor)
    {
        var line = _lines[lineIndex];
        for (int formattingIndex = 0; formattingIndex < line.Formattings.Count; formattingIndex++)
        {
            var (text, formatting) = line.GetTextSpan(formattingIndex);
            double width = RenderTextWithFormatting(dc, text, formatting, cursor);
            cursor = cursor.WithX(cursor.X + width);
        }
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

    private readonly struct Line
    {
        public readonly string Text;
        public readonly RangeList<Formatting> Formattings = new();

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

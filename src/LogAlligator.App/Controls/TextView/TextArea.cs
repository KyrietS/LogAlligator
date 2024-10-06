using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using LogAlligator.App.Utils.Ranges;
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
    public FontFamily SecondaryFontFamily { get; set; } = new FontFamily("Courier New");

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
            var line = new Line(value);
            _lines[index] = line;
            MaxLineWidth = Math.Max(MaxLineWidth, GetLineWidth(line));
        }
    }

    public void AppendFormattingToLine(
        int lineIndex, Range range, IBrush? foreground = null, IBrush? background = null, FontFamily? font = null)
    {
        Debug.Assert(_lines.Length > 0);

        var line = _lines[lineIndex];
        try
        {
            var (begin, length) = range.GetOffsetAndLength(line.Text.Length);
            
            if (length > 0)
                line.AddFormatting(begin, length, foreground, background, font);
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
        var lineIndex = (int)(position.Y / GetLineHeight());

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

            var textWidth = GetLineWidth(line, length: charIndex);
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
        
        foreach (var (text, formatting) in line)
        {
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

        if (formatting.Foreground != null)
            formattedText.SetForegroundBrush(formatting.Foreground);
        if (formatting.Typeface != null)
            formattedText.SetFontTypeface(formatting.Typeface.Value);

        double textWidth = formattedText.WidthIncludingTrailingWhitespace;

        if (formatting.Background != null) // TODO: Instead of 0.5px padding try snapping the Rect to the middle of px
            dc.FillRectangle(formatting.Background,
                new Rect(cursor - new Point(0.25, 0.25), new Size(textWidth + 0.5, formattedText.Height + 0.5)));


        dc.DrawText(formattedText, cursor);

        return textWidth;
    }

    private void RenderLineBackground(DrawingContext dc, Point cursor, IBrush? background)
    {
        if (background == null)
            return;
        
        dc.FillRectangle(background, new Rect(0, cursor.Y, Bounds.Width, GetLineHeight()));
    }

    private double GetLineWidth(Line line, int length = -1)
    {
        Debug.Assert(length <= line.Text.Length);

        if (length < 0)
            length = line.Text.Length;

        double width = 0;
        int cursor = 0;
        foreach(var (text, formatting) in line)
        {
            int currCursor = Math.Min(cursor + text.Length, length);
            string textFragment = text.Substring(0, currCursor - cursor);
            width += FormatText(textFragment, formatting.Typeface).WidthIncludingTrailingWhitespace;
            cursor = currCursor;

            if (cursor >= length)
                break;
        }

        return width;
    }

    private double GetLineHeight()
    {
        return FormatText(".").Height;
    }

    private FormattedText FormatText(string text, Typeface? typeface = null)
    {
        typeface ??= new Typeface(FontFamily);
        return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface.Value, FontSize,
            Foreground);
    }

    private struct Line : IEnumerable<(string, Line.Formatting)>
    {
        public readonly string Text;

        private readonly OverlappingRanges<IBrush?> _foregrounds = new();
        private readonly OverlappingRanges<IBrush?> _backgrounds = new();
        private readonly OverlappingRanges<Typeface?> _typefaces = new();
        private Ranges<(IBrush? Foreground, IBrush? Background, Typeface? Typeface)>? _formattings = null;

        public IBrush? Background { get; set; }
        
        public Line(string text)
        {
            Text = text;

            _foregrounds.AddRange(0, Text.Length, Formatting.Default.Foreground);
            _backgrounds.AddRange(0, Text.Length, Formatting.Default.Background);
            _typefaces.AddRange(0, Text.Length, Formatting.Default.Typeface);
        }

        public void AddFormatting(int begin, int length, IBrush? foreground = null, IBrush? background = null, FontFamily? font = null)
        {
            Typeface? typeface = font != null ? new Typeface(font) : null;
            AddFormatting(begin, length, new Formatting() { Foreground = foreground, Background = background, Typeface = typeface });
        }

        public void AddFormatting(int begin, int length, Formatting formatting)
        {
            Debug.Assert(begin >= 0 && begin < Text.Length);
            Debug.Assert(length >= 0);

            if (length == 0)
                return;

            int end = begin + length;

            if (formatting.Foreground != null)
                _foregrounds.AddRange(begin, end, formatting.Foreground);
            if (formatting.Background != null)
                _backgrounds.AddRange(begin, end, formatting.Background);
            if (formatting.Typeface != null)
                _typefaces.AddRange(begin, end, formatting.Typeface.Value);

            _formattings = null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<(string, Formatting)> GetEnumerator()
        {
            MergeFormattings();
            return new LineEnumerator(Text, _formattings!);
        }

        private void MergeFormattings()
        {
            if (_formattings != null)
                return;

            _formattings = RangesMerger.Merge(_foregrounds, _backgrounds, _typefaces);
        }

        public struct Formatting
        {
            public static readonly Formatting Default = new Formatting() { };

            public IBrush? Foreground;
            public IBrush? Background;
            public Typeface? Typeface;
        }

        private class LineEnumerator(string _text, Ranges<(IBrush? Foreground, IBrush? Background, Typeface? Font)> mergedFormattings)
            : IEnumerator<(string Text, Formatting Value)>
        {
            private int _index = -1;

            public (string Text, Line.Formatting Value) Current
            {
                get
                {
                    var (begin, end, (foreground, background, typeface)) = mergedFormattings.GetRangeAtIndex(_index);
                    var textSpan = _text.Substring(begin, end - begin);
                    return (textSpan, new Line.Formatting() { Foreground = foreground, Background = background, Typeface = typeface });
                }
            }
                

            object IEnumerator.Current => Current;

            public bool MoveNext() => ++_index < mergedFormattings.Count;

            public void Reset() { _index = -1; }

            public void Dispose() { }
        }
    }
}

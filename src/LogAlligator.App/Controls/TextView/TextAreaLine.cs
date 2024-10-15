using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using LogAlligator.App.Utils.Ranges;

namespace LogAlligator.App.Controls.TextView;

class ReadOnlyListSlice<T> : IReadOnlyList<T>
{
    private readonly IReadOnlyList<T> _list;
    private readonly int _start;
    private readonly int _length;

    public ReadOnlyListSlice(IReadOnlyList<T> list, int start = -1, int length = -1)
    {
        start = start == -1 ? 0 : start;
        length = length == -1 ? list.Count - start : length;

        Debug.Assert(start >= 0);
        Debug.Assert(length >= 0);
        Debug.Assert(start + length <= list.Count);

        _list = list;
        _start = start;
        _length = length;
    }

    public ReadOnlyListSlice<T> Slice(int start, int length)
    {
        return new ReadOnlyListSlice<T>(_list, _start + start, length);
    }

    public T this[int index] => _list[_start + index];

    public int Count => _length;

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _length; i++)
            yield return _list[_start + i];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

class TextAreaLine : IDisposable
{
    public ReadOnlyMemory<char> Text;

    private readonly OverlappingRanges<IBrush> _foregrounds = new();
    private readonly OverlappingRanges<IBrush?> _highlights = new();
    private readonly OverlappingRanges<IPen?> _borders = new();
    private readonly OverlappingRanges<Typeface> _typefaces = new();
    private readonly double _fontSize;
    private double _width = 0;
    private double _height = 0;

    private (int Begin, int End, ShapedBuffer Buffer)[]? _shapedBuffers = null;
    private (GlyphRun GlyphRun, SliceStyle Formatting)[]? _glyphRuns = null;
    public IBrush? Background { get; set; }

    public double Width 
    {
        get
        {
            AssertShaped();
            return _width;
        }
        private set => _width = value;
    }
    public double Height 
    { 
        get
        {
            AssertShaped();
            return _height;
        }
        private set => _height = value;
    }
    public TextAreaLine(ReadOnlyMemory<char> text, IBrush foreground, FontFamily font, double fontSize)
    {
        Text = text;
        _foregrounds.AddRange(0, text.Length, foreground);
        _highlights.AddRange(0, text.Length, null);
        _borders.AddRange(0, text.Length, null);
        _typefaces.AddRange(0, text.Length, new Typeface(font));
        _fontSize = fontSize;
    }

    public void ApplyStyle(Range range, Style style)
    {
        int begin = range.Start.Value;
        int end = range.End.Value;

        if (begin == end)
            return;

        Debug.Assert(begin >= 0 && begin < Text.Length);
        Debug.Assert(begin <= end);

        if (style.Foreground != null)
            _foregrounds.AddRange(begin, end, style.Foreground);
        if (style.Background != null)
            _highlights.AddRange(begin, end, style.Background);
        if (style.Border != null)
            _borders.AddRange(begin, end, style.Border);
        if (style.Typeface != null)
            _typefaces.AddRange(begin, end, style.Typeface.Value);

        _shapedBuffers = null;
        _glyphRuns = null;
    }

    /// <summary>
    /// Gets width of the line consisting of characters from 0 to length.
    /// </summary>
    public double GetWidthSpan(int length = -1)
    {
        AssertShaped();
        Debug.Assert(length <= Text.Length);

        if (length == -1)
            length = Text.Length;

        double width = 0;
        int caretPosition = 0;
        foreach (var (_, _, shapedBuffer) in _shapedBuffers!)
        {
            foreach (var glyph in shapedBuffer)
            {
                if (caretPosition >= length)
                    return width;

                width += glyph.GlyphAdvance;
                caretPosition++;
            }
        }
        return width;
    }

    public void Shape()
    {
        Debug.Assert(_shapedBuffers == null, "Shaping already done. Don't waste CPU cycles.");

        // Shape text runs. Each run has a different typeface (possibly).
        int shapedBufferIndex = 0;
        _shapedBuffers = new (int, int, ShapedBuffer)[_typefaces.Count];
        foreach (var (begin, end, typeface) in _typefaces)
        {
            var options = new TextShaperOptions(typeface.GlyphTypeface, _fontSize);
            var textSlice = Text.Slice(begin, end - begin);
            var shapedBuffer = TextShaper.Current.ShapeText(textSlice, options);
            _shapedBuffers[shapedBufferIndex++] = (begin, end, shapedBuffer);
        }

        int glyphRunIndex = 0;
        var formattings = RangesMerger.Merge(_foregrounds, _highlights, _borders, _typefaces);
        _glyphRuns = new (GlyphRun, SliceStyle)[formattings.Count];
        foreach (var (begin, end, (foreground, highlight, border, typeface)) in formattings)
        {
            var shapedBuffer = FindShapedBuffer(begin, end);
            var textSlice = Text.Slice(begin, end - begin);
            var glyphRun = new GlyphRun(typeface.GlyphTypeface, _fontSize, textSlice, shapedBuffer);
            var formatting = new SliceStyle 
            {
                Foreground = foreground, 
                Background = highlight,
                Border = border, 
                Typeface = typeface 
            };
            _glyphRuns[glyphRunIndex++] = (glyphRun, formatting);
        }

        Width = _glyphRuns.Sum(o => o.GlyphRun.Bounds.Width);
        Height = _glyphRuns.Length > 0 ? _glyphRuns.Max(o => o.GlyphRun.Bounds.Height) : 0;
    }

    IReadOnlyList<GlyphInfo> FindShapedBuffer(int begin, int end)
    {
        foreach(var (bufferBegin, bufferEnd, shapedBuffer) in _shapedBuffers!)
        {
            if (bufferBegin <= begin && end <= bufferEnd)
            {
                int sliceBegin = begin - bufferBegin;
                int sliceEnd = end - begin;
                return new ReadOnlyListSlice<GlyphInfo>(shapedBuffer, sliceBegin, sliceEnd);
            }
        }

        throw new IndexOutOfRangeException("Shaped buffer not found.");
    }

    public void Draw(DrawingContext dc, Point origin)
    {
        AssertShaped();

        double caretPosition = origin.X;
        foreach (var (glyphRun, formatting) in _glyphRuns!)
        {
            using (dc.PushTransform(Matrix.CreateTranslation(origin.WithX(caretPosition))))
            {
                if (formatting.Background is { } background)
                {
                    Rect rect = new(new Point(-0.25, -0.25), new Size(glyphRun.Bounds.Width + 0.5, glyphRun.Bounds.Height + 0.5));
                    dc.DrawRectangle(background, null, rect);
                }

                if (formatting.Border is { } border)
                {
                    Rect rect = new(new Point(0, 0), new Size(glyphRun.Bounds.Width, glyphRun.Bounds.Height));
                    dc.DrawRectangle(null, border, rect);
                }

                dc.DrawGlyphRun(formatting.Foreground, glyphRun);
            }

            caretPosition += glyphRun.Bounds.Width;
        }
    }

    public void Dispose()
    {
        if (_shapedBuffers != null)
            foreach (var (_, _, shapedBuffer) in _shapedBuffers!)
                shapedBuffer.Dispose();

        if (_glyphRuns != null)
            foreach (var (glyphRun, _) in _glyphRuns!)
                glyphRun.Dispose();

        _shapedBuffers = null;
        _glyphRuns = null;
    }

    private struct SliceStyle
    {
        public IBrush Foreground;
        public IBrush? Background;
        public IPen? Border;
        public Typeface Typeface;
    }

    private void AssertShaped()
    {
        Debug.Assert(_shapedBuffers != null, "Shape the text first.");
        Debug.Assert(_glyphRuns != null, "Shape the text first.");
    }
}

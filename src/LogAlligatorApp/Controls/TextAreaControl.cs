using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.Controls.FlatColorPalette;

namespace LogAlligator.Controls
{
    internal struct LineFormatting
    {
        public IBrush? Foreground;
        public IBrush? Background;
    }

    class TextAreaControl : Control
    {
        private struct Line
        {
            public string Text;
            public SortedList<int, Formatting> Formattings = new();

            public Line(string text)
            {
                Text = text;
                if (Text.Length > 0)
                {
                    Formattings.Add(0, Formatting.Default);
                }
            }

            /// <summary>
            /// Get text span with consistent formatting
            /// </summary>
            /// <param name="begin">
            ///     Index of character where new formatting begins. 
            ///     Effectively it must be a key from Formattings list.
            /// </param>
            public (string, Formatting) GetTextSpan(int begin)
            {
                Debug.Assert(begin >= 0 && begin < Text.Length);

                int index = Formattings.IndexOfKey(begin);
                if (index < 0)
                    throw new ArgumentException($"Formatting not found at index {begin}");

                int end = Text.Length;
                if (index < Formattings.Count - 1) // There is some new formatting after begin
                {
                    end = Formattings.Keys[index + 1];
                }

                int length = end - begin;
                var textSpan = Text.Substring(begin, length);
                var formatting = Formattings.Values[index];

                return (textSpan, formatting);
            }

            // TODO: Add support for foreach loop
            public (string, Formatting) GetTextSpanWithFormatting(int formattingIndex)
            {
                Debug.Assert(formattingIndex >= 0 && formattingIndex < Formattings.Count);

                var formatting = Formattings.Values[formattingIndex];
                int begin = Formattings.Keys[formattingIndex];
                int end = Text.Length;
                if (formattingIndex < Formattings.Count - 1)
                    end = Formattings.Keys[formattingIndex + 1];
                int length = end - begin;
                var textSpan = Text.Substring(begin, length);

                return (textSpan, formatting);
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

                Formattings[begin] = formatting;
                Formattings[begin + length] = Formatting.Default;

                // Remove all formattings between 'begin' and 'begin + length'
                RemoveFormattingInRange(begin, begin + length);
            }

            public struct Formatting
            {
                public static readonly Formatting Default = new Formatting();

                public IBrush? Foreground;
                public IBrush? Background;

            }

            private void RemoveFormattingInRange(int begin, int end)
            {
                int indexBegin = Formattings.Keys.IndexOf(begin);
                int indexEnd = Formattings.Keys.IndexOf(end);
                int howManyToRemove = indexEnd - indexBegin - 1;

                while(howManyToRemove-- != 0)
                {
                    Formattings.RemoveAt(indexBegin + 1);
                }
            }
        }

        private List<Line> lines = new();
        //private List<string> lines = new();
        //private List<List<LineFormatting>> formattings = new();

        public IBrush Foreground { get; set; }
        public IBrush Background { get; set; }
        public FontFamily FontFamily { get; set; } = FontFamily.Default;
        public double FontSize { get; set; }

        public double MaxLineWidth { get; private set; }
        public int NumberOfLinesThatCanFit
        {
            get
            {
                return (int)Math.Ceiling(Bounds.Height / GetLineHeight());
            }
        }

        public TextAreaControl() : base()
        {
            var theme = Application.Current!.RequestedThemeVariant;

            Background = Application.Current!.FindResource(theme, "ThemeBackgroundBrush") as IBrush
                ?? throw new ApplicationException("Resource not found");

            Foreground = Application.Current!.FindResource(theme, "ThemeForegroundBrush") as IBrush
                ?? throw new ApplicationException("Resource not found");

            FontSize = Application.Current!.FindResource(theme, "FontSizeNormal") as double?
                ?? throw new ApplicationException("Reource not found");

            ClipToBounds = true;

            if (Design.IsDesignMode)
            {
                lines.Add(new Line("Sample text"));
            }
        }

        public void AppendLine(string line)
        {
            lines.Add(new Line(line));
            MaxLineWidth = Math.Max(MaxLineWidth, GetLineWidth(line));
        }

        public void AppendFormattingToLastLine(int charIndex, int length, IBrush? foreground = null, IBrush? background = null)
        {
            Debug.Assert(lines.Count > 0);

            var line = lines.Last();
            length = Math.Min(length, line.Text.Length - charIndex);
            if (length <= 0)
                return;

            line.AddFormatting(charIndex, length, foreground, background);
        }

        public void Clear()
        {
            lines.Clear();
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
            for(int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                RenderLine(dc, lineIndex, cursor);
                cursor = new Point(cursor.X, cursor.Y + lineHeight);
            }
        }

        private void RenderLine(DrawingContext dc, int lineIndex, Point cursor)
        {
            var line = lines[lineIndex];
            for (int formattingIndex = 0; formattingIndex < line.Formattings.Count; formattingIndex++)
            {
                var (text, formatting) = line.GetTextSpanWithFormatting(formattingIndex);
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
            if (formatting.Background != null)
                dc.FillRectangle(formatting.Background, new Rect(cursor, new Size(textWidth, formattedText.Height)));

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
            return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, FontSize, Foreground);
        }

    }
}

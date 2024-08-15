using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using LogAlligator.App.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Avalonia.Controls.FlatColorPalette;

namespace LogAlligator.App.Controls
{
    class TextAreaControl : Control
    {
        private List<Line> lines = new();

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
        public int NumberOfLinesThatCanFit
        {
            get
            {
                return (int)Math.Ceiling(Bounds.Height / GetLineHeight());
            }
        }

        public TextAreaControl() : base()
        {
            ClipToBounds = true;
            Application.Current!.ActualThemeVariantChanged += (_, _) => InvalidateVisual();

            if (Design.IsDesignMode)
            {
                lines.Add(new Line("Sample text"));
            }
        }

        static TextAreaControl()
        {
            FontSizeProperty.Changed.AddClassHandler<TextAreaControl>((o, _) => o.InvalidateVisual());
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

        public (int LineIndex, int CharIndex) GetCharIndexAtPosition(Point position)
        {
            var lineIndex = (int)(position.Y / GetLineHeight());

            // TODO: Check if lineIndex is out of bounds
            if (lineIndex < 0 || lines.Count == 0)
                return (0, 0);
            if (lineIndex >= lines.Count)
                return (lines.Count - 1, lines.Last().Text.Length);

            var line = lines[lineIndex].Text;
            double cursor = 0;
            int charIndex = 0;
            double previousWidth = 0;

            while(position.X > cursor)
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
            }

            return (lineIndex, charIndex - 1);
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

        private struct Line
        {
            public string Text;
            public RangeList<Formatting> Formattings = new();

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
}

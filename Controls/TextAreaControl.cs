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
    class TextAreaControl : Control
    {
        private List<string> lines = new List<string>();

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
                lines.Add("Sample text");
            }
        }

        public void AppendLine(string line)
        {
            lines.Add(line);
            MaxLineWidth = Math.Max(MaxLineWidth, GetLineWidth(line));
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

            DrawAllLines(dc);
        }

        private void DrawAllLines(DrawingContext dc)
        {
            var lineHeight = GetLineHeight();
            var cursor = new Point(0, 0);
            foreach(var line in lines)
            {
                dc.DrawText(FormatText(line), cursor);
                cursor = new Point(cursor.X, cursor.Y + lineHeight);
            }
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

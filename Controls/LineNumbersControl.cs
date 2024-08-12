using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace LogAlligator.Controls
{
    class LineNumbersControl : Control
    {
        public IBrush Foreground { get; set; }
        public IBrush Background { get; set; }
        public FontFamily FontFamily { get; set; } = FontFamily.Default;
        public double FontSize { get; set; }
        public int NumberOfLines { get; set; } = 20;
        public int FirstLineNumber { get; set; } = 1;

        public LineNumbersControl() : base()
        {
            var theme = Application.Current!.RequestedThemeVariant;
            Background = Application.Current!.FindResource(theme, "ThemeControlMidBrush") as IBrush
                ?? throw new ApplicationException("Resource not found");

            Foreground = Application.Current!.FindResource(theme, "ThemeForegroundBrush") as IBrush
                ?? throw new ApplicationException("Resource not found");

            FontSize = Application.Current!.FindResource(theme, "FontSizeNormal") as double?
                ?? throw new ApplicationException("Reource not found");

            ClipToBounds = true;
        }

        public override void Render(DrawingContext dc)
        {
            dc.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));

            double lineHeight = GetLineHeight();
            var cursor = new Point(0, 0);

            for (int i = 0; i < NumberOfLines; i++)
            {
                int lineNumber = FirstLineNumber + i;
                var lineNumberText = FormatText(lineNumber.ToString());
                var xOffset = Width - lineNumberText.Width;

                dc.DrawText(lineNumberText, cursor.WithX(xOffset));
                cursor = new Point(cursor.X, cursor.Y + lineHeight);
            }
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

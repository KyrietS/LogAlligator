using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace LogAlligator.App.Utils;

public record class Highlight
{
    private const double MINIMAL_CONTRAST_RATIO = 7.5;
    public required SearchPattern Pattern { get; set; }
    public Color Background { get; set; }

    public bool HasEnoughContrastWith(Color color) => ContrastRatio(Background, color) >= MINIMAL_CONTRAST_RATIO;

    public override string ToString()
    {
        return Pattern.Pattern.ToString();
    }

    private static double ContrastRatio(Color color1, Color color2)
    {
        return ContrastRatio(RelativeLuminance(color1), RelativeLuminance(color2));
    }

    /// <summary>
    /// Constrast ratio calculated according to WCAG.
    /// https://www.w3.org/TR/WCAG21/#dfn-contrast-ratio
    /// </summary>
    private static double ContrastRatio(double luminance1, double luminance2)
    {
        return (Math.Max(luminance1, luminance2) + 0.05) / (Math.Min(luminance1, luminance2) + 0.05);
    }

    /// <summary>
    /// Relative luminance calculated according to WCAG.
    /// https://www.w3.org/TR/WCAG21/#dfn-relative-luminance
    /// </summary>
    private static double RelativeLuminance(Color color)
    {
        double R = color.R / 255.0;
        double G = color.G / 255.0;
        double B = color.B / 255.0;

        R = R <= 0.04045 ? R / 12.92 : Math.Pow((R + 0.055) / 1.055, 2.4);
        G = G <= 0.04045 ? G / 12.92 : Math.Pow((G + 0.055) / 1.055, 2.4);
        B = B <= 0.04045 ? B / 12.92 : Math.Pow((B + 0.055) / 1.055, 2.4);

        return 0.2126 * R + 0.7152 * G + 0.0722 * B;
    }
}

public class Highlights : IEnumerable<Highlight>
{
    private readonly List<Highlight> _highlights = [];
    private readonly HighlightColorProvider _colorProvider = new();

    public event EventHandler? OnChange;

    public void Add(ReadOnlyMemory<char> pattern)
    {
        if (pattern.Length == 0)
            return;

        var highlight = _colorProvider.GetHighlightColor();
        _highlights.Add(new Highlight { Pattern = new SearchPattern(pattern), Background = highlight });
        OnChange?.Invoke(this, EventArgs.Empty);
    }


    public void Remove(ReadOnlyMemory<char> pattern)
    {
        int numOfRemoved = _highlights.RemoveAll(h => h.Pattern.Equals(pattern));
        if (numOfRemoved > 0)
            OnChange?.Invoke(this, EventArgs.Empty);
    }

    public bool Contains(ReadOnlyMemory<char> pattern)
    {
        return _highlights.Any(h => h.Pattern.Equals(pattern));
    }

    // FIXME: Just use a proper data binding, please...
    public void ForceRefresh()
    {
        OnChange?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerator<Highlight> GetEnumerator()
    {
        return _highlights.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

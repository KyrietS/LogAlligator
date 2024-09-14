using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;

namespace LogAlligator.App.Utils;

public record struct Highlight
{
    public string Pattern { get; set; }
    public Color Background { get; set; }
    public Color? Foreground { get; set; }

    public void Deconstruct(out string pattern, out Color background, out Color? foreground)
    {
        pattern = Pattern;
        background = Background;
        foreground = Foreground;
    }

    public override string ToString()
    {
        return Pattern;
    }
}

public class Highlights : IEnumerable<Highlight>
{
    private List<Highlight> _highlights = new();

    public event EventHandler? OnChange;

    public void Add(string pattern, Color background)
    {
        if (pattern.Length == 0 || Contains(pattern))
            return;

        _highlights.Add(new Highlight { Pattern = pattern, Background = background });
        OnChange?.Invoke(this, EventArgs.Empty);
    }

    public void Add(string pattern)
    {
        Add(pattern, GetColor());
    }

    public void Remove(string pattern)
    {
        if (!Contains(pattern))
            return;

        _highlights.RemoveAll(h => h.Pattern == pattern);
        OnChange?.Invoke(this, EventArgs.Empty);
    }

    public bool Contains(string pattern)
    {
        return _highlights.Any(h => h.Pattern == pattern);
    }

    private Color GetColor()
    {
        Color[] colors =
        [
            Colors.Yellow,
            Colors.Orange,
            Colors.LimeGreen,
            Colors.Pink,
            Colors.Cyan,
            Colors.Lime,
            Colors.Aqua,
            Colors.Beige,
            Colors.Coral,
            Colors.Gold,
        ];

        foreach (var color in colors)
        {
            if (!_highlights.Any(h => h.Background == color))
                return color;
        }

        // All colors are used, return yellow
        return Colors.Yellow;
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

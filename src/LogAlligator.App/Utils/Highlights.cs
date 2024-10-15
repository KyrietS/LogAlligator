using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;

namespace LogAlligator.App.Utils;

public record struct Highlight
{
    public SearchPattern Pattern { get; set; }
    public Color Background { get; set; }
    public Color? Foreground { get; set; }

    public void Deconstruct(out SearchPattern pattern, out Color background, out Color? foreground)
    {
        pattern = Pattern;
        background = Background;
        foreground = Foreground;
    }

    public override string ToString()
    {
        return Pattern.Pattern.ToString();
    }
}

public class Highlights : IEnumerable<Highlight>
{
    private readonly List<Highlight> _highlights = [];

    public event EventHandler? OnChange;

    public void Add(ReadOnlyMemory<char> pattern, Color background)
    {
        if (pattern.Length == 0 || Contains(pattern))
            return;

        _highlights.Add(new Highlight { Pattern = new SearchPattern(pattern), Background = background });
        OnChange?.Invoke(this, EventArgs.Empty);
    }

    public void Add(ReadOnlyMemory<char> pattern)
    {
        Add(pattern, GetColor());
    }

    public void Remove(ReadOnlyMemory<char> pattern)
    {
        if (!Contains(pattern))
            return;

        _highlights.RemoveAll(h => h.Pattern.Equals(pattern));
        OnChange?.Invoke(this, EventArgs.Empty);
    }

    public bool Contains(ReadOnlyMemory<char> pattern)
    {
        return _highlights.Any(h => h.Pattern.Equals(pattern));
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
            if (_highlights.All(h => h.Background != color))
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

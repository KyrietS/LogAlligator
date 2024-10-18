using System;
using Avalonia.Media;

namespace LogAlligator.App.Utils;

public class HighlightColorProvider(Random? random = null)
{
    private Random _random = random ?? Random.Shared;

    public Color GetHighlightColor()
    {
        return GetRandomColor();
    }

    private Color GetRandomColor()
    {
        double hue = GetHue();
        double saturation = 1.0;
        double lightness = GetLightness();

        return HslColor.ToRgb(hue * 360, saturation, lightness);
    }

    private double GetHue()
    {
        return _random.NextDouble();
    }

    private double GetLightness()
    {
        return RandomBetween(0.1, 0.9);
    }

    private double RandomBetween(double min, double max)
    {
        return min + _random.NextDouble() * (max - min);
    }
}

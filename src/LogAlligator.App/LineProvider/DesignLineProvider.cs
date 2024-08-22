using System.Collections.Generic;
using Avalonia.Controls;

namespace LogAlligator.App.LineProvider;

public class DesignLineProvider : ILineProvider
{
    private readonly List<string> _lines = [];

    public DesignLineProvider()
    {
        if (!Design.IsDesignMode)
            throw new System.InvalidOperationException("DesignLineProvider should be used only in design mode");
    }
    public void AddLine(string line)
    {
        _lines.Add(line);
    }
    
    public int LoadData()
    {
        return _lines.Count;
    }

    public int Count => _lines.Count;

    public string this[int index] => _lines[index];

    public int GetLineLength(int index)
    {
        return _lines[index].Length;
    }
}

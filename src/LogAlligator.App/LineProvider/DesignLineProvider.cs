using System.Collections.Generic;
using System.Threading.Tasks;
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
    public void AddLine(string line) => _lines.Add(line);
    public Task LoadData() => Task.CompletedTask;
    public int Count => _lines.Count;
    public string this[int index] => _lines[index];
    public int GetLineLength(int index) => _lines[index].Length;
}

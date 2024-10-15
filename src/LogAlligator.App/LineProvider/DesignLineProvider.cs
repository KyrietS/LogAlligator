using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace LogAlligator.App.LineProvider;

public class DesignLineProvider : ILineProvider
{
    private readonly List<string> _lines = [];

    public DesignLineProvider()
    {
        if (!Design.IsDesignMode)
            throw new InvalidOperationException("DesignLineProvider should be used only in design mode");
    }
    public void AddLine(string line) => _lines.Add(line);
    public Task LoadData(Action<int> _1, CancellationToken _2) => Task.CompletedTask;
    public int Count => _lines.Count;
    public string this[int index] => _lines[index];
    public int GetLineLength(int index) => _lines[index].Length;
    public int GetLineNumber(int index) => index + 1;
    public int GetLineIndex(int lineNumber) => lineNumber - 1;

    public ILineProvider Grep(Func<string, bool> filter)
    {
        throw new NotImplementedException();
    }
}

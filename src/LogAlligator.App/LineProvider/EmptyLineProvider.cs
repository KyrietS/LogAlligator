using System;
using System.Threading;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public class EmptyLineProvider : ILineProvider
{
    public Task LoadData(Action<int> _1, CancellationToken _2) => Task.CompletedTask;
    public int Count => 0;
    public string this[int index] => throw new IndexOutOfRangeException();
    public int GetLineLength(int index) => throw new IndexOutOfRangeException();
    public int GetLineNumber(int index) => throw new IndexOutOfRangeException();
    public int GetLineIndex(int lineNumber) => throw new IndexOutOfRangeException();

    public ILineProvider Grep(Func<string, bool> filter) => this;
}

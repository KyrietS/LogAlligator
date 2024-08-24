using System;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public class EmptyLineProvider : ILineProvider
{
    public Task LoadData() => Task.CompletedTask;
    public int Count => 0;
    public string this[int index] => throw new IndexOutOfRangeException();
    public int GetLineLength(int index) => throw new IndexOutOfRangeException();
}

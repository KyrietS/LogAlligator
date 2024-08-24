using System;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public class BufferedFileLineProvider(Uri path) : ILineProvider
{
    private readonly Uri _path = path;

    public Task LoadData()
    {
        throw new System.NotImplementedException();
    }

    public int Count { get; }

    public string this[int index] => throw new System.NotImplementedException();

    public int GetLineLength(int index)
    {
        throw new System.NotImplementedException();
    }
}

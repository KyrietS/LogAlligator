using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

/// <summary>
/// This line provider is stupid because it reads the whole file into memory.
/// It is most probably the fastest line provider but not everyone can afford
/// to load the whole (possibly multi-GB) file into RAM.
/// </summary>
/// <param name="path"></param>
public class StupidFileLineProvider(Uri path) : ILineProvider
{
    private string[] _lines = [];

    public async Task LoadData(Action<int> progressCallback, CancellationToken token)
    {
        await Task.Delay(1000, token);
        progressCallback(100);
        await Task.Delay(1000, token);
        progressCallback(200);

        _lines = await File.ReadAllLinesAsync(path.LocalPath, token);
    }

    public int Count => _lines.Length;

    public string this[int index]
    {
        get
        {
            return _lines[index];
        }
    }

    public int GetLineLength(int index)
    {
        if (index < 0 || index >= _lines.Length)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _lines[index].Length;
    }

    public int GetLineNumber(int index)
    {
        return index + 1;
    }

    public int GetLineIndex(int lineNumber)
    {
        return lineNumber - 1;

    }

    public ILineProvider Grep(Func<string, bool> filter)
    {
        var newProvider = new StupidFileLineProvider(path) { _lines = _lines.Where(filter).ToArray() };
        return newProvider;
    }
}

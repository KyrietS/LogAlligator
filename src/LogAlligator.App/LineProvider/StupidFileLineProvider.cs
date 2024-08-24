using System;
using System.IO;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public class StupidFileLineProvider(Uri path) : ILineProvider
{
    private string[] _lines = [];
    
    public async Task LoadData()
    {
        _lines = await File.ReadAllLinesAsync(path.AbsolutePath);
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
}

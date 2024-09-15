using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

public class BufferedFileLineProvider(Uri path) : ILineProvider
{
    private FileStream _stream = null!;
    private StreamReader _reader = null!;
    private readonly List<(long Begin, int Length)> _lines = [];

    public async Task LoadData(Action<int> progressCallback, CancellationToken token)
    {
        await LoadLinesData(progressCallback, token);
        
        const FileOptions fileOptions = FileOptions.RandomAccess; // profile if better: FileOptions.SequentialScan;
        _stream = new FileStream(path.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, fileOptions);
        _reader = new StreamReader(_stream, Encoding.UTF8, true, 300, true);
    }

    private async Task LoadLinesData(Action<int> progressCallback, CancellationToken token)
    {
        const FileOptions fileOptions = FileOptions.SequentialScan | FileOptions.Asynchronous;
        await using var stream = new FileStream(path.AbsolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, fileOptions);
        var reader = new StreamLineReader(stream, 65536);
             
        while (reader.ReadLine() is { } line) // TODO: Make Async
        {
            _lines.Add((line.Begin, (int)line.Length));
            
            if (_lines.Count % 100 == 0) 
                progressCallback(_lines.Count);
        }

        progressCallback(_lines.Count);
    }
    
    public int Count => _lines.Count;

    public string this[int index]
    {
        get
        {
            if (index >= _lines.Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, "Could not find requested line at: " + index);
            return ReadLine(_lines[index].Begin);
        }
    }

    public int GetLineLength(int index)
    {
        if (index < 0 || index >= _lines.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        
        return _lines[index].Length;
    }

    public ILineProvider Grep(Func<string, bool> filter)
    {
        var newProvider = new BufferedFileLineProvider(path);
        newProvider._stream = _stream;
        newProvider._reader = _reader;
        newProvider._lines.AddRange(_lines.Where(line => filter(ReadLine(line.Begin))));
        return newProvider;
    }

    private string ReadLine(long begin)
    {
        _reader.BaseStream.Position = begin;
        _reader.DiscardBufferedData();
        return _reader.ReadLine() ?? "";
    }
}

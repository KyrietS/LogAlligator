using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace LogAlligator.App.LineProvider;

public class BufferedFileLineProvider(Uri path) : ILineProvider
{
    private FileStream _stream = null!;
    private StreamReader _reader = null!;
    // FIXME: Length does not seem to be needed
    private readonly List<(int Number, long Begin, int Length)> _lines = [];

    public async Task LoadData(Action<int> progressCallback, CancellationToken token)
    {
        await LoadLinesData(progressCallback, token);

        const FileOptions fileOptions = FileOptions.RandomAccess; // profile if better: FileOptions.SequentialScan;
        _stream = new FileStream(path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, fileOptions);
        _reader = new StreamReader(_stream, Encoding.UTF8, true, 300, true);
    }

    private async Task LoadLinesData(Action<int> progressCallback, CancellationToken token)
    {
        const FileOptions fileOptions = FileOptions.SequentialScan | FileOptions.Asynchronous;
        using var stream = new FileStream(path.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, fileOptions);
        var reader = new StreamLineReader(stream, 65536);
        int lineNumber = 1;

        await Task.Run(() =>
        {
            while (reader.ReadLine() is { } line)
            {
                _lines.Add((lineNumber, line.Begin, (int)line.Length));
                lineNumber++;

                if (_lines.Count % 1000 == 0)
                {
                    token.ThrowIfCancellationRequested();
                    Dispatcher.UIThread.Post(() => progressCallback(_lines.Count));
                }
            }
        }, token);

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

    public int GetLineNumber(int index)
    {
        if (index < 0 || index >= _lines.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return _lines[index].Number;
    }

    public int GetLineIndex(int lineNumber)
    {
        // Binary search
        int left = 0;
        int right = _lines.Count - 1;
        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            if (_lines[mid].Number == lineNumber)
                return mid;
            if (_lines[mid].Number < lineNumber)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return -1; // Not found
    }

    public ILineProvider Grep(Func<string, bool> filter)
    {
        var newProvider = new BufferedFileLineProvider(path) { _stream = _stream, _reader = _reader };
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

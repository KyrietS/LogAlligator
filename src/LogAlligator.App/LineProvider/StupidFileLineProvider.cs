﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogAlligator.App.LineProvider;

/// <summary>
/// This line provider is stupid because it reads the whole file into memory.
/// It is most probably the fastest line provider but not everyone can affort
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
        
        _lines = await File.ReadAllLinesAsync(path.AbsolutePath, token);
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

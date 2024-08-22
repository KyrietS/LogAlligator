﻿using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace LogAlligator.App.LineProvider;

public class StupidFileLineProvider(Uri path) : ILineProvider
{
    private string[] _lines = [];

    public int LoadData()
    {
        _lines = File.ReadAllLines(path.AbsolutePath);
        return _lines.Length;
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
            throw new System.ArgumentOutOfRangeException(nameof(index));
        
        return _lines[index].Length;
    }
}

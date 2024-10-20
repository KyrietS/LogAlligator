using System;
using System.IO;

namespace LogAlligator.App.LineProvider;

internal class StreamLineReader
{
    private readonly Stream _stream;
    private readonly byte[] _buffer;

    private long _begin = 0;     // Buffer begin in the stream
    private long _end = 0;       // Buffer end in the stream
    private long _cursor = 0;    // Cursor in the buffer
    private long _lineBegin = 0; // Line begin in the stream
    private bool _eof = false;   // End of file reached

    public StreamLineReader(Stream stream, int bufferSize)
    {
        Validate(stream, bufferSize);
        
        _stream = stream;
        _buffer = new byte[bufferSize];
        LoadNewBuffer();
    }
    
    public long StreamPosition
    {
        get => _stream.Position;
        set
        {
            _stream.Position = value;
            _begin = value;
            _end = value;
            _cursor = value;
            _lineBegin = value;
            _eof = false;
            LoadNewBuffer();
        }
    }
    
    public bool EndOfStream => _eof;

    /// <summary>
    /// Reads a line from the stream. Line is terminated by '\n', '\r' or '\r\n'.
    /// </summary>
    /// <returns>string line if there is one or null if no more characters are in the stream.</returns>
    public (long Begin, long Length)? ReadLine()
    {
        if (_eof)
            return null;

        while (true)
        {
            var (foundPos, searchLen) = SearchBuffer();
            if (foundPos >= _cursor) // Newline found in the buffer
            {
                var result = (Begin: _lineBegin, Length: foundPos - _lineBegin);
                _lineBegin = foundPos + searchLen;
                _cursor = _lineBegin;
                return result;
            }

            LoadNewBuffer();
        
            if (_eof)
                return (Begin: _lineBegin, Length: _end - _lineBegin);
        }
    }

    private (long Pos, int Length) SearchBuffer()
    {
        if (_cursor > _end)
            return (-1, 0);
        
        int viewBegin = (int)(_cursor - _begin);
        int viewLength = (int)(_end - _cursor);
        var bufferView = _buffer.AsSpan(viewBegin, viewLength);
        var foundPos = bufferView.IndexOfAny((byte)'\r', (byte)'\n');
        int searchLength = 1;
        
        if (foundPos >= 0 && bufferView[foundPos] == '\r')
        {
            searchLength = IsNextCharLineFeed(_cursor + foundPos) ? 2 : 1;
        }
        
        return (_cursor + foundPos, searchLength);
    }

    // TODO: Can I make this without seeking (changing stream Position)?
    private bool IsNextCharLineFeed(long pos)
    {
        long originalPos = _stream.Position;
        _stream.Position = pos + 1;
        int nextByte = _stream.ReadByte();
        _stream.Position = originalPos;
        return nextByte == '\n';
    }
    
    private void LoadNewBuffer()
    {
        _begin = _end;
        _end = _begin + _stream.Read(_buffer.AsSpan());
        _cursor = Math.Max(_begin, _cursor);
        _eof = _begin == _end;
    }

    private static void Validate(Stream stream, int bufferSize)
    {
        if (bufferSize <= 0)     
            throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be greater than 0");
        
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));
        
        if (!stream.CanSeek)
            throw new ArgumentException("Stream must be seekable", nameof(stream));
    }
}

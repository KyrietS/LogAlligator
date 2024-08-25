using LogAlligator.App.LineProvider;

namespace LogAlligator.Tests.LineProvider;

public class StreamLineReaderTests
{
    public static IEnumerable<object[]> BufferSizes()
    {
        yield return [1];
        yield return [2];
        yield return [3];
        yield return [4];
        yield return [5];
        yield return [6];
        yield return [7];
        yield return [8];
        yield return [1024];
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void NoNewlineCharacter(int bufferSize)
    {
        var stream = new MemoryStream("abc"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void OnlyNewLine(int bufferSize)
    {
        var stream = new MemoryStream("\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 0), sut.ReadLine());
        Assert.Equal((1, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void DoubleNewLine(int bufferSize)
    {
        var stream = new MemoryStream("\n\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 0), sut.ReadLine());
        Assert.Equal((1, 0), sut.ReadLine());
        Assert.Equal((2, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void TripleNewLine(int bufferSize)
    {
        var stream = new MemoryStream("\n\n\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 0), sut.ReadLine());
        Assert.Equal((1, 0), sut.ReadLine());
        Assert.Equal((2, 0), sut.ReadLine());
        Assert.Equal((3, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void NewLineAtTheEndOfStream(int bufferSize)
    {
        var stream = new MemoryStream("abcd\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        Assert.Equal((0, 4), sut.ReadLine());
        Assert.Equal((5, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void NewLineAtTheBeginningOfStream(int bufferSize)
    {
        var stream = new MemoryStream("\nabc"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        Assert.Equal((0, 0), sut.ReadLine());
        Assert.Equal((1, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void NewLineAtTheBeginningAndEndOfStream(int bufferSize)
    {
        var stream = new MemoryStream("\nabc\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        Assert.Equal((0, 0), sut.ReadLine());
        Assert.Equal((1, 3), sut.ReadLine());
        Assert.Equal((5, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void TwoLines(int bufferSize)
    {
        var stream = new MemoryStream("abc\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((4, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void EmptyLineBetweenTwoLines(int bufferSize)
    {
        var stream = new MemoryStream("abc\n\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((4, 0), sut.ReadLine());
        Assert.Equal((5, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void TwoLinesAndNewLineAtEnd(int bufferSize)
    {
        var stream = new MemoryStream("abc\ndef\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((4, 3), sut.ReadLine());
        Assert.Equal((8, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void SetPositionToBegin(int bufferSize)
    {
        var stream = new MemoryStream("abc\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        sut.StreamPosition = 0;
        
        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((4, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void SetPositionToBeginAfterReading(int bufferSize)
    {
        var stream = new MemoryStream("abc\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);
        
        sut.ReadLine();

        sut.StreamPosition = 0;
        
        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((4, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void SetPositionToBeginOfSecondLine(int bufferSize)
    {
        var stream = new MemoryStream("abc\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        sut.StreamPosition = 4;
        
        Assert.Equal((4, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void SetPositionToMiddleOfLine(int bufferSize)
    {
        var stream = new MemoryStream("abcdef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        sut.StreamPosition = 2;
        
        Assert.Equal((2, 4), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void SetPositionToLastCharacterInLine(int bufferSize)
    {
        var stream = new MemoryStream("abc"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        sut.StreamPosition = 2;
        
        Assert.Equal((2, 1), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void SetPositionToAfterLastCharacterInLine(int bufferSize)
    {
        var stream = new MemoryStream("abc"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        sut.StreamPosition = 3;
        
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void CarriageReturn(int bufferSize)
    {
        var stream = new MemoryStream("abc\rdef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((4, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void WindowsNewLine(int bufferSize)
    {
        var stream = new MemoryStream("abc\r\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((5, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void WindowsNewLine_EmptyLine(int bufferSize)
    {
        var stream = new MemoryStream("\r\n"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 0), sut.ReadLine());
        Assert.Equal((2, 0), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
    
    [Theory]
    [MemberData(nameof(BufferSizes))]
    public void WindowsNewLine_EmptyLineBetweenLines(int bufferSize)
    {
        var stream = new MemoryStream("abc\r\n\r\ndef"u8.ToArray());
        var sut = new StreamLineReader(stream, bufferSize);

        Assert.Equal((0, 3), sut.ReadLine());
        Assert.Equal((5, 0), sut.ReadLine());
        Assert.Equal((7, 3), sut.ReadLine());
        Assert.Null(sut.ReadLine());
    }
}

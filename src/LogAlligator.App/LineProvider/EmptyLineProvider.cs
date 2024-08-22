using System;

namespace LogAlligator.App.LineProvider;

public class EmptyLineProvider : ILineProvider
{
    public int LoadData()
    {
        return 0;
    }

    public int Count => 0;

    public string this[int index] => throw new IndexOutOfRangeException();

    public int GetLineLength(int index)
    {
        throw new IndexOutOfRangeException();
    }
}

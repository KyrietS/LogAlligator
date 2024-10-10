
using System;
using System.Collections;
using System.Collections.Generic;

namespace LogAlligator.App.Utils.Ranges;
internal class Ranges<TValueType>(int _capacity = 0) : IEnumerable<(int Begin, int End, TValueType Value)>
{
    public SortedList<int, TValueType> Boundaries { get; set; } = new(_capacity);

    public int Count => Math.Max(0, Boundaries.Count - 1);
    public (int Begin, int End, TValueType Value) this[int index] => GetRangeAtIndex(index);


    public void Clear()
    {
        Boundaries.Clear();
    }

    public (int Begin, int End, TValueType Value) GetRangeAtIndex(int index)
    {
        if (index < 0 || index >= Boundaries.Count - 1)
            throw new ArgumentOutOfRangeException();

        return (Boundaries.GetKeyAtIndex(index), Boundaries.GetKeyAtIndex(index + 1), Boundaries.GetValueAtIndex(index));
    }

    public List<(int Begin, int End, TValueType Value)> GetAllRanges()
    {
        List<(int, int, TValueType)> result = new();
        for (var i = 0; i < Boundaries.Count - 1; i++)
            result.Add((Boundaries.GetKeyAtIndex(i), Boundaries.GetKeyAtIndex(i + 1), Boundaries.GetValueAtIndex(i)));

        return result;
    }

    public IEnumerator<(int Begin, int End, TValueType Value)> GetEnumerator()
    {
        for (int i = 0; i < Boundaries.Count - 1; i++)
        {
            yield return (Boundaries.GetKeyAtIndex(i), Boundaries.GetKeyAtIndex(i + 1), Boundaries.GetValueAtIndex(i));
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

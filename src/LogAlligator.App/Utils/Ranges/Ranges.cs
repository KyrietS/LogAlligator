
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
        return new RangesEnumerator(Boundaries);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class RangesEnumerator(SortedList<int, TValueType> ranges)
        : IEnumerator<(int Begin, int End, TValueType Value)>
    {
        private int _index = -1;

        public (int Begin, int End, TValueType Value) Current => (ranges.GetKeyAtIndex(_index),
            ranges.GetKeyAtIndex(_index + 1), ranges.GetValueAtIndex(_index));

        object IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < ranges.Count - 1;

        public void Reset() => _index = -1;

        public void Dispose() { }
    }
}

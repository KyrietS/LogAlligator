using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LogAlligator.App.Utils;

/// <summary>
///     A data structure where you can assign ValueType to an integer range.
///     Ranges cannot have any gaps. So for example if you add 2 disjoint
///     ranges in the following order: (1, 3, 'a') and (5, 7, 'b') you will
///     get 3 ranges: (1, 3, 'a'), (3, 7, 'a'), (5, 7, 'b')
/// </summary>
internal class RangeList<TValueType> : IEnumerable<(int Begin, int End, TValueType Value)>
{
    private readonly SortedList<int, TValueType> _ranges = new();

    public int Count => Math.Max(0, _ranges.Count - 1);

    public (int Begin, int End, TValueType Value) this[int index] => GetRangeAtIndex(index);

    public IEnumerator<(int Begin, int End, TValueType Value)> GetEnumerator()
    {
        return new RangeListEnumerator(_ranges);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void AddRange(int start, int end, TValueType value)
    {
        Debug.Assert(start <= end);

        if (_ranges.Count == 0)
        {
            _ranges[start] = value;
            _ranges[end] = value;
            return;
        }

        var currVal = _ranges.First().Value;

        // Insert begin
        for (var i = 0; i <= _ranges.Count; i++)
        {
            if (i == _ranges.Count)
            {
                _ranges[start] = value;
                break;
            }

            var rangeBegin = _ranges.GetKeyAtIndex(i);
            currVal = _ranges.GetValueAtIndex(i);

            if (start <= rangeBegin)
            {
                _ranges[start] = value;
                break;
            }
        }

        // Insert end
        for (var i = _ranges.IndexOfKey(start) + 1; i <= _ranges.Count; i++)
        {
            if (i == _ranges.Count)
            {
                _ranges[end] = currVal;
                break;
            }

            var rangeBegin = _ranges.GetKeyAtIndex(i);

            if (end < rangeBegin)
            {
                _ranges[end] = currVal;
                break;
            }

            if (end == rangeBegin) break;

            // Remove ranges between (start, end)
            if (rangeBegin < end)
            {
                currVal = _ranges.GetValueAtIndex(i);
                _ranges.RemoveAt(i);
                i--;
            }
        }
    }

    public void Clear()
    {
        _ranges.Clear();
    }

    public (int Begin, int End, TValueType Value) GetRangeAtIndex(int index)
    {
        if (index < 0 || index >= _ranges.Count - 1)
            throw new ArgumentOutOfRangeException();

        return (_ranges.GetKeyAtIndex(index), _ranges.GetKeyAtIndex(index + 1), _ranges.GetValueAtIndex(index));
    }

    public List<(int Begin, int End, TValueType Value)> GetAllRanges()
    {
        List<(int, int, TValueType)> result = new();
        for (var i = 0; i < _ranges.Count - 1; i++)
            result.Add((_ranges.GetKeyAtIndex(i), _ranges.GetKeyAtIndex(i + 1), _ranges.GetValueAtIndex(i)));

        return result;
    }

    private class RangeListEnumerator(SortedList<int, TValueType> ranges)
        : IEnumerator<(int Begin, int End, TValueType Value)>
    {
        private int _index = -1;

        public (int Begin, int End, TValueType Value) Current => (ranges.GetKeyAtIndex(_index),
            ranges.GetKeyAtIndex(_index + 1), ranges.GetValueAtIndex(_index));

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < ranges.Count - 1;
        }

        public void Reset()
        {
            _index = -1;
        }

        public void Dispose()
        {
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogAlligator.App.Utils
{
    /// <summary>
    /// A data structure where you can assign ValueType to a integer range.
    /// 
    /// Ranges cannot have any gaps. So for example if you add 2 disjoint
    /// ranges in the following order: (1, 3, 'a') and (5, 7, 'b') you will
    /// get 3 ranges: (1, 3, 'a'), (3, 7, 'a'), (5, 7, 'b')
    /// </summary>
    /// <typeparam name="ValueType"></typeparam>
    internal class RangeList<ValueType> : IEnumerable<(int Begin, int End, ValueType Value)>
    {
        private SortedList<int, ValueType> ranges = new();

        public int Count => Math.Max(0, ranges.Count - 1);

        public (int Begin, int End, ValueType Value) this[int index]
        {
            get { return GetRangeAtIndex(index); }
        }

        public void AddRange(int start, int end, ValueType value)
        {
            Debug.Assert(start <= end);

            if (ranges.Count == 0)
            {
                ranges[start] = value;
                ranges[end] = value;
                return;
            }

            ValueType currVal = ranges.First().Value;

            // Insert begin
            for (int i = 0; i <= ranges.Count; i++)
            {
                if (i == ranges.Count)
                {
                    ranges[start] = value;
                    break;
                }

                var rangeBegin = ranges.GetKeyAtIndex(i);
                currVal = ranges.GetValueAtIndex(i);

                if (start <= rangeBegin)
                {
                    ranges[start] = value;
                    break;
                }
            }

            // Insert end
            for (int i = ranges.IndexOfKey(start) + 1; i <= ranges.Count; i++)
            {
                if (i == ranges.Count)
                {
                    ranges[end] = currVal;
                    break;
                }

                var rangeBegin = ranges.GetKeyAtIndex(i);

                if (end < rangeBegin)
                {
                    ranges[end] = currVal;
                    break;
                }

                if (end == rangeBegin)
                {
                    break;
                }

                // Remove ranges between (start, end)
                if (rangeBegin < end)
                {
                    currVal = ranges.GetValueAtIndex(i);
                    ranges.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Clear()
        {
            ranges.Clear();
        }

        public (int Begin, int End, ValueType Value) GetRangeAtIndex(int index)
        {
            if (index < 0 || index >= ranges.Count - 1)
                throw new ArgumentOutOfRangeException();

            return (ranges.GetKeyAtIndex(index), ranges.GetKeyAtIndex(index + 1), ranges.GetValueAtIndex(index));
        }

        public List<(int Begin, int End, ValueType Value)> GetAllRanges()
        {
            List<(int, int, ValueType)> result = new();
            for (int i = 0; i < ranges.Count - 1; i++)
            {
                result.Add((ranges.GetKeyAtIndex(i), ranges.GetKeyAtIndex(i + 1), ranges.GetValueAtIndex(i)));
            }

            return result;
        }

        public IEnumerator<(int Begin, int End, ValueType Value)> GetEnumerator()
        {
            return new RangeListEnumerator(ranges);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class RangeListEnumerator : IEnumerator<(int Begin, int End, ValueType Value)>
        {
            private readonly SortedList<int, ValueType> ranges;
            private int index = -1;

            public RangeListEnumerator(SortedList<int, ValueType> ranges)
            {
                this.ranges = ranges;
            }

            public (int Begin, int End, ValueType Value) Current
            {
                get
                {
                    return (ranges.GetKeyAtIndex(index), ranges.GetKeyAtIndex(index + 1), ranges.GetValueAtIndex(index));
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                index++;
                return index < ranges.Count - 1;
            }

            public void Reset()
            {
                index = -1;
            }

            public void Dispose()
            {
            }
        }
    }
}

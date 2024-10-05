using System.Diagnostics;
using System.Linq;

namespace LogAlligator.App.Utils.Ranges;


/// <summary>
///     A data structure where you can assign ValueType to an integer range.
///     Ranges cannot have any gaps. So for example if you add 2 disjoint
///     ranges in the following order: (1, 3, 'a') and (5, 7, 'b') you will
///     get 3 ranges: (1, 3, 'a'), (3, 7, 'a'), (5, 7, 'b')
/// </summary>
internal class OverlappingRanges<TValueType> : Ranges<TValueType>
{
    public void AddRange(int start, int end, TValueType value)
    {
        Debug.Assert(start <= end);

        if (Boundaries.Count == 0)
        {
            Boundaries[start] = value;
            Boundaries[end] = value;
            return;
        }

        var currVal = Boundaries.First().Value;

        // Insert begin
        for (var i = 0; i <= Boundaries.Count; i++)
        {
            if (i == Boundaries.Count)
            {
                Boundaries[start] = value;
                break;
            }

            var rangeBegin = Boundaries.GetKeyAtIndex(i);

            if (start < rangeBegin)
            {
                Boundaries[start] = value;
                break;
            }
            if (start == rangeBegin)
            {
                currVal = Boundaries.GetValueAtIndex(i);
                Boundaries[start] = value;
                break;
            }


            currVal = Boundaries.GetValueAtIndex(i);
        }

        // Insert end
        for (var i = Boundaries.IndexOfKey(start) + 1; i <= Boundaries.Count; i++)
        {
            if (i == Boundaries.Count)
            {
                Boundaries[end] = currVal;
                break;
            }

            var rangeBegin = Boundaries.GetKeyAtIndex(i);

            if (end < rangeBegin)
            {
                Boundaries[end] = currVal;
                break;
            }

            if (end == rangeBegin) break;

            // Remove ranges between (start, end)
            if (rangeBegin < end)
            {
                currVal = Boundaries.GetValueAtIndex(i);
                Boundaries.RemoveAt(i);
                i--;
            }
        }
    }

}

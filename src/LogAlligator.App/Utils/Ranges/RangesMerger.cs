using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace LogAlligator.App.Utils.Ranges;

/// <summary>
/// Merges multiple ranges into one. Each range is treated as a separate channel.
/// The result is a single range with a tuple of values from each channel.
/// </summary>
internal static class RangesMerger
{
    public static Ranges<(T1, T2)> Merge<T1, T2>(Ranges<T1> channel1, Ranges<T2> channel2)
    {
        if (channel1.Count == 0 && channel2.Count == 0)
            return new Ranges<(T1, T2)>(0);

        Debug.Assert(channel1.First().Begin == channel2.First().Begin, "Cannot merge ranges with different begin");

        MergedRanges merged = new([channel1.Boundaries, channel2.Boundaries]);
        Ranges<(T1, T2)> result = new(merged.Boundaries.Count);
        foreach (var (begin, values) in merged.Boundaries)
        {
            result.Boundaries[begin] = ((T1)values[0].Value!, (T2)values[1].Value!);
        }
        return result;
    }
    public static Ranges<(T1, T2, T3)> Merge<T1, T2, T3>(Ranges<T1> channel1, Ranges<T2> channel2, Ranges<T3> channel3)
    {
        if (channel1.Count == 0 && channel2.Count == 0 && channel3.Count == 0)
            return new Ranges<(T1, T2, T3)>(0);

        Debug.Assert(channel1.First().Begin == channel2.First().Begin && 
            channel2.First().Begin == channel3.First().Begin, "Cannot merge ranges with different begin");

        MergedRanges merged = new([channel1.Boundaries, channel2.Boundaries, channel3.Boundaries]);
        Ranges<(T1, T2, T3)> result = new(merged.Boundaries.Count);
        foreach (var (begin, values) in merged.Boundaries)
        {
            result.Boundaries[begin] = ((T1)values[0].Value!, (T2)values[1].Value!, (T3)values[2].Value!);
        }
        return result;
    }

    public static Ranges<(T1, T2, T3, T4)> Merge<T1, T2, T3, T4>(Ranges<T1> channel1, Ranges<T2> channel2, Ranges<T3> channel3, Ranges<T4> channel4)
    {
        if (channel1.Count == 0 && channel2.Count == 0 && channel3.Count == 0)
            return new Ranges<(T1, T2, T3, T4)>(0);

        Debug.Assert(channel1.First().Begin == channel2.First().Begin &&
            channel2.First().Begin == channel3.First().Begin &&
            channel3.First().Begin == channel4.First().Begin, "Cannot merge ranges with different begin");

        MergedRanges merged = new([channel1.Boundaries, channel2.Boundaries, channel3.Boundaries, channel4.Boundaries]);
        Ranges<(T1, T2, T3, T4)> result = new(merged.Boundaries.Count);
        foreach (var (begin, values) in merged.Boundaries)
        {
            result.Boundaries[begin] = ((T1)values[0].Value!, (T2)values[1].Value!, (T3)values[2].Value!, (T4)values[3].Value!);
        }
        return result;
    }

    private class MergedRanges : Ranges<MergedRanges.MergedValue[]>
    {
        IDictionary[] _channels;
        public MergedRanges(IDictionary[] channels) : base(channels.Sum(ch => ch.Count))
        {
            Debug.Assert(channels.All(ch => ch.Count > 0));
            _channels = channels;
            MergeChannels();
        }

        public void MergeChannels()
        {
            for (var channelIndex = 0; channelIndex < _channels.Length; channelIndex++)
            {
                AddAllValuesFromChannel(channelIndex);
            }

            MergeValuesFromChannels();
        }

        private void AddAllValuesFromChannel(int channelIndex)
        {
            var channel = _channels[channelIndex];
            foreach (DictionaryEntry boundary in channel)
            {
                AddValueFromChannel(channelIndex, (int)boundary.Key, boundary.Value);
            }
        }

        private void AddValueFromChannel(int channelIndex, int begin, object? value)
        {
            if (!Boundaries.ContainsKey(begin))
            {
                var channelValues = new MergedValue[_channels.Length];
                channelValues[channelIndex] = new MergedValue { Value = value };
                Boundaries[begin] = channelValues;
            }
            else
            {
                Boundaries[begin][channelIndex] = new MergedValue { Value = value };
            }
        }

        private void MergeValuesFromChannels()
        {
            Debug.Assert(Boundaries.Count > 0);

            var oldValues = (MergedValue[])Boundaries[0].Clone();
            foreach (var (begin, newValues) in Boundaries)
            {
                MergeValues(begin, oldValues, newValues);
            }
        }

        private void MergeValues(int begin, MergedValue[] oldValues, MergedValue[] newValues)
        {
            Debug.Assert(oldValues.Length == newValues.Length);

            for (var i = 0; i < oldValues.Length; i++)
            {
                if (newValues[i] == null)
                {
                    newValues[i] = oldValues[i];
                }
                else
                {
                    oldValues[i] = newValues[i];
                }
            }
        }

        internal class MergedValue
        {
            public object? Value;
        }
    }
}

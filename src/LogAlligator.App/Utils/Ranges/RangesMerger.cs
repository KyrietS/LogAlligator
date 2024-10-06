using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace LogAlligator.App.Utils.Ranges;

internal static class RangesMerger
{
    public static Ranges<(T1, T2)> Merge<T1, T2>(Ranges<T1> channel1, Ranges<T2> channel2)
    {
        MergedRanges merged = new([channel1.Boundaries, channel2.Boundaries]);
        Ranges<(T1, T2)> result = new(merged.Boundaries.Count);
        foreach (var (begin, values) in merged.Boundaries)
        {
            result.Boundaries[begin] = ((T1)values[0], (T2)values[1]);
        }
        return result;
    }
    public static Ranges<(T1, T2, T3)> Merge<T1, T2, T3>(Ranges<T1> channel1, Ranges<T2> channel2, Ranges<T3> channel3)
    {
        MergedRanges merged = new([channel1.Boundaries, channel2.Boundaries, channel3.Boundaries]);
        Ranges<(T1, T2, T3)> result = new(merged.Boundaries.Count);
        foreach (var (begin, values) in merged.Boundaries)
        {
            result.Boundaries[begin] = ((T1)values[0], (T2)values[1], (T3)values[2]);
        }
        return result;
    }


    private class MergedRanges : Ranges<object[]>
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
                AddValueFromChannel(channelIndex, (int)boundary.Key, boundary.Value!);
            }
        }

        private void AddValueFromChannel(int channelIndex, int begin, object value)
        {
            if (!Boundaries.ContainsKey(begin))
            {
                var channelValues = new object[_channels.Length];
                channelValues[channelIndex] = value;
                Boundaries[begin] = channelValues;
            }
            else
            {
                Boundaries[begin][channelIndex] = value;
            }
        }

        private void MergeValuesFromChannels()
        {
            Debug.Assert(Boundaries.Count > 0);

            var oldValues = (object[])Boundaries[0].Clone();
            foreach (var (begin, newValues) in Boundaries)
            {
                MergeValues(begin, oldValues, newValues);
            }
        }

        private void MergeValues(int begin, object[] oldValues, object[] newValues)
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
    }
}

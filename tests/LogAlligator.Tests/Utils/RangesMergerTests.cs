using LogAlligator.App.Utils.Ranges;

namespace LogAlligator.Tests.Utils;

public class RangesMergerTests
{
    private OverlappingRanges<char?> channel1 = new();
    private OverlappingRanges<char?> channel2 = new();
    private OverlappingRanges<char?> channel3 = new();

    [Fact]
    public void MergeTwoIdenticalChannels()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 9, 'b');

        var result = RangesMerger.Merge(channel1, channel2);

        Assert.Single(result);
        Assert.Equal((0, 9, ('a', 'b')), result[0]);
    }

    [Fact]
    public void MergeThreeIdenticalChannels()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 9, 'b');
        channel3.AddRange(0, 9, 'c');

        var result = RangesMerger.Merge(channel1, channel2, channel3);

        Assert.Single(result);
        Assert.Equal((0, 9, ('a', 'b', 'c')), result[0]);
    }

    [Fact]
    public void CannotMergeTwoChannelsWithDifferentBegins()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(1, 9, 'b');

        Assert.ThrowsAny<Exception>(() => RangesMerger.Merge(channel1, channel2));
    }

    [Fact]
    public void CannotMergeThreeChannelsWithDifferentBegins()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 9, 'b');
        channel3.AddRange(1, 9, 'c');

        Assert.ThrowsAny<Exception>(() => RangesMerger.Merge(channel1, channel2, channel3));
    }

    [Fact]
    public void MergeTwoChannelsWithDifferentEnds()
    {
        channel1.AddRange(0, 5, 'a');
        channel2.AddRange(0, 3, 'b');
        channel2.AddRange(3, 7, 'c');

        var result = RangesMerger.Merge(channel1, channel2);

        Assert.Equal(3, result.Count);
        Assert.Equal((0, 3, ('a', 'b')), result[0]);
        Assert.Equal((3, 5, ('a', 'c')), result[1]);
        Assert.Equal((5, 7, ('a', 'c')), result[2]);
    }

    [Fact]
    public void MergeTwoChannels_WithNull()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 9, null);

        var result = RangesMerger.Merge(channel1, channel2);
        Assert.Single(result);
        Assert.Equal((0, 9, ('a', null)), result[0]);
    }

    [Fact]
    public void MergeTwoChannels_WithMultipleRanges()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 4, 'b');
        channel2.AddRange(4, 9, 'c');

        var result = RangesMerger.Merge(channel1, channel2);

        Assert.Equal(2, result.Count);
        Assert.Equal((0, 4, ('a', 'b')), result[0]);
        Assert.Equal((4, 9, ('a', 'c')), result[1]);
    }

    [Fact]
    public void MergeTwoChannels_WithMultipleRangesOverlapping()
    {
        channel1.AddRange(0, 5, 'a');
        channel1.AddRange(5, 9, 'b');
        channel2.AddRange(0, 2, 'c');
        channel2.AddRange(2, 9, 'd');

        var result = RangesMerger.Merge(channel1, channel2);

        Assert.Equal(3, result.Count);
        Assert.Equal((0, 2, ('a', 'c')), result[0]);
        Assert.Equal((2, 5, ('a', 'd')), result[1]);
        Assert.Equal((5, 9, ('b', 'd')), result[2]);
    }

    [Fact]
    public void MergeThreeChannels_WithMultipleRanges()
    {
        channel1.AddRange(0, 9, '1');
        channel2.AddRange(0, 9, '2');
        channel3.AddRange(0, 9, '3');
        channel3.AddRange(0, 4, 'a');
        channel3.AddRange(4, 9, 'b');

        var result = RangesMerger.Merge(channel1, channel2, channel3);

        Assert.Equal(2, result.Count);
        Assert.Equal((0, 4, ('1', '2', 'a')), result[0]);
        Assert.Equal((4, 9, ('1', '2', 'b')), result[1]);
    }

    [Fact]
    public void MergeTwoRanges_WithNullValues()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 4, null);
        channel2.AddRange(4, 9, 'b');

        var result = RangesMerger.Merge(channel1, channel2);

        Assert.Equal(2, result.Count);
        Assert.Equal((0, 4, ('a', null)), result[0]);
        Assert.Equal((4, 9, ('a', 'b')), result[1]);
    }

    [Fact]
    public void MergeThreeRanges_WithNullValues()
    {
        channel1.AddRange(0, 9, 'a');
        channel2.AddRange(0, 4, 'b');
        channel2.AddRange(4, 6, null);
        channel2.AddRange(6, 9, 'c');
        channel3.AddRange(0, 9, null);

        var result = RangesMerger.Merge(channel1, channel2, channel3);

        Assert.Equal(3, result.Count);
        Assert.Equal((0, 4, ('a', 'b', null)), result[0]);
        Assert.Equal((4, 6, ('a', null, null)), result[1]);
        Assert.Equal((6, 9, ('a', 'c', null)), result[2]);
    }
}

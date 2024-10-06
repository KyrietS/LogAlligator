using LogAlligator.App.Utils.Ranges;

namespace LogAlligator.Tests.Utils;

public class OverlappingRangesTests
{
    private OverlappingRanges<char> sut = new();

    [Fact]
    public void Add_FirstRange()
    {
        sut.AddRange(0, 9, 'a');

        Assert.Single(sut);
        Assert.Equal((0, 9, 'a'), sut[0]);
    }

    [Fact]
    public void Add_FirstRange_WithZeroLength()
    {
        sut.AddRange(4, 4, 'a');

        Assert.Empty(sut);
    }

    [Fact]
    public void Add_Range_WithZeroLength()
    {
        sut.AddRange(0, 9, 'a');
        sut.AddRange(5, 5, 'b');

        Assert.Equal(2, sut.Count);
        Assert.Equal((0, 5, 'a'), sut[0]);
        Assert.Equal((5, 9, 'a'), sut[1]);
    }

    [Fact]
    public void Add_DisjointRange_OnRight()
    {
        sut.AddRange(0, 5, 'a');
        sut.AddRange(7, 9, 'b');

        Assert.Equal(3, sut.Count);
        Assert.Equal((0, 5, 'a'), sut[0]);
        Assert.Equal((5, 7, 'a'), sut[1]);
        Assert.Equal((7, 9, 'b'), sut[2]);
    }

    [Fact]
    public void Add_DisjointRange_OnLeft()
    {
        sut.AddRange(7, 9, 'b');
        sut.AddRange(0, 5, 'a');

        Assert.Equal(3, sut.Count);
        Assert.Equal((0, 5, 'a'), sut[0]);
        Assert.Equal((5, 7, 'b'), sut[1]);
        Assert.Equal((7, 9, 'b'), sut[2]);
    }

    [Fact]
    public void Add_DisjointRange_WithBackground()
    {
        sut.AddRange(0, 9, 'x');
        sut.AddRange(2, 3, 'a');
        sut.AddRange(5, 6, 'b');

        Assert.Equal(5, sut.Count);
        Assert.Equal((0, 2, 'x'), sut[0]);
        Assert.Equal((2, 3, 'a'), sut[1]);
        Assert.Equal((3, 5, 'x'), sut[2]);
        Assert.Equal((5, 6, 'b'), sut[3]);
        Assert.Equal((6, 9, 'x'), sut[4]);
    }

    [Fact]
    public void Add_DisjointRange_WithBackgroundReversed()
    {
        sut.AddRange(0, 9, 'x');
        sut.AddRange(5, 6, 'b');
        sut.AddRange(2, 3, 'a');

        Assert.Equal(5, sut.Count);
        Assert.Equal((0, 2, 'x'), sut[0]);
        Assert.Equal((2, 3, 'a'), sut[1]);
        Assert.Equal((3, 5, 'x'), sut[2]);
        Assert.Equal((5, 6, 'b'), sut[3]);
        Assert.Equal((6, 9, 'x'), sut[4]);
    }

    [Fact]
    public void Add_AdjoiningRange_WithBackground()
    {
        sut.AddRange(0, 9, 'x');
        sut.AddRange(2, 4, 'a');
        sut.AddRange(4, 5, 'b');

        Assert.Equal(4, sut.Count);
        Assert.Equal((0, 2, 'x'), sut[0]);
        Assert.Equal((2, 4, 'a'), sut[1]);
        Assert.Equal((4, 5, 'b'), sut[2]);
        Assert.Equal((5, 9, 'x'), sut[3]);
    }

    [Fact]
    public void Add_AdjoiningRange_WithBackgroundReversed()
    {
        sut.AddRange(0, 9, 'x');
        sut.AddRange(4, 5, 'b');
        sut.AddRange(2, 4, 'a');

        Assert.Equal(4, sut.Count);
        Assert.Equal((0, 2, 'x'), sut[0]);
        Assert.Equal((2, 4, 'a'), sut[1]);
        Assert.Equal((4, 5, 'b'), sut[2]);
        Assert.Equal((5, 9, 'x'), sut[3]);
    }

    [Fact]
    public void Add_AdjoiningRange_OnRight()
    {
        sut.AddRange(0, 5, 'a');
        sut.AddRange(5, 9, 'b');

        Assert.Equal(2, sut.Count);
        Assert.Equal((0, 5, 'a'), sut[0]);
        Assert.Equal((5, 9, 'b'), sut[1]);
    }

    [Fact]
    public void Add_AdjoiningRange_OnLeft()
    {
        sut.AddRange(5, 9, 'b');
        sut.AddRange(0, 5, 'a');

        Assert.Equal(2, sut.Count);
        Assert.Equal((0, 5, 'a'), sut[0]);
        Assert.Equal((5, 9, 'b'), sut[1]);
    }

    [Fact]
    public void Add_OverlapRange_OnRight()
    {
        sut.AddRange(0, 5, 'a');
        sut.AddRange(4, 9, 'b');

        Assert.Equal(2, sut.Count);
        Assert.Equal((0, 4, 'a'), sut[0]);
        Assert.Equal((4, 9, 'b'), sut[1]);
    }

    [Fact]
    public void Add_OverlapRange_OnLeft()
    {
        sut.AddRange(5, 9, 'a');
        sut.AddRange(1, 6, 'b');

        Assert.Equal(2, sut.Count);
        Assert.Equal((1, 6, 'b'), sut[0]);
        Assert.Equal((6, 9, 'a'), sut[1]);
    }

    [Fact]
    public void Add_OverlapRange_AllOver()
    {
        sut.AddRange(2, 5, 'a');
        sut.AddRange(1, 6, 'b');

        Assert.Single(sut);
        Assert.Equal((1, 6, 'b'), sut[0]);
    }

    [Fact]
    public void Add_OverlapRange_Inside()
    {
        sut.AddRange(0, 9, 'a');
        sut.AddRange(4, 6, 'b');

        Assert.Equal(3, sut.Count);
        Assert.Equal((0, 4, 'a'), sut[0]);
        Assert.Equal((4, 6, 'b'), sut[1]);
        Assert.Equal((6, 9, 'a'), sut[2]);
    }

    [Fact]
    public void Add_OverlapRange_Inside_OnRight()
    {
        sut.AddRange(0, 9, 'a');
        sut.AddRange(5, 9, 'b');

        Assert.Equal(2, sut.Count);
        Assert.Equal((0, 5, 'a'), sut[0]);
        Assert.Equal((5, 9, 'b'), sut[1]);
    }

    [Fact]
    public void Add_OverlapRange_Inside_OnLeft()
    {
        sut.AddRange(0, 9, 'a');
        sut.AddRange(0, 5, 'b');

        Assert.Equal(2, sut.Count);
        Assert.Equal((0, 5, 'b'), sut[0]);
        Assert.Equal((5, 9, 'a'), sut[1]);
    }

    [Fact]
    public void Add_OverlapRange_OverTwoAdjoiningRanges()
    {
        sut.AddRange(0, 5, 'a');
        sut.AddRange(5, 9, 'b');
        sut.AddRange(4, 6, 'c');

        Assert.Equal(3, sut.Count);
        Assert.Equal((0, 4, 'a'), sut[0]);
        Assert.Equal((4, 6, 'c'), sut[1]);
        Assert.Equal((6, 9, 'b'), sut[2]);
    }
}

public class OverlappingRangesWithNullValuesTests
{
    private OverlappingRanges<char?> sut = new();

    [Fact]
    public void Add_Range_AllNulls()
    {
        sut.AddRange(0, 9, null);

        Assert.Single(sut);
        Assert.Equal((0, 9, null), sut[0]);
    }

    [Fact]
    public void Add_OverlappingRange_WithNull()
    {
        sut.AddRange(0, 9, 'a');
        sut.AddRange(4, 6, null);

        Assert.Equal(3, sut.Count);
        Assert.Equal((0, 4, 'a'), sut[0]);
        Assert.Equal((4, 6, null), sut[1]);
        Assert.Equal((6, 9, 'a'), sut[2]);
    }

    [Fact]
    public void Add_OverlappingNullRange_WithValue()
    {
        sut.AddRange(0, 9, null);
        sut.AddRange(4, 6, 'a');

        Assert.Equal(3, sut.Count);
        Assert.Equal((0, 4, null), sut[0]);
        Assert.Equal((4, 6, 'a'), sut[1]);
        Assert.Equal((6, 9, null), sut[2]);
    }

}

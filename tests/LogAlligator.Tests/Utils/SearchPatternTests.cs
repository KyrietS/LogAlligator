using LogAlligator.App.Utils;

namespace LogAlligator.Tests.Utils;

public class SearchPatternTests
{
    [Fact]
    public void EmptyPattern_ShouldNotMatchAnything()
    {
        var sut = new SearchPattern("".AsMemory());

        Assert.Empty(sut.MatchAll("".AsMemory()));
        Assert.Empty(sut.MatchAll("a".AsMemory()));
        Assert.Empty(sut.MatchAll("ab".AsMemory()));
    }

    [Fact]
    public void PatternSingleChar_ShouldMatchSingleChar()
    {
        var sut = new SearchPattern("a".AsMemory());

        var result = sut.MatchAll("a".AsMemory());
        Assert.Single(result);
        Assert.Equal((0, 1), result[0]);
    }

    [Fact]
    public void PatternSingleChar_ShouldMatchSingleChars()
    {
        var sut = new SearchPattern("a".AsMemory());

        var result = sut.MatchAll("abba".AsMemory());
        Assert.Equal(2, result.Count);
        Assert.Equal((0, 1), result[0]);
        Assert.Equal((3, 4), result[1]);
    }

    [Fact]
    public void PatternSingleChar_ShouldNotMatchDifferentChar()
    {
        var sut = new SearchPattern("a".AsMemory());

        var result = sut.MatchAll("b".AsMemory());
        Assert.Empty(result);
    }

    [Fact]
    public void PatternMultipleChars_ShouldMatch()
    {
        var sr = new SearchResult(0, 2);

        var sut = new SearchPattern("ab".AsMemory());

        var result = sut.MatchAll("abxab".AsMemory());
        Assert.Equal(2, result.Count);
        Assert.Equal((0, 2), result[0]);
        Assert.Equal((3, 5), result[1]);
    }

    [Fact]
    public void CaseInsensitive_ShouldMatchCapitalText()
    {
        var sut = new SearchPattern("ab".AsMemory(), caseSensitive: false);

        var result = sut.MatchAll("xxABxx".AsMemory());
        Assert.Single(result);
        Assert.Equal((2, 4), result[0]);
    }

    [Fact]
    public void CaseInsensitiveCapitalPattern_ShouldMatch()
    {
        var sut = new SearchPattern("AB".AsMemory(), caseSensitive: false);

        var result = sut.MatchAll("xxabxx".AsMemory());
        Assert.Single(result);
        Assert.Equal((2, 4), result[0]);
    }

    [Fact]
    public void CaseSensitive_ShouldMatch()
    {
        var sut = new SearchPattern("aB".AsMemory(), caseSensitive: true);

        var result = sut.MatchAll("xxaBxx".AsMemory());
        Assert.Single(result);
        Assert.Equal((2, 4), result[0]);
    }

    [Fact]
    public void CaseSensitive_ShouldNotMatch()
    {
        var sut = new SearchPattern("aB".AsMemory(), caseSensitive: true);

        var result = sut.MatchAll("xxabxx".AsMemory());
        Assert.Empty(result);
    }
}

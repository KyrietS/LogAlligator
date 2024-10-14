using LogAlligator.App.Utils;

namespace LogAlligator.Tests.Utils;

public class SearchPatternTests
{
    [Fact]
    public void SingleIsMatchTest()
    {
        var sut = new SearchPattern("a".AsMemory());
        Assert.True(sut.Match("__a__".AsMemory()));
        Assert.False(sut.Match("__b__".AsMemory()));
    }

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

        var result = sut.MatchAll("ab_ab".AsMemory());
        Assert.Equal(2, result.Count);
        Assert.Equal((0, 2), result[0]);
        Assert.Equal((3, 5), result[1]);
    }

    [Fact]
    public void CaseInsensitive_ShouldMatchCapitalText()
    {
        var sut = new SearchPattern("ab".AsMemory(), caseSensitive: false);

        var result = sut.MatchAll("__AB__".AsMemory());
        Assert.Single(result);
        Assert.Equal((2, 4), result[0]);
    }

    [Fact]
    public void CaseInsensitiveCapitalPattern_ShouldMatch()
    {
        var sut = new SearchPattern("AB".AsMemory(), caseSensitive: false);

        var result = sut.MatchAll("__ab__".AsMemory());
        Assert.Single(result);
        Assert.Equal((2, 4), result[0]);
    }

    [Fact]
    public void CaseSensitive_ShouldMatch()
    {
        var sut = new SearchPattern("aB".AsMemory(), caseSensitive: true);

        var result = sut.MatchAll("__aB__".AsMemory());
        Assert.Single(result);
        Assert.Equal((2, 4), result[0]);
    }

    [Fact]
    public void CaseSensitive_ShouldNotMatch()
    {
        var sut = new SearchPattern("aB".AsMemory(), caseSensitive: true);

        var result = sut.MatchAll("__ab__".AsMemory());
        Assert.Empty(result);
    }

    [Fact]
    public void Regex_IsMatch()
    {
        var sut = new SearchPattern("a.b".AsMemory(), regex: true);
        Assert.True(sut.Match("a_b".AsMemory()));
        Assert.False(sut.Match("ab".AsMemory()));
    }

    [Fact]
    public void Regex_Simple_ShouldMatchAnyChar()
    {
        var sut = new SearchPattern("a.b".AsMemory(), regex: true);

        var result = sut.MatchAll("_a_b_".AsMemory());
        Assert.Equal((1, 4), result[0]);
    }

    [Fact]
    public void Regex_Simple_ShouldMatchAnyrCharOrNone()
    {
        var sut = new SearchPattern("a.?b".AsMemory(), regex: true);

        var result = sut.MatchAll("_a_b_".AsMemory());
        Assert.Equal((1, 4), result[0]);
    }

    [Fact]
    public void Regex_Simple_ShouldNotMatch()
    {
        var sut = new SearchPattern("a.b".AsMemory(), regex: true);

        var result = sut.MatchAll("__ab__".AsMemory());
        Assert.Empty(result);
    }

    [Fact]
    public void Regex_Wildcard_ShouldMatch()
    {
        var sut = new SearchPattern("a.*b".AsMemory(), regex: true);

        var result1 = sut.MatchAll("__ab__".AsMemory());
        Assert.Equal( (2, 4), result1[0]);

        var result2 = sut.MatchAll("__a_b__".AsMemory());
        Assert.Equal((2, 5), result2[0]);
    }

    [Fact]
    public void Regex_Wildcard_ShouldMatchTheLongestOccurence()
    {
        var sut = new SearchPattern("a.*b".AsMemory(), regex: true);

        var result = sut.MatchAll("a_ab_b".AsMemory());
        Assert.Equal((0, 6), result[0]);
    }

    [Fact]
    public void Regex_CastSensitive_ShouldNotMatch()
    {
        var sut = new SearchPattern("a.b".AsMemory(), caseSensitive: true, regex: true);

        var result = sut.MatchAll("A_B".AsMemory());
        Assert.Empty(result);
    }

    [Fact]
    public void Regex_CaseInsensitive_ShouldMatch()
    {
        var sut = new SearchPattern("a.b".AsMemory(), caseSensitive: false, regex: true);

        var result = sut.MatchAll("A_B".AsMemory());
        Assert.Equal((0, 3), result[0]);
    }
}

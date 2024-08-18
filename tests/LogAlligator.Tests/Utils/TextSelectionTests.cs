using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogAlligator.App.Utils;

namespace LogAlligator.Tests.Utils;

public class TextSelectionTests
{
    private TextSelection sut = new();

    [Fact]
    public void DefaultTextSelection_ShouldNotGiveAnySelection()
    {
        Assert.Null(sut.GetSelectionAtLine(-1));
        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Null(sut.GetSelectionAtLine(1));
        Assert.Null(sut.GetSelectionAtLine(2));
    }
    [Fact]
    public void BeginWithoutEnd_ShouldGiveZeroWidthSelection()
    {
        sut.SetBegin(0, 0);
        Assert.Equal((0, 0), sut.GetSelectionAtLine(0));

        sut.SetBegin(0, 5);
        Assert.Equal((5, 5), sut.GetSelectionAtLine(0));

        sut.SetBegin(1, 5);
        Assert.Equal((5, 5), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void EndWithoutBegin_ShouldNotGiveAnySelection()
    {
        sut.SetEnd(1, 5);
        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Null(sut.GetSelectionAtLine(1));
        Assert.Null(sut.GetSelectionAtLine(2));
    }

    [Fact]
    public void SetBegin_ShouldResetEnd_AndGiveZeroWidthSelection()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 4);
        sut.SetEnd(lineIndex: 1, charIndex: 6);
        sut.SetBegin(lineIndex: 1, charIndex: 2);

        Assert.Equal((2, 2), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void OneLine_Selection_ShouldGiveValidRange()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 4);
        sut.SetEnd(lineIndex: 1, charIndex: 6);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((4, 6), sut.GetSelectionAtLine(1));
        Assert.Null(sut.GetSelectionAtLine(2));
    }

    [Fact]
    public void OneLine_SelectionTwice_ShouldGiveValidRange()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 2);
        sut.SetEnd(lineIndex: 1, charIndex: 4);
        sut.SetEnd(lineIndex: 1, charIndex: 6);

        Assert.Equal((2, 6), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void OneLine_ReversedSelection_ShouldGiveValidRange()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 6);
        sut.SetEnd(lineIndex: 1, charIndex: 4);

        Assert.Equal((4, 6), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void OneLine_ReversedSelectionTwice_ShouldGiveValidRange()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 6);
        sut.SetEnd(lineIndex: 1, charIndex: 4);
        sut.SetEnd(lineIndex: 1, charIndex: 2);

        Assert.Equal((2, 6), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void OneLine_SelectionLeftmost_ShouldGiveValidRange()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 0);
        sut.SetEnd(lineIndex: 1, charIndex: 4);

        Assert.Equal((0, 4), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void OneLine_SelectionWithNegativeIndex_ShouldGiveValidRange()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 10);
        sut.SetEnd(lineIndex: 1, charIndex: -5);

        Assert.Equal((-5, 10), sut.GetSelectionAtLine(1));
    }

    [Fact]
    public void OneWholeLine_Selection_ShouldGiveUndefinedEnding()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 0);
        sut.SetEnd(lineIndex: 2, charIndex: 0);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((0, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, 0), sut.GetSelectionAtLine(2));
    }

    [Fact]
    public void TwoLines_Selection_ShouldGiveValidRanges()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 4);
        sut.SetEnd(lineIndex: 2, charIndex: 6);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((4, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, 6), sut.GetSelectionAtLine(2));
        Assert.Null(sut.GetSelectionAtLine(3));
    }

    [Fact]
    public void TwoLines_SelectionReversed_ShouldGiveValidRanges()
    {
        sut.SetBegin(lineIndex: 2, charIndex: 6);
        sut.SetEnd(lineIndex: 1, charIndex: 4);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((4, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, 6), sut.GetSelectionAtLine(2));
        Assert.Null(sut.GetSelectionAtLine(3));
    }

    [Fact]
    public void TwoLines_LeftmostToMiddle_ShouldGiveValidRanges()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 0);
        sut.SetEnd(lineIndex: 2, charIndex: 5);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((0, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, 5), sut.GetSelectionAtLine(2));
    }

    [Fact]
    public void TwoLines_MiddleToRightmost_ShouldGiveValidRanges()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 5);
        sut.SetEnd(lineIndex: 3, charIndex: 0);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((5, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, null), sut.GetSelectionAtLine(2));
        Assert.Equal((null, 0), sut.GetSelectionAtLine(3));
        Assert.Null(sut.GetSelectionAtLine(4));
    }

    [Fact]
    public void SetBeginLine_ShouldSelectWholeLine()
    {
        sut.SetBeginLine(1);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((0, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, 0), sut.GetSelectionAtLine(2));
        Assert.Null(sut.GetSelectionAtLine(3));
    }

    [Fact]
    public void SetEndLine_BeforeSetBeginLine_ShouldSelectWholeLine()
    {
        sut.SetEndLine(1);

        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Equal((0, null), sut.GetSelectionAtLine(1));
        Assert.Equal((null, 0), sut.GetSelectionAtLine(2));
        Assert.Null(sut.GetSelectionAtLine(3));
    }

    [Fact]
    public void SetEndLine_BelowBeginLine_ShouldGiveValidRange()
    {
        sut.SetBeginLine(1);
        sut.SetEndLine(2);

        Assert.Null(sut[0]);
        Assert.Equal((0, null), sut[1]);
        Assert.Equal((null, null), sut[2]);
        Assert.Equal((null, 0), sut[3]);
        Assert.Null(sut[4]);
    }

    [Fact]
    public void SetEndLine_AboveBeginLine_ShouldGiveValidRange()
    {
        sut.SetBeginLine(2);
        sut.SetEndLine(1);

        Assert.Null(sut[0]);
        Assert.Equal((0, null), sut[1]);
        Assert.Equal((null, null), sut[2]);
        Assert.Equal((null, 0), sut[3]);
        Assert.Null(sut[4]);
    }

    [Fact]
    public void SetEndLine_ToSameLineAsBegin_ShouldGiveValidRange()
    {
        sut.SetBeginLine(1);
        sut.SetEndLine(1);

        Assert.Null(sut[0]);
        Assert.Equal((0, null), sut[1]);
        Assert.Equal((null, 0), sut[2]);
        Assert.Null(sut[3]);
    }

    [Fact]
    public void Clear_ShouldClearSelection()
    {
        sut.SetBegin(lineIndex: 1, charIndex: 2);
        sut.SetEnd(lineIndex: 1, charIndex: 6);
        sut.Clear();

        Assert.Null(sut.GetSelectionAtLine(-1));
        Assert.Null(sut.GetSelectionAtLine(0));
        Assert.Null(sut.GetSelectionAtLine(1));
        Assert.Null(sut.GetSelectionAtLine(2));
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogAlligator.App.Utils;

internal struct TextSelection
{
    private record struct TextPosition(int LineIndex, int CharIndex)
    {
        public TextPosition() : this(-1, -1) { }
        public static bool operator <(TextPosition left, TextPosition right)
        {
            if (left.LineIndex < right.LineIndex) return true;
            else if (left.LineIndex > right.LineIndex) return false;
            else if (left.CharIndex < right.CharIndex) return true;
            else return false;
        }
        public static bool operator >(TextPosition left, TextPosition right)
        {
            return right < left;
        }
    }
    private TextPosition begin = new();
    private TextPosition end = new();
    private int? beginLine = null;

    private TextPosition SelectionStart => end < begin ? end : begin;
    private TextPosition SelectionStop => begin > end ? begin : end;

    public TextSelection()
    {
    }

    public (int LineIndex, int CharIndex) Begin => (begin.LineIndex, begin.CharIndex);
    public (int LineIndex, int CharIndex) End => (end.LineIndex, end.CharIndex);

    /// <summary>
    /// Sets the starting point of a selection. This will also set the ending point.
    /// </summary>
    public void SetBegin(int lineIndex, int charIndex)
    {
        begin = new TextPosition(lineIndex, charIndex);
        end = begin;
    }

    /// <summary>
    /// Sets the ending point of a selection. You should call this function <b>after</b> <see cref="SetBegin"/>.
    /// </summary>
    public void SetEnd(int lineIndex, int charIndex)
    {
        end = new TextPosition(lineIndex, charIndex);
    }

    /// <summary>
    /// Selects whole line. This function starts seletion of multiple lines, see <see cref="SetEndLine"/>.
    /// </summary>
    /// <remarks>After <see cref="Clear"/> is called you should call <c>SetBeginLine</c> again before selecting lines.</remarks>
    public void SetBeginLine(int lineIndex)
    {
        SetBegin(lineIndex, 0);
        SetEnd(lineIndex + 1, 0);
        beginLine = lineIndex;
    }

    /// <summary>
    /// Sets end to multi-line selection. Prior to this function you should call <see cref="SetBeginLine"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Raised when you call this function before calling <see cref="SetBeginLine"/></exception>
    public void SetEndLine(int lineIndex)
    {
        if (beginLine == null)
            throw new InvalidOperationException("You should call SetBeginLine first!");

        var firstLine = Math.Min(lineIndex, beginLine.Value);
        var lastLine = Math.Max(lineIndex, beginLine.Value);
        SetBegin(firstLine, 0);
        SetEnd(lastLine + 1, 0);
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void Clear()
    {
        begin = new();
        end = new();
        beginLine = null;
    }

    /// <summary>
    /// Gets the selection range for a particular line.
    /// </summary>
    /// <param name="lineIndex">Line index that is being checked.</param>
    /// <returns>
    /// <para>A selection range for a line with index of <paramref name="lineIndex"/>.</para>
    /// <list type="bullet">
    /// <item><description><c>null</c> when selection does not overlap with given line at all</description></item>
    /// <item><description><c>(null, end)</c> when selection begins before give line and ends at position <c>end</c></description></item>
    /// <item><description><c>(begin, null)</c> when selection begins at position <c>begin</c> and ends after given line</description></item>
    /// <item><description><c>(null, null)</c> when selection begins before given line and ends after it (whole line is selected)</description></item>
    /// <item><description><c>(begin, end)</c> when selection begins at position <c>begin</c> and ends at <c>end</c></description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Result tuple <c>(begin end)</c> represents a range from <c>begin</c> inclusive to <c>end</c> exclusive.
    /// If both <c>begin</c> and <c>end</c> are not <c>null</c> it is guaranteed that <c>begin <= end</c>
    /// </remarks>
    public (int? Begin, int? End)? GetSelectionAtLine(int lineIndex)
    {
        var start = SelectionStart;
        var stop = SelectionStop;

        if (start.LineIndex == -1 || stop.LineIndex == -1)
            return null;

        if (lineIndex == start.LineIndex && start.LineIndex == stop.LineIndex)
        {
            return (start.CharIndex, stop.CharIndex);
        }
        if (lineIndex == start.LineIndex)
        {
            return (start.CharIndex, null);
        }
        if (lineIndex == stop.LineIndex)
        {
            return (null, stop.CharIndex);
        }
        if (start.LineIndex < lineIndex && lineIndex < stop.LineIndex)
        {
            return (null, null);
        }
        return null;
    }

    public (int? Begin, int? End)? this[int lineIndex] => GetSelectionAtLine(lineIndex);
}

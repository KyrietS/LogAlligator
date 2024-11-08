using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LogAlligator.App.Utils
{
    public record struct SearchResult(int Begin, int End);

    /// <summary>
    /// SearchPattern holds the search criteria and provides methods to match the pattern in a line.
    /// Important: DO NOT create new instances ad hoc when using regex, as it will compile the regex every time.
    /// Compiling the regex is extremely expensive.
    /// </summary>
    public class SearchPattern
    {
        // FIXME: Why is this ReadOnlyMemory instead of a string? This class is not performance critical.
        public ReadOnlyMemory<char> Pattern { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool IsRegex { get; set; }

        private Regex? _regex;
        private Regex Regex => _regex ??= new(Pattern.ToString(), GetRegexOptions());

        public bool Equals(ReadOnlyMemory<char> otherPattern)
        {
            return Pattern.Span.SequenceEqual(otherPattern.Span);
        }

        public override string ToString()
        {
            return Pattern.ToString();
        }

        public SearchPattern(ReadOnlyMemory<char> pattern, bool caseSensitive = false, bool regex = false)
        {
            Pattern = pattern;
            IsCaseSensitive = caseSensitive;
            IsRegex = regex;
        }

        public bool Match(ReadOnlyMemory<char> line)
        {
            if (Pattern.Length == 0 || line.Length == 0)
                return false;

            if (IsRegex)
                return MatchRegex(line);
            else
                return MatchClassic(line);
        }

        public List<(int Begin, int End)> MatchAll(ReadOnlyMemory<char> line)
        {
            if (Pattern.Length == 0 || line.Length == 0)
                return new();

            if (IsRegex)
                return MatchAllRegex(line);
            else
                return MatchAllClassic(line);
        }

        private bool MatchRegex(ReadOnlyMemory<char> line)
        {
            return Regex.IsMatch(line.ToString());
        }

        private bool MatchClassic(ReadOnlyMemory<char> line)
        {
            return line.Span.Contains(Pattern.Span, GetClassicStringComparison());
        }

        private List<(int, int)> MatchAllRegex(ReadOnlyMemory<char> line)
        {
            List<(int, int)> results = new();

            var matches = Regex.Matches(line.ToString());
            foreach(Match match in matches)
            {
                results.Add((match.Index, match.Index + match.Length));
            }

            return results;
        }

        private List<(int, int)> MatchAllClassic(ReadOnlyMemory<char> line)
        {
            List<(int, int)> results = new();
            var lineSpan = line.Span;

            for (int index = 0; index < line.Length; index += Pattern.Length)
            {
                int foundPos = lineSpan.IndexOf(Pattern.Span, GetClassicStringComparison());
                if (foundPos == -1)
                    break;

                index += foundPos;
                results.Add((index, index + Pattern.Length));
                lineSpan = lineSpan.Slice(foundPos + Pattern.Length);
            }

            return results;
        }

        private RegexOptions GetRegexOptions()
        {
            RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant;
            return options | (IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        }

        private StringComparison GetClassicStringComparison()
        {
            return IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        }
    }
}

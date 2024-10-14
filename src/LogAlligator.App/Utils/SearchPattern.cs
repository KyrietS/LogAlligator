using System;
using System.Collections.Generic;

namespace LogAlligator.App.Utils
{
    public record struct SearchResult(int Begin, int End);

    public class SearchPattern
    {
        public ReadOnlyMemory<char> Pattern { get; set; }
        public bool IsCaseSensitive { get; set; }
        public bool IsRegex { get; set; }

        public bool Equals(ReadOnlyMemory<char> otherPattern)
        {
            return Pattern.Span == otherPattern.Span;
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

        public List<(int Begin, int End)> MatchAll(ReadOnlyMemory<char> line)
        {
            List<(int, int)> results = new();
            
            if (Pattern.Length == 0 || line.Length == 0)
                return results;

            var lineSpan = line.Span;

            for(int index = 0; index < line.Length; index += Pattern.Length)
            {
                int foundPos = lineSpan.IndexOf(Pattern.Span, GetStringComparison());
                if (foundPos == -1)
                    break;

                index += foundPos;
                results.Add((index, index + Pattern.Length));
                lineSpan = lineSpan.Slice(foundPos + Pattern.Length);
            }

            return results;
        }

        private StringComparison GetStringComparison()
        {
            return IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        }
    }
}

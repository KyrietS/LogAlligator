using System;
using System.Collections;
using System.Collections.Generic;
using Serilog;

namespace LogAlligator.App.Utils
{
    public record class Bookmark
    {
        public int Id { get; init; }
        public required string Name { get; set; }
        public int LineNumber { get; set; }
    }

    // FIXME: All bookmarks should be sorted by line number. Be careful with context menu when you do this.
    public class Bookmarks : IEnumerable<Bookmark>
    {
        private readonly List<Bookmark> _bookmarks = [];
        private int _nextId = 1;
        public event EventHandler? OnChange;

        public void Add(string name, int lineNumber)
        {
            if (name.Length == 0)
            {
                Log.Warning("Bookmark name cannot be empty");
                return;
            }

            _bookmarks.Add(new Bookmark { Id = _nextId++, Name = name, LineNumber = lineNumber });
            SortBookmarks();
            OnChange?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveById(int id)
        {
            _bookmarks.RemoveAll(b => b.Id == id);
            OnChange?.Invoke(this, EventArgs.Empty);
        }

        public IEnumerator<Bookmark> GetEnumerator()
        {
            return _bookmarks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void SortBookmarks()
        {
            _bookmarks.Sort((b1, b2) => b1.LineNumber.CompareTo(b2.LineNumber));
        }
    }
}

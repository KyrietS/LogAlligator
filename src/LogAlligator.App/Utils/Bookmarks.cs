using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace LogAlligator.App.Utils
{
    public record struct Bookmark
    {
        public string Name { get; set; }
        public int LineNumber { get; set; }
    }

    public class Bookmarks : IEnumerable<Bookmark>
    {
        private List<Bookmark> _bookmarks = new();
        public event EventHandler? OnChange;

        public void Add(string name, int lineNumber)
        {
            if (name.Length == 0)
            {
                Log.Warning("Bookmark name cannot be empty");
                return;
            }

            _bookmarks.Add(new Bookmark { Name = name, LineNumber = lineNumber });
            OnChange?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveAt(int index)
        {
            _bookmarks.RemoveAt(index);
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
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Context
{
    /// <summary>
    /// This class is some kind of a ViewModel for FileView. It holds all the data that is needed for the view.
    /// 
    /// Note: DO NOT subscribe to events from properties in this class, because it may outlive your view.
    /// The only view that should be subscribed to events is the FileView itself.
    /// </summary>
    public class FileViewContext
    {
        public Highlights Highlights { get; init; } = new();
        public Bookmarks Bookmarks { get; init; } = new();
    }
}

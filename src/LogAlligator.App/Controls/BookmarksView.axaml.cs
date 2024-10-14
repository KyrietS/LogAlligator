using System;
using System.Collections.Generic;
using Avalonia.Controls;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Controls;

public partial class BookmarksView : UserControl
{
    private Bookmarks? _bookmarks;

    public event EventHandler<int>? JumpToBookmark;

    public BookmarksView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            ListOfBookmarks.Items.Add(new ListBoxItem { Content = "First bookmark" });
            ListOfBookmarks.Items.Add(new ListBoxItem { Content = "Second bookmark" });
            ListOfBookmarks.Items.Add(new ListBoxItem { Content = "Third bookmark" });
        }
    }

    public void Initialize(Bookmarks bookmarks)
    {
        _bookmarks = bookmarks;
        _bookmarks.OnChange += (sender, args) => Refresh();
    }

    public void OnDelete()
    {
        if (ListOfBookmarks.SelectedItems is null)
            return;

        List<int> bookmarksToRemove = [];
        int index = 0;
        foreach (var selectedItem in ListOfBookmarks.SelectedItems)
        {
            if (selectedItem is ListBoxItem item)
            {
                bookmarksToRemove.Add(index);
            }

            index++;
        }

        foreach (int indexToRemove in bookmarksToRemove)
        {
            _bookmarks!.RemoveAt(indexToRemove);
        }
    }

    private void Refresh()
    {
        ListOfBookmarks.Items.Clear();
        foreach (var item in PrepareListOfBookmarks())
        {
            ListOfBookmarks.Items.Add(item);
        }
    }

    private List<ListBoxItem> PrepareListOfBookmarks()
    {
        List<ListBoxItem> items = new();

        foreach (var bookmark in _bookmarks ?? [])
        {
            var item = new ListBoxItem { Content = bookmark.Name };
            item.ContextMenu = BuildContextMenu(items.Count);
            item.DoubleTapped += (_, _) => JumpToBookmark?.Invoke(this, bookmark.LineNumber);
            items.Add(item);
        }

        return items;
    }

    private ContextMenu BuildContextMenu(int index)
    {
        var delete = new MenuItem { Header = "Delete" };
        delete.Click += (_, _) => _bookmarks?.RemoveAt(index);

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(delete);
        return contextMenu;
    }
}

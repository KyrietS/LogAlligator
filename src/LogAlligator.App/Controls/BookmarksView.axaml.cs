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
        _bookmarks.OnChange += (_, _) => Refresh();
    }

    public void OnDelete()
    {
        if (ListOfBookmarks.SelectedItems is null)
            return;

        List<int> bookmarkIdsToRemove = [];
        foreach (var selectedItem in ListOfBookmarks.SelectedItems)
        {
            if (selectedItem is ListBoxItem item)
            {
                Bookmark bookmark = (Bookmark)item.DataContext!;
                bookmarkIdsToRemove.Add(bookmark.Id);
            }
        }

        foreach (int bookmarkId in bookmarkIdsToRemove)
        {
            _bookmarks!.RemoveById(bookmarkId);
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
        List<ListBoxItem> items = [];

        foreach (var bookmark in _bookmarks ?? [])
        {
            var item = new ListBoxItem { Content = bookmark.Name, DataContext = bookmark, ContextMenu = BuildContextMenu() };
            item.DoubleTapped += (_, _) => JumpToBookmark?.Invoke(this, bookmark.LineNumber);
            items.Add(item);
        }

        return items;
    }

    private ContextMenu BuildContextMenu()
    {
        var delete = new MenuItem { Header = "Remove" };
        delete.Click += (_, _) => OnDelete();

        var contextMenu = new ContextMenu();
        contextMenu.Items.Add(delete);
        return contextMenu;
    }
}

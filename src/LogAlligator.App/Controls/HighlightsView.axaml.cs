using System;
using System.Collections.Generic;
using Avalonia.Controls;
using LogAlligator.App.Utils;

namespace LogAlligator.App.Controls;

public partial class HighlightsView : UserControl
{
    private Highlights? _highlights;
    public HighlightsView()
    {
        InitializeComponent();

        if (Design.IsDesignMode)
        {
            ListOfHighlights.Items.Add(new ListBoxItem { Content = "First pattern" });
            ListOfHighlights.Items.Add(new ListBoxItem { Content = "Second pattern" });
            ListOfHighlights.Items.Add(new ListBoxItem { Content = "Third pattern" });
        }
    }

    public void Initialize(Highlights highlights)
    {
        _highlights = highlights;
        _highlights.OnChange += (_, _) => Refresh();
    }

    public void OnDelete()
    {
        if (ListOfHighlights.SelectedItems is null)
            return;

        List<string> patternsToRemove = [];
        foreach (var selectedItem in ListOfHighlights.SelectedItems)
        {
            if (selectedItem is ListBoxItem item)
            {
                patternsToRemove.Add(item.Content?.ToString() ?? "");
            }
        }

        foreach (var pattern in patternsToRemove)
        {
            _highlights?.Remove(pattern.AsMemory());
        }
    }

    private void Refresh()
    {
        ListOfHighlights.Items.Clear();
        foreach (var item in PrepareListOfHighlights())
        {
            ListOfHighlights.Items.Add(item);
        }
    }

    private List<ListBoxItem> PrepareListOfHighlights()
    {
        List<ListBoxItem> items = [];

        foreach (var highlight in _highlights ?? [])
        {
            var item = new ListBoxItem { Content = highlight.ToString(), ContextMenu = BuildContextMenu() };
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

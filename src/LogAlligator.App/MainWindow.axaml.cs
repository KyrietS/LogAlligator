using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using LogAlligator.App.Controls;
using Serilog;

namespace LogAlligator.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        Application.Current!.ActualThemeVariantChanged += (_, _) => RefreshAllToolTips();
        FileTabs.Background = new SolidColorBrush(Colors.Transparent);
        FileTabs.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        FileTabs.AddHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(FileTabs, true);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = GetFileFromDragEvent(e);
        if (file == null)
            return;
        
        Log.Debug("Dropped file: {FileName}", file.Name);
        AddFileTab(file);
    }
    
    private void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = GetFileFromDragEvent(e) != null ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private IStorageFile? GetFileFromDragEvent(DragEventArgs e)
    {
        var files = e.Data.GetFiles()?.ToList() ?? [];
        if (files.Count != 1)
        {
            return null;
        }

        return files[0] as IStorageFile;
    }
    
    public async void OnLoadData()
    {
        if (Design.IsDesignMode) return;

        // Load wide.txt file (for testing)
        Uri filePath = new Uri(Path.GetFullPath("wide.txt"));
        var file = await StorageProvider.TryGetFileFromPathAsync(filePath);

        if (file == null)
        {
            Log.Warning("File {FilePath} not found", filePath);
            return;
        }
        AddFileTab(file);
    }

    public void OnSwitchTheme()
    {
        Application.Current!.RequestedThemeVariant = Application.Current!.ActualThemeVariant == ThemeVariant.Light
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
    }

    public void OnExit()
    {
        Close();
    }

    private async void MenuBar_Open_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await this.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open log file", AllowMultiple = false, FileTypeFilter = null
        });

        if (files.Count < 1)
        {
            return;
        }
        if (files.Count > 1)
        {
            Log.Warning("More than one file was selected. Only the first one will be opened.");
        }

        AddFileTab(files[0]);
    }

    private void AddFileTab(IStorageFile file)
    {
        var fileTab = new TabItem { Header = file.Name, Content = new FileView{FilePath = file.Path} };
        ToolTip.SetTip(fileTab, file.Path.AbsolutePath);
        FileTabs.SelectedIndex = FileTabs.Items.Add(fileTab);
    }

    /// <summary>
    /// When tooltip is displayed and after that the theme is changes, the theme of the tooltip is not updated.
    /// To fix this I set all tooltips again to force the update.
    /// </summary>
    private void RefreshAllToolTips()
    {
        // Iterate over all logical descendants of the window
        foreach (var child in this.GetLogicalDescendants())
        {
            // Only a Control can have a ToolTip
            if (child is Control control)
            {
                var toolTip = ToolTip.GetTip(control);
                if (toolTip is string toolTipString)
                {
                    // Set a new Tip with the same value to force the ToolTip to update
                    // Note: you cannot use ToolTip.SetTip(control, toolTipString) because it will not work
                    ToolTip.SetTip(control, new String(toolTipString));
                }
            }
        }
    }
}

using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
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
    private WindowNotificationManager _windowNotificationManager;
    public MainWindow()
    {
        InitializeComponent();
        
        FileTabs.Background = new SolidColorBrush(Colors.Transparent);
        FileTabs.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        FileTabs.AddHandler(DragDrop.DropEvent, OnDrop);
        DragDrop.SetAllowDrop(FileTabs, true);

        _windowNotificationManager = new WindowNotificationManager(this); // Will be used in the future
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var file = GetFileFromDragEvent(e);
        if (file == null)
            return;
        
        this.Activate(); // Bring window to front
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
        var fileView = new FileView { FilePath = file.Path };
        var fileTab = new TabItem { Header = file.Name, Content = fileView };
        
        fileView.RemovalRequested += (_, _) => OnTabRequestedRemoval(fileTab);
        fileTab.ContextMenu = CreateFileTabContextMenu(fileTab);
        
        ToolTip.SetTip(fileTab, file.Path.AbsolutePath);
        FileTabs.SelectedIndex = FileTabs.Items.Add(fileTab);
    }

    private ContextMenu CreateFileTabContextMenu(TabItem tab)
    {
        var ctxMenu = new ContextMenu();
        var closeMenuItem = new MenuItem { Header = "Close file view" };
        closeMenuItem.Click += (_, _) => OnTabRequestedRemoval(tab);
        ctxMenu.Items.Add(closeMenuItem);
        return ctxMenu;
    }
    
    private void OnTabRequestedRemoval(TabItem tab)
    {
        Log.Debug($"Removing tab {tab.Header}");

        FileTabs.Items.Remove(tab);
    }
}

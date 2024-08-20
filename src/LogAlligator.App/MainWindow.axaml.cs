using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    
    public void OnLoadData()
    {
        if (Design.IsDesignMode) return;

        // Load wide.txt file
        Uri filePath = new Uri(Path.GetFullPath("wide.txt"));
        FileTabs.Items.Insert(0, new TabItem { Header = "Sample data", Content = new FileView{FilePath = filePath} });
        FileTabs.SelectedIndex = 0;
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
        FileTabs.SelectedIndex = FileTabs.Items.Add(fileTab);
    }
}

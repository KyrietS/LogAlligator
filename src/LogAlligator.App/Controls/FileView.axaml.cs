using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using LogAlligator.App.LineProvider;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class FileView : UserControl
{
    private bool _isLoaded = false;
    private readonly Uri? _filePath;
    public Uri FilePath
    {
        get => _filePath ?? throw new InvalidOperationException("File path is not set");
        init
        {
            if (!value.IsAbsoluteUri)
                value = new Uri(Path.GetFullPath(value.OriginalString));
            _filePath = value;
        }
    }

    public FileView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Design.IsDesignMode) return;
        
        if (FilePath.IsFile && !_isLoaded)
        {
            LoadFile();
            _isLoaded = true;
        }
    }

    private void LoadFile()
    {
        try
        {
            LoadData();
        }
        catch (Exception e)
        {
            Log.Warning("Error when tried to load a file: {FilePath}", FilePath);
            Log.Warning("Exception: {Exception}", e);
            ShowMessageBoxFileCouldNotBeLoaded();
        }
    }

    private void LoadData()
    {
        var lineProvider = new StupidFileLineProvider(FilePath);
        lineProvider.LoadData(); // TODO: Do this in a background thread
        Log.Debug("Loaded {Lines} lines from {FilePath}", lineProvider.Count, FilePath);
        LogView.SetLineProvider(lineProvider);
    }

    private void ShowMessageBoxFileCouldNotBeLoaded()
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Error", "File could not be loaded\nCheck logs for more details", ButtonEnum.Ok,
                    Icon.Error, WindowStartupLocation.CenterOwner);
            await box.ShowWindowDialogAsync(this.VisualRoot as Window);
        });
    }
}

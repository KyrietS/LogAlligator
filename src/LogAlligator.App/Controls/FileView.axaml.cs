using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class FileView : UserControl
{
    private bool _isLoaded = false;
    public string FilePath { get; }

    // TODO: Instead of constructor with string, use a property
    public FileView(string filePath)
    {
        FilePath = filePath;
        InitializeComponent();
    }
    public FileView() : this(string.Empty) { }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (FilePath != "" && !_isLoaded)
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
        var lines = File.ReadAllLines(FilePath);
        Log.Debug("Loaded {Lines} lines from {FilePath}", lines.Length, FilePath);
        LogView.SetData(lines);
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

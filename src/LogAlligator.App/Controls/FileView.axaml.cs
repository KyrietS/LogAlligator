using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Serilog;

namespace LogAlligator.App.Controls;

public partial class FileView : UserControl
{
    private string FilePath { get; set; } = "";

    public FileView()
    {
        InitializeComponent();
    }

    public void LoadFile(string filePath)
    {
        try
        {
            FilePath = filePath;
            LoadData();
        }
        catch (Exception e)
        {
            FilePath = "";
            Log.Warning("Error when tried to load a file: {FilePath}", filePath);
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

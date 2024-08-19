using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

namespace LogAlligator.App.Controls;

public partial class FileView : UserControl
{
    public string FilePath { get; private set; } = "";

    public FileView()
    {
        InitializeComponent();
    }

    public void LoadFile(string filePath)
    {
        try
        {
            LoadData(filePath);
            FilePath = filePath;
        }
        catch (Exception e)
        {
            FilePath = "";
            Debug.WriteLine("Error when tried to load a file: " + filePath);
            Debug.WriteLine(e);
            ShowMessageBoxFileCouldNotBeLoaded();
        }

    }

    private void LoadData(string filePath)
    {
        // var lines = File.ReadAllLines(FilePath);
        throw new Exception("Not implemented");
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

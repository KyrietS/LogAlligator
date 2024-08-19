using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using LogAlligator.App.Controls;

namespace LogAlligator.App;

public partial class MainWindow : Window
{
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

        var file = files[0];
        var fileView = new FileView();
        var fileTab = new TabItem
        {
            Header = file.Name,
            Content = fileView
        };
        FileTabs.SelectedIndex = FileTabs.Items.Add(fileTab);
        fileView.LoadFile(file.Path.AbsolutePath);
    }

    public MainWindow()
    {
        InitializeComponent();
    }

    public void OnLoadData()
    {
        if (Design.IsDesignMode) return;

        FileView.LoadFile("wide.txt");
    }

    public void OnSwitchTheme()
    {
        Application.Current!.RequestedThemeVariant = Application.Current!.ActualThemeVariant == ThemeVariant.Light ?
            ThemeVariant.Dark : ThemeVariant.Light;
    }

    public void OnExit()
    {
        Close();
    }
}

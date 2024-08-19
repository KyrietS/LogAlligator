using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace LogAlligator.App;

public partial class MainWindow : Window
{
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

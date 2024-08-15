using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using System.IO;

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

        var lines = File.ReadAllLines("wide.txt");
        //var lines = File.ReadAllLines("pan-tadeusz.txt");
        TextView.SetText(lines);
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
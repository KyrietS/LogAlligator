using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using LogAlligator.App;

namespace LogAlligator.Tests;

public class MainWindowTests
{
    private readonly Window window = new MainWindow()
    {
        Width = 500,
        Height = 500
    };

    [AvaloniaFact]
    public void CloseWindowUsingKeyboard()
    {
        bool isClosed = false;
        window.Closed += (_, _) => isClosed = true;

        window.Show();

        // Alt
        window.KeyPressQwerty(PhysicalKey.AltLeft, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.AltLeft, RawInputModifiers.None);
        // F
        window.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.F, RawInputModifiers.None);
        // E
        window.KeyPressQwerty(PhysicalKey.E, RawInputModifiers.None);

        Assert.True(isClosed);
    }
}

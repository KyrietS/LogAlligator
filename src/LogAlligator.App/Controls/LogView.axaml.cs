using Avalonia.Controls;

namespace LogAlligator.App.Controls;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();
    }

    public void SetData(string[] data)
    {
        TextView.SetText(data);
    }
}

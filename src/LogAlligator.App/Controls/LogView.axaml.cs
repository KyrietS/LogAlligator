using Avalonia.Controls;
using LogAlligator.App.LineProvider;

namespace LogAlligator.App.Controls;

public partial class LogView : UserControl
{
    public LogView()
    {
        InitializeComponent();
    }

    public void SetLineProvider(ILineProvider lineProvider)
    {
        TextView.SetLineProvider(lineProvider);
    }
}

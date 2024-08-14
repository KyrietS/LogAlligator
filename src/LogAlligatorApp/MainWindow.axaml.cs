using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using System.IO;

namespace LogAlligator
{
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

        public void OnExit()
        {
            Close();
        }
    }
}
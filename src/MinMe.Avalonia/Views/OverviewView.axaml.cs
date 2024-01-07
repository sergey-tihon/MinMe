using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinMe.Avalonia.Views
{
    public partial class OverviewView : UserControl
    {
        public OverviewView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

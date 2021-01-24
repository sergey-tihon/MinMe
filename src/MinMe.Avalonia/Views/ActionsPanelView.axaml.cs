using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinMe.Avalonia.Views
{
    public class ActionsPanelView : UserControl
    {
        public ActionsPanelView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

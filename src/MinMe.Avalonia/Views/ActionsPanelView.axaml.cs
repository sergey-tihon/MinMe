using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinMe.Avalonia.Views
{
    public partial class ActionsPanelView : UserControl
    {
        public ActionsPanelView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

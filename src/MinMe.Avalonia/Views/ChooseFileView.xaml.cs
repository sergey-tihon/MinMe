using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinMe.Avalonia.Views
{
    public class ChooseFileView : UserControl
    {
        public ChooseFileView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}

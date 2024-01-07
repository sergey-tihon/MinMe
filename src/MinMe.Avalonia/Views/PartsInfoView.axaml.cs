using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace MinMe.Avalonia.Views;

public partial class PartsInfoView : UserControl
{
    public PartsInfoView()
    {
        this.InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
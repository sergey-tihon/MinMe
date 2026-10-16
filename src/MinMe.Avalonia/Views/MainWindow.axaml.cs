using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using MinMe.Avalonia.ViewModels;

namespace MinMe.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }


    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        // Open file dialog directly after app start
        if (Application.Current is App { Host: not null } app)
        {
            var vm = app.Host.Services.GetService<ActionsPanelViewModel>();
            vm!.OpenCommand.Execute().Subscribe();
        }
    }
}

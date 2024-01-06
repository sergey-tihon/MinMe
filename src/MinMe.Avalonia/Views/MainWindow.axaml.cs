using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using MinMe.Avalonia.ViewModels;

namespace MinMe.Avalonia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Open file dialog directly after app start
            if (Application.Current is App app && app.Host is not null)
            {
                var vm = app.Host.Services.GetService<ActionsPanelViewModel>();
                vm!.OpenCommand.Execute().Subscribe();
            }
        }
    }
}

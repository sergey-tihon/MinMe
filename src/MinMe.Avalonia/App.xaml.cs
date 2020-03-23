using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MinMe.Avalonia.Services;
using MinMe.Avalonia.ViewModels;
using MinMe.Avalonia.Views;
using System;

namespace MinMe.Avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public IHost? Host { get; private set; }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            Host = CreateHost();
            Host.Start();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Exit += Desktop_Exit;

                desktop.MainWindow = Host.Services.GetService<MainWindow>();
                desktop.MainWindow.Content = Host.Services.GetService<MainViewModel>();
            }
        }

        private async void Desktop_Exit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
        {
            if (Host is null)
                return;

            using (Host)
            {
                await Host.StopAsync(TimeSpan.FromSeconds(5));
            }
        }

        public static IHost CreateHost() =>
            Microsoft.Extensions.Hosting.Host
                .CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    var window = new MainWindow();
                    services.AddSingleton(window);
                    services.AddSingleton<INotificationManager>(
                        new WindowNotificationManager(window)
                        {
                            Position = NotificationPosition.TopRight,
                            MaxItems = 3
                        });

                    // Services
                    services.AddSingleton<StateService>();
                    // ViewModels
                    services.AddSingleton<MainViewModel>();
                    services.AddSingleton<OverviewViewModel>();
                    services.AddSingleton<ActionsPanelViewModel>();
                    services.AddSingleton<SlidesInfoViewModel>();
                    services.AddSingleton<PartsInfoViewModel>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .Build();
    }
}
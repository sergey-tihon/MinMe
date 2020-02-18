using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using MinMe.Avalonia.ViewModels;
using MinMe.Avalonia.Views;

namespace MinMe.Avalonia
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            base.OnFrameworkInitializationCompleted();

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                var notificationArea = new WindowNotificationManager(desktop.MainWindow)
                {
                    Position = NotificationPosition.TopRight,
                    MaxItems = 3
                };
                desktop.MainWindow.DataContext = new MainWindowViewModel(notificationArea);
            }
        }
    }
}
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using ReactiveUI;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia.Controls.Notifications;

using MinMe.Analyzers;

namespace MinMe.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private INotificationManager _notificationManager;
        private ViewModelBase _content;
        public MainWindowViewModel(INotificationManager notificationManager)
        {
            //_content = Content = new ChooseFileViewModel();
            _notificationManager = notificationManager;
            _content = Content = new PowerPointViewModel(_notificationManager);
        }

        public ViewModelBase Content
        {
            get => _content;
            private set => this.RaiseAndSetIfChanged(ref _content, value);
        }
    }
}

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
            _content = Content = new ChooseFileViewModel();
            _notificationManager = notificationManager;

            OpenCommand = ReactiveCommand.CreateFromTask(OpenFile);

            //PowerPoint = new PowerPointViewModel();
        }

        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        //public PowerPointViewModel PowerPoint { get; }

        public ViewModelBase Content
        {
            get => _content;
            private set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        private async Task OpenFile()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Choose File",
                AllowMultiple = false,
                Filters = {
                    new FileDialogFilter() {
                        Name = "PowerPoint files (*.pptx)",
                        Extensions = {"pptx"}
                    }
                }
            };

            var dialogResult = await openFileDialog.ShowAsync(GetWindow());

            var fileName = dialogResult.FirstOrDefault();
            if (fileName is { })
            {
                using var analyzer = new PowerPointAnalyzer(fileName);
                var fileContentInfo = analyzer.Analyze();
                var stream = analyzer.GetThumbnail();
                Content = new PowerPointViewModel(_notificationManager, fileContentInfo, stream);
            }
        }
    }
}

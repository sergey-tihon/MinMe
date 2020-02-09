using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using MinMe.Core.PowerPoint;
using ReactiveUI;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace MinMe.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        ViewModelBase _content;
        public MainWindowViewModel()
        {
            _content = Content = new ChooseFileViewModel();

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
                        Name = "PowePoint files (*.pptx)",
                        Extensions = {"pptx"}
                    }
                }
            };

            var applicationLifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var dialogResult = await openFileDialog.ShowAsync(applicationLifetime.MainWindow);

            var fileName = dialogResult.FirstOrDefault();
            if (fileName is { })
            {
                using var analyzer = new PowerPointAnalyzer(fileName);
                var fileContentInfo = analyzer.Analyze();
                var stream = analyzer.GetThumbnail();
                Content = new PowerPointViewModel(fileContentInfo, stream);
            }
        }
    }
}

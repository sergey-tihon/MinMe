using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using Clippit;
using Clippit.PowerPoint;

using MinMe.Core.Model;
using MinMe.Core.PowerPoint;

using ReactiveUI;

namespace MinMe.Avalonia.ViewModels
{
    public class PowerPointViewModel : ViewModelBase
    {
        public PowerPointViewModel()
        {
            FileName = "Please open file";
            OpenCommand = ReactiveCommand.CreateFromTask(OpenFile);

            IObservable<bool> actionsEnabled = this.WhenAnyValue(
                x => x.FileContentInfo,
                (FileContentInfo x) => x != null);
            OptimizeCommand = ReactiveCommand.Create(() => { }, actionsEnabled);
            PublishCommand = ReactiveCommand.Create(PublishSlides, actionsEnabled);
        }

        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> OptimizeCommand { get; }
        public ReactiveCommand<Unit, Unit> PublishCommand { get; }

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        private FileContentInfo _fileContentInfo;
        public FileContentInfo FileContentInfo
        {
            get => _fileContentInfo;
            set => this.RaiseAndSetIfChanged(ref _fileContentInfo, value);
        }

        private async Task OpenFile()
        {
            var openFileDialog = new OpenFileDialog();
            var applicationLifetime = Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            var dialogResult =  await openFileDialog.ShowAsync(applicationLifetime.MainWindow);
            FileName = dialogResult.FirstOrDefault();

            using var analyzer = new PowerPointAnalyzer(FileName);
            FileContentInfo = analyzer.Analyze();
        }

        private void PublishSlides()
        {
            var fileName = FileName;
            var presentation = new PmlDocument(fileName);
            var slides = PresentationBuilder.PublishSlides(presentation);

            var targetDir = new FileInfo(fileName).DirectoryName;
            foreach (var slide in slides)
            {
                var targetPath = Path.Combine(targetDir, Path.GetFileName(slide.FileName));
                slide.SaveAs(targetPath);
            }
        }
    }
}
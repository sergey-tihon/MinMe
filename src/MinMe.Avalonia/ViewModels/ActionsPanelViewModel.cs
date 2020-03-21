using Clippit;
using Clippit.PowerPoint;
using MinMe.Analyzers;
using MinMe.Analyzers.Model;
using MinMe.Avalonia.Services;
using ReactiveUI;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using MinMe.Optimizers;
using Notification = Avalonia.Controls.Notifications.Notification;
using Microsoft.Extensions.Logging;

namespace MinMe.Avalonia.ViewModels
{
    class ActionsPanelViewModel : ViewModelBase
    {
        private readonly INotificationManager _notificationManager;
        private readonly StateService _stateService;
        private readonly ILogger<ActionsPanelViewModel> _logger;

        public ActionsPanelViewModel(StateService stateService, INotificationManager notificationManager,
            ILogger<ActionsPanelViewModel> logger)
        {
            _notificationManager = notificationManager;
            _stateService = stateService;
            _logger = logger;

            _fileContentInfo = _stateService.FileContentInfo
                .ToProperty(this, nameof(FileContentInfo));

            OpenCommand = ReactiveCommand.CreateFromTask(OpenFile);

            var fileAnalyzed = _stateService.FileContentInfo.Select(x => x is { });
            OptimizeCommand = ReactiveCommand.CreateFromTask(Optimize, fileAnalyzed);

            var moreThanOneSlide = _stateService.FileContentInfo.Select(x => x?.Slides?.Count > 1);
            PublishCommand = ReactiveCommand.CreateFromTask(PublishSlides, moreThanOneSlide);
        }

        private readonly ObservableAsPropertyHelper<FileContentInfo?> _fileContentInfo;
        public FileContentInfo? FileContentInfo => _fileContentInfo.Value;

        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> OptimizeCommand { get; }
        public ReactiveCommand<Unit, Unit> PublishCommand { get; }


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
                _stateService.ChangeState(analyzer.Analyze());
                //AssignThumbnail(analyzer.GetThumbnail());
            }
        }

        //private Bitmap? DefaultThumbnail;
        //private void AssignThumbnail(Stream? thumbnail)
        //{
        //    if (thumbnail is { })
        //    {
        //        Thumbnail = new Bitmap(thumbnail);
        //    }
        //    else
        //    {
        //        if (DefaultThumbnail is null)
        //        {
        //            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
        //            var uri = new Uri("avares://MinMe.Avalonia/Assets/PowerPoint.png");
        //            DefaultThumbnail = new Bitmap(assets.Open(uri));
        //        }
        //        Thumbnail = DefaultThumbnail;
        //    }
        //}

        private async Task PublishSlides()
        {
            if (FileContentInfo is null)
            {
                _logger.LogError("FileContentInfo shoud not be null");
                return;
            }

            var openFileDialog = new OpenFolderDialog
            {
                Title = "Select folder",
                //Directory = new FileInfo(FileContentInfo.FileName).DirectoryName,
            };

            var targetDir = await openFileDialog.ShowAsync(GetWindow());
            if (!string.IsNullOrEmpty(targetDir))
            {
                var presentation = new PmlDocument(FileContentInfo.FileName);
                var slides = PresentationBuilder.PublishSlides(presentation);
                var count = 0;
                foreach (var slide in slides)
                {
                    var targetPath = Path.Combine(targetDir, Path.GetFileName(slide.FileName));
                    slide.SaveAs(targetPath);
                    count++;
                }

                _notificationManager.Show(new Notification("Slides are published", $"Successfully published {count} slides."));
            }
        }

        private async Task Optimize()
        {
            if (FileContentInfo is null)
            {
                _logger.LogError("FileContentInfo shoud not be null");
                return;
            }

            var sourceFileInfo = new FileInfo(FileContentInfo.FileName);
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Optimized File",
                InitialFileName = sourceFileInfo.Name,
                Directory = sourceFileInfo.DirectoryName,
                Filters = {
                    new FileDialogFilter {
                        Name = "PowerPoint files (*.pptx)",
                        Extensions = {"pptx"}
                    }
                }
            };

            var resultFileName = await saveFileDialog.ShowAsync(GetWindow());
            if (resultFileName is null)
                return;

            await using (var originalStream = new FileStream(FileContentInfo.FileName, FileMode.Open, FileAccess.Read))
            {
                var extension = Path.GetExtension(FileContentInfo.FileName);
                await using var transformedStream = new ImageOptimizer().Transform(extension, originalStream);

                if (transformedStream is { })
                {
                    await using var targetFile = File.Create(resultFileName);
                    transformedStream.CopyTo(targetFile);
                }
            }

            var initialFileSize = sourceFileInfo.Length;
            var resultFileSize = new FileInfo(resultFileName).Length;
            var compression = 100.0 * (initialFileSize - resultFileSize) / initialFileSize;

            _notificationManager.Show(new Notification("Presentation is optimized",
                $"Compressed presentation size from {Helpers.PrintFileSize(initialFileSize)} to {Helpers.PrintFileSize(resultFileSize)} (compression {compression:0.00}%)."));
        }

    }
}

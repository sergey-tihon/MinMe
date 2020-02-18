using System;
using System.IO;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Clippit;
using Clippit.PowerPoint;

using MinMe.Analyzers;
using MinMe.Analyzers.Model;
using MinMe.Optimizers;

using ReactiveUI;

using Notification = Avalonia.Controls.Notifications.Notification;

namespace MinMe.Avalonia.ViewModels
{
    public class PowerPointViewModel : ViewModelBase
    {
        public PowerPointViewModel(INotificationManager notificationManager, FileContentInfo fileContentInfo, Stream? thumbnail)
        {
            FileContentInfo = fileContentInfo;

            if (thumbnail is { }) {
                Thumbnail = new Bitmap(thumbnail);
            }
            else
            {
                var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                var uri = new Uri("avares://MinMe.Avalonia/Assets/PowerPoint.png");
                Thumbnail = new Bitmap(assets.Open(uri));
            }

            OptimizeCommand = ReactiveCommand.CreateFromTask(Optimize);

            var moreThanOneSlide = this.WhenAnyValue(x => x.FileContentInfo,
                (FileContentInfo x) => x.Slides?.Count > 1);
            PublishCommand = ReactiveCommand.CreateFromTask(PublishSlides, moreThanOneSlide);

            _notificationManager = notificationManager;
        }


        private readonly INotificationManager _notificationManager;

        public ReactiveCommand<Unit, Unit> OptimizeCommand { get; }
        public ReactiveCommand<Unit, Unit> PublishCommand { get; }

        public FileContentInfo FileContentInfo { get; }

        public string FileName => Path.GetFileNameWithoutExtension(FileContentInfo.FileName);
        public string PublishActionName {
            get {
                var count = FileContentInfo.Slides?.Count ?? 0;
                return count <= 1 ? "Publish Slides" : $"Publish Slides ({count})";
            }
        }

        public Bitmap Thumbnail { get; }

        private async Task PublishSlides()
        {
            var openFileDialog = new OpenFolderDialog
            {
                Title = "Select folder",
                //Directory = new FileInfo(FileContentInfo.FileName).DirectoryName,
            };

            var targetDir = await openFileDialog.ShowAsync(GetWindow());
            if (targetDir is { })
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

                if (transformedStream is {})
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
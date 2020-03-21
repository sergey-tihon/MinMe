using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Collections;
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
        public PowerPointViewModel(INotificationManager notificationManager)
        {
            _notificationManager = notificationManager;

            OpenCommand = ReactiveCommand.CreateFromTask(OpenFile);

            var fileAnalyzed = this.WhenAnyValue(x => x.FileContentInfo, (FileContentInfo? x) => x != null);
            OptimizeCommand = ReactiveCommand.CreateFromTask(Optimize, fileAnalyzed);

            var moreThanOneSlide = this.WhenAnyValue(x => x.FileContentInfo, (FileContentInfo? x) => x?.Slides?.Count > 1);
            PublishCommand = ReactiveCommand.CreateFromTask(PublishSlides, moreThanOneSlide);

            _fileName = this
                .WhenAnyValue(x => x.FileContentInfo)
                .Select(x => x is null ? "" : Path.GetFileNameWithoutExtension(x.FileName))
                .ToProperty(this, nameof(FileName), "", deferSubscription: true);

            _slideTitles = this
                .WhenAnyValue(x => x.FileContentInfo)
                .Select(x => new DataGridCollectionView(x?.Slides ?? new List<SlideInfo>()))
                .ToProperty(this, nameof(SlideTitles), deferSubscription: true);

            _parts = this
                .WhenAnyValue(x => x.FileContentInfo)
                .Select(x => {
                    var view = new DataGridCollectionView(x?.Parts ?? new List<PartInfo>());
                    view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(PartInfo.PartType)));
                    view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(PartInfo.Size), true));
                    return view;
                })
                .ToProperty(this, nameof(Parts), deferSubscription: true);

            OpenCommand.Execute().Subscribe();
        }


        private readonly INotificationManager _notificationManager;

        public ReactiveCommand<Unit, Unit> OpenCommand { get; }
        public ReactiveCommand<Unit, Unit> OptimizeCommand { get; }
        public ReactiveCommand<Unit, Unit> PublishCommand { get; }

        public FileContentInfo? _fileContentInfo;
        public FileContentInfo? FileContentInfo
        {
            get => _fileContentInfo;
            private set => this.RaiseAndSetIfChanged(ref _fileContentInfo, value);
        }

        private Bitmap? _thumbnail;
        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

        private readonly ObservableAsPropertyHelper<string> _fileName;
        public string FileName => _fileName.Value;

        private readonly ObservableAsPropertyHelper<DataGridCollectionView?> _slideTitles;
        public DataGridCollectionView? SlideTitles => _slideTitles.Value;

        private readonly ObservableAsPropertyHelper<DataGridCollectionView?> _parts;
        public DataGridCollectionView? Parts => _parts.Value;

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
                FileContentInfo = analyzer.Analyze();
                AssignThumbnail(analyzer.GetThumbnail());
            }
        }

        private Bitmap? DefaultThumbnail;
        private void AssignThumbnail(Stream? thumbnail)
        {
            if (thumbnail is { })
            {
                Thumbnail = new Bitmap(thumbnail);
            }
            else
            {
                if (DefaultThumbnail is null) {
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    var uri = new Uri("avares://MinMe.Avalonia/Assets/PowerPoint.png");
                    DefaultThumbnail = new Bitmap(assets.Open(uri));
                }
                Thumbnail = DefaultThumbnail;
            }
        }

        private async Task PublishSlides()
        {
            var openFileDialog = new OpenFolderDialog
            {
                Title = "Select folder",
                //Directory = new FileInfo(FileContentInfo.FileName).DirectoryName,
            };

            var targetDir = await openFileDialog.ShowAsync(GetWindow());
            if (!string.IsNullOrEmpty(targetDir))
            {
                var presentation = new PmlDocument(FileContentInfo?.FileName);
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
            var sourceFileInfo = new FileInfo(FileContentInfo?.FileName);
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
using Clippit.PowerPoint;
using MinMe.Analyzers;
using MinMe.Analyzers.Model;
using MinMe.Avalonia.Services;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using MinMe.Optimizers;
using Notification = Avalonia.Controls.Notifications.Notification;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Drawing;
using Avalonia.Platform.Storage;

namespace MinMe.Avalonia.ViewModels;

class ActionsPanelViewModel : ViewModelBase
{
    private static readonly IReadOnlyList<FilePickerFileType> PowerPointFileType =
    [
        new FilePickerFileType("PowerPoint files (*.pptx)") { Patterns = ["*.pptx"] }
    ];
    
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

        var fileAnalyzed = _stateService.FileContentInfo.Select(x => x is not null);
        OptimizeCommand = ReactiveCommand.CreateFromTask(Optimize, fileAnalyzed);

        var moreThanOneSlide = _stateService.FileContentInfo.Select(x => x?.Slides.Count > 1);
        PublishCommand = ReactiveCommand.CreateFromTask(PublishSlides, moreThanOneSlide);

        PublishModes = new ObservableCollection<PublishMode> {
            new("2160p (4K)", new ImageOptimizerOptions {
                ExpectedScreenSize = new Size(3840, 2160),
                DegreeOfParallelism = Environment.ProcessorCount
            }),
            new("1080p (Full HD)", new ImageOptimizerOptions {
                ExpectedScreenSize = new Size(1920, 1080),
                DegreeOfParallelism = Environment.ProcessorCount
            }),
            new("720p (HD ready)", new ImageOptimizerOptions {
                ExpectedScreenSize = new Size(1280, 720),
                DegreeOfParallelism = Environment.ProcessorCount
            }),
        };
        _selectedMode = PublishModes[1];
    }

    public class PublishMode(string name, ImageOptimizerOptions options)
    {
        public string Name { get; } = name;
        public ImageOptimizerOptions Options { get; } = options;

        public override string ToString() => Name;
    }

    private readonly ObservableAsPropertyHelper<FileContentInfo?> _fileContentInfo;
    public FileContentInfo? FileContentInfo => _fileContentInfo.Value;

    public ObservableCollection<PublishMode> PublishModes { get; }

    private PublishMode _selectedMode;
    public PublishMode SelectedMode
    {
        get => _selectedMode;
        set => this.RaiseAndSetIfChanged(ref _selectedMode, value);
    }

    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> OptimizeCommand { get; }
    public ReactiveCommand<Unit, Unit> PublishCommand { get; }


    private async Task OpenFile()
    {
        var files = await GetStorageProvider().OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Choose File",
                AllowMultiple = false,
                FileTypeFilter = PowerPointFileType
            }
        );

        if (files.Count > 0)
        {
            FileContentInfo? state = null;
            await _stateService.Run(() =>
            {
                var file = files[0];
                using var analyzer = new PowerPointAnalyzer(file.TryGetLocalPath()!);
                state = analyzer.Analyze();
            });
            if (state is { }) 
                _stateService.SetState(state);
        }
    }

    private async Task PublishSlides()
    {
        if (FileContentInfo is null)
        {
            _logger.LogError("FileContentInfo should not be null");
            return;
        }

        var folders = await GetStorageProvider().OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var targetDir = folders[0].TryGetLocalPath()!;
            var count = 0;
            await _stateService.Run(() =>
            {
                var presentation = new PmlDocument(FileContentInfo.FileName);
                var slides = PresentationBuilder.PublishSlides(presentation);
                foreach (var slide in slides)
                {
                    var targetPath = Path.Combine(targetDir, Path.GetFileName(slide.FileName));
                    slide.SaveAs(targetPath);
                    count++;
                }
            });
            _notificationManager.Show(new Notification("Slides are published", $"Successfully published {count} slides."));
        }
    }

    private async Task Optimize()
    {
        if (FileContentInfo is null)
        {
            _logger.LogError("FileContentInfo should not be null");
            return;
        }

        var sourceFileInfo = new FileInfo(FileContentInfo.FileName);

        var storageProvider = GetStorageProvider();
        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Optimized File",
            DefaultExtension = "pptx",
            SuggestedFileName = sourceFileInfo.Name,
            SuggestedStartLocation = await storageProvider.TryGetFolderFromPathAsync(sourceFileInfo.DirectoryName!),
            FileTypeChoices = PowerPointFileType
        });

        if (file is null)
            return;

        var targetFilePath = file.TryGetLocalPath()!;
        await _stateService.RunTask(async () =>
        {
            await using var originalStream = new FileStream(FileContentInfo.FileName, FileMode.Open, FileAccess.Read);
            var extension = Path.GetExtension(FileContentInfo.FileName);
            
            await using var transformedStream = new ImageOptimizer()
                .Transform(extension, originalStream, out _, SelectedMode.Options);

            await using var targetFile = File.Create(targetFilePath);
            await transformedStream.CopyToAsync(targetFile);
        });

        var initialFileSize = sourceFileInfo.Length;
        var resultFileSize = new FileInfo(targetFilePath).Length;
        var compression = 100.0 * (initialFileSize - resultFileSize) / initialFileSize;

        _notificationManager.Show(new Notification("Presentation is optimized",
            $"Compressed presentation size from {Helpers.PrintFileSize(initialFileSize)} to {Helpers.PrintFileSize(resultFileSize)} (compression {compression:0.00}%)."));
    }

}

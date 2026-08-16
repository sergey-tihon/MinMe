using System.Reactive.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DocumentFormat.OpenXml.Packaging;
using MinMe.Avalonia.Services;
using ReactiveUI;

namespace MinMe.Avalonia.ViewModels;

class OverviewViewModel : ViewModelBase
{
    public OverviewViewModel(StateService stateService)
    {
        _fileName = stateService
            .FileContentInfo.Select(x =>
                x is null ? "" : Path.GetFileNameWithoutExtension(x.FileName)
            )
            .ToProperty(this, nameof(FileName), "", deferSubscription: true);

        var uri = new Uri("avares://MinMe.Avalonia/Assets/PowerPoint.png");
        var defaultThumbnail = new Bitmap(AssetLoader.Open(uri));

        _thumbnail = stateService
            .FileContentInfo.Select(GetThumbnail)
            .ToProperty(this, nameof(Thumbnail), defaultThumbnail, deferSubscription: true);

        _hasFile = stateService
            .FileContentInfo.Select(x => x is not null)
            .ToProperty(this, nameof(HasFile), deferSubscription: true);

        _slideCount = stateService
            .FileContentInfo.Select(x => x?.Slides.Count ?? 0)
            .ToProperty(this, nameof(SlideCount), deferSubscription: true);
        _partCount = stateService
            .FileContentInfo.Select(x => x?.Parts.Count ?? 0)
            .ToProperty(this, nameof(PartCount), deferSubscription: true);

        Bitmap GetThumbnail(Analyzers.Model.FileContentInfo? x)
        {
            if (x is null)
                return defaultThumbnail;
            try
            {
                using var fileStream = File.Open(x.FileName, FileMode.Open);
                var openSettings = new OpenSettings { AutoSave = false };
                using PresentationDocument document = PresentationDocument.Open(
                    fileStream,
                    false,
                    openSettings
                );
                if (document.ThumbnailPart is null)
                    return defaultThumbnail;

                using var ms = new MemoryStream();
                using var stream = document.ThumbnailPart.GetStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                return new Bitmap(ms);
            }
            catch (Exception)
            {
                return defaultThumbnail;
            }
        }
        ;
    }

    private readonly ObservableAsPropertyHelper<Bitmap> _thumbnail;
    public Bitmap Thumbnail => _thumbnail.Value;

    private readonly ObservableAsPropertyHelper<bool> _hasFile;
    public bool HasFile => _hasFile.Value;

    private readonly ObservableAsPropertyHelper<int> _slideCount;
    public int SlideCount => _slideCount.Value;

    private readonly ObservableAsPropertyHelper<int> _partCount;
    public int PartCount => _partCount.Value;

    private readonly ObservableAsPropertyHelper<string> _fileName;
    public string FileName => _fileName.Value;
}

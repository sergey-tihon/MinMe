using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DocumentFormat.OpenXml.Packaging;
using MinMe.Avalonia.Services;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive.Linq;

namespace MinMe.Avalonia.ViewModels
{
    class OverviewViewModel : ViewModelBase
    {
        public OverviewViewModel(StateService stateService)
        {
            _fileName = stateService.FileContentInfo
                .Select(x => x is null ? "" : Path.GetFileNameWithoutExtension(x.FileName))
                .ToProperty(this, nameof(FileName), "", deferSubscription: true);


            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
            var uri = new Uri("avares://MinMe.Avalonia/Assets/PowerPoint.png");
            var defaultThumbnail = new Bitmap(assets.Open(uri));

            _thumbnail = stateService.FileContentInfo.Select(GetThumbnail)
                .ToProperty(this, nameof(Thumbnail), defaultThumbnail, deferSubscription: true);

            Bitmap GetThumbnail(Analyzers.Model.FileContentInfo x)
            {
                try
                {
                    using var fileStream = File.Open(x.FileName, FileMode.Open);
                    var openSettings = new OpenSettings { AutoSave = false };
                    using PresentationDocument document = PresentationDocument.Open(fileStream, false, openSettings);
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
            };
        }

        private readonly ObservableAsPropertyHelper<Bitmap> _thumbnail;
        public Bitmap Thumbnail => _thumbnail.Value;

        private readonly ObservableAsPropertyHelper<string> _fileName;
        public string FileName => _fileName.Value;
    }
}

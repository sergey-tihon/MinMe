using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Collections;
using Avalonia.Media.Imaging;
using MinMe.Analyzers.Model;
using MinMe.Avalonia.Services;

using ReactiveUI;

namespace MinMe.Avalonia.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        public MainViewModel(StateService stateService,
            ActionsPanelViewModel actionsPanelViewModel,
            SlidesInfoViewModel slideInfoViewModel,
            PartsInfoViewModel partsInfoViewModel)
        {
            ActionsPanelViewModel = actionsPanelViewModel;
            SlidesInfoViewModel = slideInfoViewModel;
            PartsInfoViewModel = partsInfoViewModel;

            _fileContentInfo = stateService.FileContentInfo
                .ToProperty(this, nameof(FileContentInfo), deferSubscription: true);

            _fileName = stateService.FileContentInfo
                .Select(x => x is null ? "" : Path.GetFileNameWithoutExtension(x.FileName))
                .ToProperty(this, nameof(FileName), "", deferSubscription: true);
        }

        public ActionsPanelViewModel ActionsPanelViewModel { get; }
        public SlidesInfoViewModel SlidesInfoViewModel { get; }
        public PartsInfoViewModel PartsInfoViewModel { get; }

        private readonly ObservableAsPropertyHelper<FileContentInfo?> _fileContentInfo;
        public FileContentInfo? FileContentInfo => _fileContentInfo.Value;

        private Bitmap? _thumbnail;
        public Bitmap? Thumbnail
        {
            get => _thumbnail;
            private set => this.RaiseAndSetIfChanged(ref _thumbnail, value);
        }

        private readonly ObservableAsPropertyHelper<string> _fileName;
        public string FileName => _fileName.Value;
    }
}
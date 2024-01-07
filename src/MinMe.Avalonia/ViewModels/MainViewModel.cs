using MinMe.Analyzers.Model;
using MinMe.Avalonia.Services;

using ReactiveUI;

namespace MinMe.Avalonia.ViewModels
{
    class MainViewModel : ViewModelBase
    {
        public MainViewModel(StateService stateService,
            ActionsPanelViewModel actionsPanelViewModel,
            OverviewViewModel overviewViewModel,
            SlidesInfoViewModel slideInfoViewModel,
            PartsInfoViewModel partsInfoViewModel)
        {
            ActionsPanelViewModel = actionsPanelViewModel;
            OverviewViewModel = overviewViewModel;
            SlidesInfoViewModel = slideInfoViewModel;
            PartsInfoViewModel = partsInfoViewModel;

            _isBusy = stateService.IsBusy
                .ToProperty(this, nameof(IsBusy), deferSubscription: true);
            _fileContentInfo = stateService.FileContentInfo
                .ToProperty(this, nameof(FileContentInfo), deferSubscription: true);
        }

        public ActionsPanelViewModel ActionsPanelViewModel { get; }
        public OverviewViewModel OverviewViewModel { get; }
        public SlidesInfoViewModel SlidesInfoViewModel { get; }
        public PartsInfoViewModel PartsInfoViewModel { get; }

        private readonly ObservableAsPropertyHelper<bool> _isBusy;
        public bool IsBusy => _isBusy.Value;

        private readonly ObservableAsPropertyHelper<FileContentInfo?> _fileContentInfo;
        public FileContentInfo? FileContentInfo => _fileContentInfo.Value;
    }
}
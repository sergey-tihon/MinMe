using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ReactiveUI;
using ElectronNET.API;
using MinMe.Blazor.Services;

namespace MinMe.Blazor.ViewModels
{
    public class PublishSlidesViewModel : ReactiveObject
    {
        private readonly DocumentService _documentService;
        private readonly NotificationService _notificationService;

        public string Placeholder =
            HybridSupport.IsElectronActive
                ? "Current file" : "Drop file here";

        private string _fileName = "";
        public string FileName
        {
            get => _fileName;
            set => this.RaiseAndSetIfChanged(ref _fileName, value);
        }

        private Core.Model.FileContentInfo? _fileContentInfo;
        public Core.Model.FileContentInfo? FileContentInfo
        {
            get => _fileContentInfo;
            set => this.RaiseAndSetIfChanged(ref _fileContentInfo, value);
        }


        public PublishSlidesViewModel(DocumentService documentService, NotificationService notificationService)
        {
            _documentService = documentService;
            _notificationService = notificationService;

            ChooseFile = ReactiveCommand.CreateFromTask(ChooseFileImpl);
            PublishSlides = ReactiveCommand.Create(PublishSlidesImpl);
        }

        public ReactiveCommand<Unit, Unit> ChooseFile { get; }
        public ReactiveCommand<Unit, Unit> PublishSlides { get; }

        private async Task ChooseFileImpl()
        {
            if (HybridSupport.IsElectronActive)
            {
                FileName = await _documentService.OpenFile();
            }

            if (!File.Exists(FileName))
            {
                _notificationService.ShowError("File not found", FileName);
            }
            else
            {
                //await Task.Run(() => {
                    using var analyzer = new Core.PowerPoint.PowerPointAnalyzer(FileName);
                    FileContentInfo = analyzer.Analyze();
                //});
            }
        }

        private void PublishSlidesImpl()
        {
            var count = _documentService.PublishSlides(FileName);
            _notificationService.ShowSuccess("Success", $"Extracted {count} slide");
        }
    }
}

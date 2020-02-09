using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Clippit;
using Clippit.PowerPoint;

using MinMe.Core.Model;
using MinMe.Core.PowerPoint;

using ReactiveUI;

namespace MinMe.Avalonia.ViewModels
{
    public class PowerPointViewModel : ViewModelBase
    {
        public PowerPointViewModel(FileContentInfo fileContentInfo, Stream? thumbnail)
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

            OptimizeCommand = ReactiveCommand.Create(() => { });

            var moreThanOneSlide = this.WhenAnyValue(x => x.FileContentInfo,
                (FileContentInfo x) => x.Slides?.Count > 1);
            PublishCommand = ReactiveCommand.CreateFromTask(PublishSlides, moreThanOneSlide);
        }

        public ReactiveCommand<Unit, Unit> OptimizeCommand { get; }
        public ReactiveCommand<Unit, Unit> PublishCommand { get; }

        public FileContentInfo FileContentInfo { get; }
        public string FileName => Path.GetFileName(FileContentInfo.FileName);
        public Bitmap Thumbnail { get; }

        private async Task PublishSlides()
        {
            var openFileDialog = new OpenFolderDialog
            {
                Title = "Select folder",
                //Directory = new FileInfo(FileContentInfo.FileName).DirectoryName,
            };

            var window = GetWindow();
            var targetDir = await openFileDialog.ShowAsync(window);

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

                var wnd = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                    "Slides Published", $"Successfully published {count} slides.",
                    icon: MessageBox.Avalonia.Enums.Icon.Success,
                    style: MessageBox.Avalonia.Enums.Style.Windows);
                await wnd.ShowDialog(window);
            }
        }
    }
}
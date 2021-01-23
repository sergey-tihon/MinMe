using Avalonia.Collections;
using MinMe.Analyzers.Model;
using MinMe.Avalonia.Models;
using MinMe.Avalonia.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace MinMe.Avalonia.ViewModels
{
    class SlidesInfoViewModel : ViewModelBase
    {
        public SlidesInfoViewModel(StateService stateService)
        {
            _slides = stateService.FileContentInfo
                .Select(ToDataGridCollection)
                .ToProperty(this, nameof(Slides), deferSubscription: false);
        }

        private readonly ObservableAsPropertyHelper<DataGridCollectionView> _slides;
        public DataGridCollectionView? Slides => _slides.Value;

        private DataGridCollectionView ToDataGridCollection(FileContentInfo? fileContentInfoOpt)
        {
            IEnumerable<SlideInfoRow> rows;
            if (fileContentInfoOpt is { } fileContentInfo)
            {
                var partSizes = fileContentInfo.Parts
                    .ToDictionary(x => x.Name, x => x.Size, StringComparer.InvariantCultureIgnoreCase);

                var slideSizes = fileContentInfo.Parts
                    .ToDictionary(x => x.Name, x => x.Size, StringComparer.InvariantCultureIgnoreCase);
                foreach(var kv in fileContentInfo.PartUsages)
                {
                    var size = partSizes[kv.Key];
                    foreach (var usage in kv.Value.OfType<Reference>())
                        slideSizes[usage.From.OriginalString] += size;
                }

                rows = fileContentInfo.Slides
                    .OrderBy(x => x.Number)
                    .Select(x =>
                    {
                        if (!slideSizes.TryGetValue(x.FileName, out var size))
                            size = 0;
                        return new SlideInfoRow(x, size);
                    });
            }
            else
            {
                rows = new List<SlideInfoRow>();
            }

            return new DataGridCollectionView(rows);
        }
    }
}

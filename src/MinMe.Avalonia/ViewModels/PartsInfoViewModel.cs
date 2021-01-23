using Avalonia.Collections;
using MinMe.Analyzers.Model;
using MinMe.Avalonia.Models;
using MinMe.Avalonia.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;

namespace MinMe.Avalonia.ViewModels
{
    class PartsInfoViewModel : ViewModelBase
    {
        public PartsInfoViewModel(StateService stateService)
        {
            _parts = stateService.FileContentInfo
                .Select(ToDataGridCollection)
                .ToProperty(this, nameof(Parts), deferSubscription: false);
        }

        private readonly ObservableAsPropertyHelper<DataGridCollectionView> _parts;
        public DataGridCollectionView? Parts => _parts.Value;

        private DataGridCollectionView ToDataGridCollection(FileContentInfo? fileContentInfoOpt)
        {
            IEnumerable<PartInfoRow> rows;
            if (fileContentInfoOpt is { } fileContentInfo)
            {
                var slideToNumber = fileContentInfo.Slides
                    .ToDictionary(x => x.FileName, x => x.Number, StringComparer.InvariantCultureIgnoreCase);

                rows = fileContentInfo.Parts.Select(x =>
                {
                    var usageInfo = String.Empty;
                    if (fileContentInfo.PartUsages.TryGetValue(x.Name, out var usage))
                    {
                        var refs = usage.OfType<Reference>().ToList();
                        if (refs.Count > 0)
                        {
                            var ids = refs
                                .Select(r =>
                                    slideToNumber.TryGetValue(r.From.OriginalString, out int number)
                                    ? $"Slide #{number}" : r.From.OriginalString)
                                .Distinct()
                                .OrderBy(x => x);
                            usageInfo = string.Join(", ", ids);
                        }
                    }
                    return new PartInfoRow(x, usageInfo);
                });
            } else {
                rows = new List<PartInfoRow>();
            }

            var view = new DataGridCollectionView(rows);
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(PartInfoRow.PartType)));
            view.SortDescriptions.Add(DataGridSortDescription.FromPath(nameof(PartInfoRow.Size), ListSortDirection.Descending));
            return view;
        }
    }
}

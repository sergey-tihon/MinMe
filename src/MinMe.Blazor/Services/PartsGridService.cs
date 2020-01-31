using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http;
using Blazor.Fluxor;
using GridShared;
using GridShared.Utility;
using GridMvc.Server;
using MinMe.Core.Model;
using MinMe.Blazor.Store.Document;

namespace MinMe.Blazor.Services
{
    public class PartsGridService
    {
        public IState<DocumentState> DocumentState { get; }

        public PartsGridService(IState<DocumentState> state)
        {
            DocumentState = state;
        }

        public ItemsDTO<PartInfo> GetOrdersGridRows(
            Action<IGridColumnCollection<PartInfo>> columns,
            QueryDictionary<StringValues> query)
        {
            var allItems = DocumentState.Value.FileContentInfo?.Parts ?? new List<PartInfo>();
            var server = new GridServer<PartInfo>(allItems, new QueryCollection(query),
                true, "partsGrid", columns, 3000)
                .Sortable()
                .Groupable();

            // return items to displays
            return server.ItemsToDisplay;
        }
    }
}

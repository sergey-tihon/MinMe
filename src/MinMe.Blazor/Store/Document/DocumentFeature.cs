using System;
using Blazor.Fluxor;

namespace MinMe.Blazor.Store.Document
{
    public class DocumentFeature : Feature<DocumentState>
    {
        public override string GetName() => "Document";

        protected override DocumentState GetInitialState()
            => new DocumentState(null);
    }
}

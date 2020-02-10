using System;
using Blazor.Fluxor;

using MinMe.Analyzers;

namespace MinMe.Blazor.Store.Document
{
    public class OpenFileReducer : Reducer<DocumentState, OpenFileAction>
    {
        public override DocumentState Reduce(DocumentState state, OpenFileAction action)
        {
            using var analyzer = new PowerPointAnalyzer(action.FileName);
            var fileContentInfo = analyzer.Analyze();
            return new DocumentState(fileContentInfo);
        }
    }
}

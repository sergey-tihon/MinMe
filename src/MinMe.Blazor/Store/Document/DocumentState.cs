using System;

using MinMe.Analyzers.Model;

namespace MinMe.Blazor.Store.Document
{
    public class DocumentState
    {
        public FileContentInfo? FileContentInfo { get; }
        
        public DocumentState(FileContentInfo? contentInfo)
        {
            FileContentInfo = contentInfo;
        }
    }
}

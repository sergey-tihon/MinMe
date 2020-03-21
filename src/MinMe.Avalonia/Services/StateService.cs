using MinMe.Analyzers.Model;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Subjects;

namespace MinMe.Avalonia.Services
{
    class StateService
    {
        public IObservable<FileContentInfo?> FileContentInfo => FileContentInfoSubject;

        private readonly Subject<FileContentInfo> FileContentInfoSubject
            = new Subject<FileContentInfo>();

        public void ChangeState(FileContentInfo fileContentInfo)
        {
            FileContentInfoSubject.OnNext(fileContentInfo);
        }
    }
}

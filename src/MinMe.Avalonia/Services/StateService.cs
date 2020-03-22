using MinMe.Analyzers.Model;
using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MinMe.Avalonia.Services
{
    class StateService
    {
        public StateService() => IsBusySubject.OnNext(false);
        public IObservable<FileContentInfo?> FileContentInfo => FileContentInfoSubject;
        public IObservable<bool> IsBusy => IsBusySubject;

        private readonly Subject<FileContentInfo> FileContentInfoSubject = new Subject<FileContentInfo>();
        private readonly Subject<bool> IsBusySubject = new Subject<bool>();

        public void SetState(FileContentInfo fileContentInfo)
            => FileContentInfoSubject.OnNext(fileContentInfo);

        public async Task RunTask(Func<Task> action)
        {
            try
            {
                IsBusySubject.OnNext(true);
                await Task.Run(action);
            }
            finally
            {
                IsBusySubject.OnNext(false);
            }
        }
    }
}

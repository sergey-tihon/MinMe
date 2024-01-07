using MinMe.Analyzers.Model;
using System.Reactive.Subjects;

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

        public async Task Run(Action action)
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

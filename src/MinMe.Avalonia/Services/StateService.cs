using MinMe.Analyzers.Model;
using System.Reactive.Subjects;

namespace MinMe.Avalonia.Services;

class StateService
{
    public StateService() => _isBusySubject.OnNext(false);
    public IObservable<FileContentInfo?> FileContentInfo => _fileContentInfoSubject;
    public IObservable<bool> IsBusy => _isBusySubject;

    private readonly Subject<FileContentInfo> _fileContentInfoSubject = new();
    private readonly Subject<bool> _isBusySubject = new();

    public void SetState(FileContentInfo fileContentInfo)
        => _fileContentInfoSubject.OnNext(fileContentInfo);

    public async Task RunTask(Func<Task> action)
    {
        try
        {
            _isBusySubject.OnNext(true);
            await Task.Run(action);
        }
        finally
        {
            _isBusySubject.OnNext(false);
        }
    }

    public async Task Run(Action action)
    {
        try
        {
            _isBusySubject.OnNext(true);
            await Task.Run(action);
        }
        finally
        {
            _isBusySubject.OnNext(false);
        }
    }
}

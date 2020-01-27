using System;
using System.Reactive;
using System.Threading.Tasks;
using MinMe.Blazor.Data;
using ReactiveUI;

namespace MinMe.Blazor.ViewModels
{
    public class CounterViewModel : ReactiveObject
    {
        private readonly NotificationService _notificationService;

        private int _currentCount;

        private readonly ObservableAsPropertyHelper<int> _count;

        public CounterViewModel(NotificationService notificationService)
        {
            Increment = ReactiveCommand.CreateFromTask(IncrementCount);
            ShowError = ReactiveCommand.Create(ShowErrorImpl);

            _notificationService = notificationService;
            _count = Increment.ToProperty(this, x => x.CurrentCount, scheduler: RxApp.MainThreadScheduler);
        }

        public int CurrentCount => _count.Value;


        public ReactiveCommand<Unit, int> Increment { get; }
        public ReactiveCommand<Unit, Unit> ShowError { get; }

        private Task<int> IncrementCount()
        {
            _currentCount++;
            return Task.FromResult(_currentCount);
        }

        private void ShowErrorImpl()
        {
            _notificationService.ShowError("Error message.");
        }
    }
}

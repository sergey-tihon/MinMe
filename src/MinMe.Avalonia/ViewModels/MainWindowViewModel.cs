using System;
using System.Collections.Generic;
using System.Text;

using ReactiveUI;

namespace MinMe.Avalonia.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public MainWindowViewModel()
        {
            PowerPoint = new PowerPointViewModel();
            Greeting = "Hello World!";
        }

        public PowerPointViewModel PowerPoint { get; }


        private string _greeting;
        public string Greeting
        {
            get => _greeting;
            set => this.RaiseAndSetIfChanged(ref _greeting, value);
        }
    }
}

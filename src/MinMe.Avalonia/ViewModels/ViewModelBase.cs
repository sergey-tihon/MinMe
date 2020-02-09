using System;
using System.Collections.Generic;
using System.Text;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

using ReactiveUI;

namespace MinMe.Avalonia.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        protected static Window GetWindow()
        {
            if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
                return lifetime.MainWindow;

            var name = Application.Current.ApplicationLifetime?.GetType().Name;
            throw new Exception($"Unknown application lifetime '{name}'");
        }
    }
}

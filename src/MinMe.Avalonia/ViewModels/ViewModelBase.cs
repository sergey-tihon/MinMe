using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using ReactiveUI;

namespace MinMe.Avalonia.ViewModels;

public class ViewModelBase : ReactiveObject
{
    protected static IStorageProvider GetStorageProvider()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } window })
            return window.StorageProvider;

        var name = Application.Current?.ApplicationLifetime?.GetType().Name;
        throw new InvalidDataException($"Unknown application lifetime '{name}' or window is not set");
    }
}

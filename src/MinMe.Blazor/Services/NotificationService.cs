using System;
using Blazored.Toast.Services;
using ElectronNET.API;
using ElectronNET.API.Entities;

namespace MinMe.Blazor.Services
{
    public class NotificationService
    {
        private readonly IToastService _toastService;

        public NotificationService(IToastService toastService)
        {
            _toastService = toastService;
        }

        public void ShowError(string title, string errorMessage)
        {
            if (HybridSupport.IsElectronActive)
            {
                Electron.Dialog.ShowErrorBox(title, errorMessage);
            }
            else
            {
                _toastService.ShowError(errorMessage, title);
            }
        }

        public void ShowSuccess(string title, string message)
        {
            if (HybridSupport.IsElectronActive)
            {
                Electron.Dialog.ShowMessageBoxAsync(new MessageBoxOptions(message)
                {
                    Title = title,
                    Buttons = new [] {"Ok"},
                });
            }
            else
            {
                _toastService.ShowSuccess(message, title);
            }
        }
    }
}

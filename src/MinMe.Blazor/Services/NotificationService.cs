using System;
using Blazored.Toast.Services;
using ElectronNET.API;

namespace MinMe.Blazor.Services
{
    public class NotificationService
    {
        private readonly IToastService _toastService;

        public NotificationService(IToastService toastService)
        {
            _toastService = toastService;
        }

        public void ShowError(string errorMessage, string title = "MinMe Error")
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
    }
}

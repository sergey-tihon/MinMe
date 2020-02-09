using System;
using Blazored.Toast.Services;

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
            _toastService.ShowError(errorMessage, title);
        }

        public void ShowSuccess(string title, string message)
        {
            _toastService.ShowSuccess(message, title);
        }
    }
}

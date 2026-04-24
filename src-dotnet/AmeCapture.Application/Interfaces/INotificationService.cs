namespace AmeCapture.Application.Interfaces;

public interface INotificationService
{
    Task ShowNotificationAsync(string title, string message, Action? onClick = null);
}

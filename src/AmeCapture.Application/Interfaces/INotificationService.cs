namespace AmeCapture.Application.Interfaces
{
    public interface INotificationService
    {
        public Task ShowNotificationAsync(string title, string message, Action? onClick = null);
    }
}

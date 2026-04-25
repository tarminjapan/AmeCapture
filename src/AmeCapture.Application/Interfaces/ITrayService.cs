namespace AmeCapture.Application.Interfaces
{
    public interface ITrayService
    {
        public void Initialize();
        public void ShowWindow();
        public void HideWindow();
        public void Exit();
    }
}

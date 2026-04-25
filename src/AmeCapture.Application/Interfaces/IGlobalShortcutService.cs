namespace AmeCapture.Application.Interfaces
{
    public interface IGlobalShortcutService
    {
        public Task InitializeAsync();
        public bool RegisterHotKey(string name, string shortcut, Action callback);
        public void UnregisterHotKey(string name);
        public void UnregisterAll();
    }
}

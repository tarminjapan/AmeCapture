namespace AmeCapture.Application.Interfaces;

public interface IGlobalShortcutService
{
    Task InitializeAsync();
    bool RegisterHotKey(string name, string shortcut, Action callback);
    void UnregisterHotKey(string name);
    void UnregisterAll();
}

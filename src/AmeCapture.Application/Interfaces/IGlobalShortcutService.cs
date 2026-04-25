namespace AmeCapture.Application.Interfaces;

public interface IGlobalShortcutService
{
    bool RegisterHotKey(string name, string shortcut, Action callback);
    void UnregisterHotKey(string name);
    void UnregisterAll();
}

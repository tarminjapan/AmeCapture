namespace AmeCapture.Application.Interfaces;

public interface IGlobalShortcutService
{
    void RegisterHotKey(string name, string shortcut, Action callback);
    void UnregisterHotKey(string name);
    void UnregisterAll();
}

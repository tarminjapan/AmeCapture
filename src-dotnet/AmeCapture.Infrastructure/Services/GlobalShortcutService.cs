using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services;

public class GlobalShortcutService : IGlobalShortcutService
{
    private readonly Dictionary<string, HotKeyInfo> _hotKeys = new();
    private readonly object _lock = new();
    private int _nextId = 1;

    public void RegisterHotKey(string name, string shortcut, Action callback)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Global shortcuts are only supported on Windows.");

        lock (_lock)
        {
            if (_hotKeys.ContainsKey(name))
                UnregisterHotKey(name);

            var keys = ParseShortcut(shortcut);
            var id = Interlocked.Increment(ref _nextId);

            if (NativeMethods.RegisterHotKey(IntPtr.Zero, id, keys.Modifiers, keys.VirtualKey))
            {
                _hotKeys[name] = new HotKeyInfo
                {
                    Id = id,
                    Callback = callback,
                    Keys = keys
                };
            }
            else
            {
                throw new InvalidOperationException($"Failed to register hotkey: {shortcut}");
            }
        }
    }

    public void UnregisterHotKey(string name)
    {
        lock (_lock)
        {
            if (_hotKeys.TryGetValue(name, out var info))
            {
                NativeMethods.UnregisterHotKey(IntPtr.Zero, info.Id);
                _hotKeys.Remove(name);
            }
        }
    }

    public void UnregisterAll()
    {
        lock (_lock)
        {
            foreach (var info in _hotKeys.Values)
            {
                NativeMethods.UnregisterHotKey(IntPtr.Zero, info.Id);
            }
            _hotKeys.Clear();
        }
    }

    private static (int Modifiers, int VirtualKey) ParseShortcut(string shortcut)
    {
        var keys = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries);
        int modifiers = 0;
        int virtualKey = 0;

        foreach (var key in keys)
        {
            var trimmed = key.Trim().ToLowerInvariant();
            switch (trimmed)
            {
                case "ctrl":
                    modifiers |= NativeMethods.MOD_CONTROL;
                    break;
                case "alt":
                    modifiers |= NativeMethods.MOD_ALT;
                    break;
                case "shift":
                    modifiers |= NativeMethods.MOD_SHIFT;
                    break;
                case "win":
                    modifiers |= NativeMethods.MOD_WIN;
                    break;
                default:
                    virtualKey = ParseVirtualKey(trimmed);
                    break;
            }
        }

        return (modifiers, virtualKey);
    }

    private static int ParseVirtualKey(string key)
    {
        if (key.Length == 1 && char.IsLetter(key[0]))
            return char.ToUpper(key[0]);

        return key.ToUpperInvariant() switch
        {
            "F1" => 0x70,
            "F2" => 0x71,
            "F3" => 0x72,
            "F4" => 0x73,
            "F5" => 0x74,
            "F6" => 0x75,
            "F7" => 0x76,
            "F8" => 0x77,
            "F9" => 0x78,
            "F10" => 0x79,
            "F11" => 0x7A,
            "F12" => 0x7B,
            "A" => 0x41,
            "B" => 0x42,
            "C" => 0x43,
            "D" => 0x44,
            "E" => 0x45,
            "F" => 0x46,
            "G" => 0x47,
            "H" => 0x48,
            "I" => 0x49,
            "J" => 0x4A,
            "K" => 0x4B,
            "L" => 0x4C,
            "M" => 0x4D,
            "N" => 0x4E,
            "O" => 0x4F,
            "P" => 0x50,
            "Q" => 0x51,
            "R" => 0x52,
            "S" => 0x53,
            "T" => 0x54,
            "U" => 0x55,
            "V" => 0x56,
            "W" => 0x57,
            "X" => 0x58,
            "Y" => 0x59,
            "Z" => 0x5A,
            "D0" => 0x30,
            "D1" => 0x31,
            "D2" => 0x32,
            "D3" => 0x33,
            "D4" => 0x34,
            "D5" => 0x35,
            "D6" => 0x36,
            "D7" => 0x37,
            "D8" => 0x38,
            "D9" => 0x39,
            _ => 0
        };
    }

    private class HotKeyInfo
    {
        public int Id { get; set; }
        public Action Callback { get; set; } = null!;
        public (int Modifiers, int VirtualKey) Keys { get; set; }
    }

    private static class NativeMethods
    {
        public const int MOD_ALT = 0x0001;
        public const int MOD_CONTROL = 0x0002;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}

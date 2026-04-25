using System.Globalization;
using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;

namespace AmeCapture.Infrastructure.Services
{
    public class GlobalShortcutService : IGlobalShortcutService, IDisposable
    {
        private readonly Dictionary<string, HotKeyInfo> _hotKeys = [];
        private readonly Lock _lock = new();
        private int _nextId = 1;
        private IntPtr _messageWindow = IntPtr.Zero;
        private readonly Thread? _messageThread;
        private volatile bool _running = true;
        private readonly TaskCompletionSource<IntPtr> _initTcs = new();

        public GlobalShortcutService()
        {
            _messageThread = new Thread(() =>
            {
                var wc = new WndClassEx
                {
                    Size = Marshal.SizeOf<WndClassEx>(),
                    WindowProc = Marshal.GetFunctionPointerForDelegate<WndProcDelegate>(WndProc),
                    Instance = IntPtr.Zero,
                    ClassName = "GlobalShortcutService_" + Guid.NewGuid()
                };

                if (NativeMethods.RegisterClassEx(ref wc) != 0)
                {
                    nint hwnd = NativeMethods.CreateWindowEx(
                        0, wc.ClassName, "", 0, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                    _messageWindow = hwnd;
                    _initTcs.SetResult(hwnd);

                    Serilog.Log.Information("GlobalShortcutService message loop started");

                    while (_running)
                    {
                        if (NativeMethods.PeekMessage(out Msg msg, IntPtr.Zero, 0, 0, 1))
                        {
                            _ = NativeMethods.TranslateMessage(ref msg);
                            _ = NativeMethods.DispatchMessage(ref msg);
                        }
                        else
                        {
                            Thread.Sleep(10);
                        }
                    }

                    _ = NativeMethods.DestroyWindow(hwnd);
                    _ = NativeMethods.UnregisterClass(wc.ClassName, IntPtr.Zero);
                }
                else
                {
                    Serilog.Log.Error("Failed to register window class for GlobalShortcutService");
                    _initTcs.SetException(new InvalidOperationException("Failed to register window class"));
                }
            })
            { IsBackground = true };
            _messageThread.Start();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var delayTask = Task.Delay(TimeSpan.FromSeconds(5));
                Task completedTask = await Task.WhenAny(_initTcs.Task, delayTask);
                if (completedTask == delayTask)
                {
                    Serilog.Log.Error("GlobalShortcutService initialization timed out");
                }
                else
                {
                    _ = await _initTcs.Task;
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "GlobalShortcutService initialization failed");
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const int WM_HOTKEY = 0x0312;

            if (msg == WM_HOTKEY)
            {
                int hotKeyId = wParam.ToInt32();
                Serilog.Log.Debug("WM_HOTKEY received, hotKeyId={HotKeyId}", hotKeyId);
                lock (_lock)
                {
                    foreach (HotKeyInfo info in _hotKeys.Values)
                    {
                        if (info.Id == hotKeyId)
                        {
                            Serilog.Log.Debug("Hotkey matched, invoking callback for id={HotKeyId}", hotKeyId);
                            try
                            {
                                info.Callback?.Invoke();
                            }
                            catch (Exception ex)
                            {
                                Serilog.Log.Warning(ex, "Hotkey callback failed");
                            }
                            break;
                        }
                    }
                }
            }

            return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public bool RegisterHotKey(string name, string shortcut, Action callback)
        {
            Serilog.Log.Debug("RegisterHotKey: name={Name}, shortcut={Shortcut}", name, shortcut);
            lock (_lock)
            {
                if (_hotKeys.ContainsKey(name))
                {
                    Serilog.Log.Debug("RegisterHotKey: unregistering existing hotkey {Name}", name);
                    UnregisterHotKey(name);
                }

                (int Modifiers, int VirtualKey) keys = ParseShortcut(shortcut);
                int id = Interlocked.Increment(ref _nextId);
                Serilog.Log.Debug("RegisterHotKey: parsed modifiers={Modifiers}, virtualKey={VirtualKey}, id={Id}", keys.Modifiers, keys.VirtualKey, id);

                if (NativeMethods.RegisterHotKey(_messageWindow, id, keys.Modifiers, keys.VirtualKey))
                {
                    _hotKeys[name] = new HotKeyInfo
                    {
                        Id = id,
                        Callback = callback,
                        Keys = keys
                    };
                    Serilog.Log.Debug("RegisterHotKey: successfully registered {Name} (id={Id})", name, id);
                    return true;
                }
                else
                {
                    Serilog.Log.Warning("Failed to register hotkey: {Shortcut}", shortcut);
                    return false;
                }
            }
        }

        public void UnregisterHotKey(string name)
        {
            Serilog.Log.Debug("UnregisterHotKey: name={Name}", name);
            lock (_lock)
            {
                if (_hotKeys.TryGetValue(name, out HotKeyInfo? info))
                {
                    _ = NativeMethods.UnregisterHotKey(_messageWindow, info.Id);
                    _ = _hotKeys.Remove(name);
                    Serilog.Log.Debug("UnregisterHotKey: unregistered {Name} (id={Id})", name, info.Id);
                }
            }
        }

        public void UnregisterAll()
        {
            Serilog.Log.Debug("UnregisterAll: clearing {Count} hotkeys", _hotKeys.Count);
            lock (_lock)
            {
                foreach (HotKeyInfo info in _hotKeys.Values)
                {
                    _ = NativeMethods.UnregisterHotKey(_messageWindow, info.Id);
                }
                _hotKeys.Clear();
            }
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _running = false;
                _ = (_messageThread?.Join(1000));
                UnregisterAll();
            }

            _disposed = true;
        }

        private static (int Modifiers, int VirtualKey) ParseShortcut(string shortcut)
        {
            string[] keys = shortcut.Split('+', StringSplitOptions.RemoveEmptyEntries);
            int modifiers = 0;
            int virtualKey = 0;

            foreach (string key in keys)
            {
                string trimmed = key.Trim().ToLowerInvariant();
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
            return key.Length == 1 && char.IsLetter(key[0])
                ? char.ToUpper(key[0], CultureInfo.InvariantCulture)
                : key.ToUpperInvariant() switch
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

        private struct WndClassEx
        {
            public int Size;
            public int Style;
            public IntPtr WindowProc;
            public int ClassExtra;
            public int WindowExtra;
            public IntPtr Instance;
            public IntPtr Icon;
            public IntPtr Cursor;
            public IntPtr BackgroundBrush;
            public IntPtr MenuName;
            public string ClassName;
            public IntPtr IconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Msg
        {
            public IntPtr Hwnd;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public int PtX;
            public int PtY;
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        private sealed class HotKeyInfo
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

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern ushort RegisterClassEx(ref WndClassEx lpwcx);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr CreateWindowEx(
                int dwExStyle,
                string lpClassName,
                string lpWindowName,
                int dwStyle,
                int x,
                int y,
                int nWidth,
                int nHeight,
                IntPtr hWndParent,
                IntPtr hMenu,
                IntPtr hInstance,
                IntPtr lpParam);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern bool DestroyWindow(IntPtr hWnd);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll", CharSet = CharSet.Unicode)]
            public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern bool PeekMessage(out Msg lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern bool TranslateMessage(ref Msg lpMsg);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern IntPtr DispatchMessage(ref Msg lpMsg);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            [DllImport("user32.dll")]
            public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        }
    }
}

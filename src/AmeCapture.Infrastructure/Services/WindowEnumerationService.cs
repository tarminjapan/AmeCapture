using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Models;

namespace AmeCapture.Infrastructure.Services
{
    public class WindowEnumerationService : IWindowEnumerationService
    {
        public async Task<IReadOnlyList<WindowInfo>> EnumerateWindowsAsync()
        {
            Serilog.Log.Debug("WindowEnumerationService.EnumerateWindowsAsync started");
            return await Task.Run(() =>
            {
                var windows = new List<WindowInfo>();
                var handle = GCHandle.Alloc(windows);
                try
                {
                    _ = NativeMethods.EnumWindows((hwnd, lParam) =>
                    {
                        if (!NativeMethods.IsWindowVisible(hwnd))
                        {
                            return true;
                        }

                        long style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
                        if ((style & NativeMethods.WS_DISABLED) != 0)
                        {
                            return true;
                        }

                        if (NativeMethods.IsIconic(hwnd))
                        {
                            return true;
                        }

                        char[] titleBuf = new char[512];
                        int titleLen = NativeMethods.GetWindowText(hwnd, titleBuf, titleBuf.Length);
                        if (titleLen == 0)
                        {
                            return true;
                        }

                        string title = new(titleBuf, 0, titleLen);
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            return true;
                        }

                        char[] classBuf = new char[256];
                        int classLen = NativeMethods.GetClassName(hwnd, classBuf, classBuf.Length);
                        string className = new(classBuf, 0, classLen);

                        var rect = new NativeMethods.RECT();
                        int hr = NativeMethods.DwmGetWindowAttribute(hwnd,
                            NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                            ref rect, Marshal.SizeOf<NativeMethods.RECT>());

                        (int x, int y, int w, int h) bounds;
                        if (hr >= 0)
                        {
                            bounds = (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                        }
                        else
                        {
                            var fallback = new NativeMethods.RECT();
                            _ = NativeMethods.GetWindowRect(hwnd, ref fallback);
                            bounds = (fallback.Left, fallback.Top, fallback.Right - fallback.Left, fallback.Bottom - fallback.Top);
                        }

                        Serilog.Log.Debug("WindowEnumerationService: found window hwnd={Hwnd}, title={Title}, class={Class}, bounds=({X},{Y},{W},{H})", hwnd, title, className, bounds.x, bounds.y, bounds.w, bounds.h);

                        var list = (List<WindowInfo>)GCHandle.FromIntPtr(lParam).Target!;
                        list.Add(new WindowInfo
                        {
                            Hwnd = hwnd,
                            Title = title,
                            ClassName = className,
                            Bounds = bounds
                        });
                        return true;
                    }, GCHandle.ToIntPtr(handle));
                }
                finally
                {
                    handle.Free();
                }

                windows.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));
                Serilog.Log.Debug("WindowEnumerationService: enumerated {Count} windows", windows.Count);
                return windows;
            });
        }
    }

    file static class NativeMethods
    {
        public const int GWL_STYLE = -16;
        public const long WS_DISABLED = 0x08000000L;
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, ref RECT pvAttribute, int cbAttribute);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);
    }
}

using System.Runtime.InteropServices;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Models;

namespace AmeCapture.Infrastructure.Services
{
    public class CaptureService : ICaptureService
    {
        public async Task<CaptureResult> CaptureFullScreenAsync(string savePath)
        {
            Serilog.Log.Debug("CaptureFullScreenAsync started, savePath={SavePath}", savePath);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                string? dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    _ = Directory.CreateDirectory(dir);
                }

                int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
                int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);
                Serilog.Log.Debug("Screen dimensions: {Width}x{Height}", width, height);

                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Invalid screen dimensions.");
                }

                using var screenDc = new SafeScreenDc(NativeMethods.GetDC(IntPtr.Zero));
                Serilog.Log.Debug("ScreenDC handle: {Handle}, IsInvalid={IsInvalid}", screenDc.DangerousGetHandle(), screenDc.IsInvalid);
                using var memDc = new SafeCompatibleDc(screenDc);
                using var bitmap = new SafeBitmap(memDc, width, height);
                using var selectGuard = new SafeSelectObject(memDc, bitmap.DangerousGetHandle());

                if (!NativeMethods.BitBlt(memDc, 0, 0, width, height, screenDc, 0, 0, NativeMethods.SRCCOPY))
                {
                    throw new InvalidOperationException("BitBlt failed.");
                }

                using var img = System.Drawing.Image.FromHbitmap(bitmap.DangerousGetHandle());
                img.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
                Serilog.Log.Debug("Image saved to {SavePath}, size={Size} bytes", savePath, new FileInfo(savePath).Length);

                sw.Stop();
                Serilog.Log.Debug("CaptureFullScreenAsync completed in {Elapsed}ms", sw.ElapsedMilliseconds);

                return new CaptureResult
                {
                    FilePath = savePath,
                    Width = (uint)width,
                    Height = (uint)height
                };
            });
        }

        public async Task<CaptureResult> CaptureWindowAsync(nint hwnd, string savePath)
        {
            Serilog.Log.Debug("CaptureWindowAsync started, hwnd={Hwnd}, savePath={SavePath}", hwnd, savePath);
            var sw = System.Diagnostics.Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                string? dir = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    _ = Directory.CreateDirectory(dir);
                }

                var (left, top, right, bottom) = GetWindowBounds(hwnd);
                int width = right - left;
                int height = bottom - top;
                Serilog.Log.Debug("Window bounds: left={Left}, top={Top}, right={Right}, bottom={Bottom}, size={Width}x{Height}", left, top, right, bottom, width, height);

                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("Invalid window dimensions.");
                }

                using var windowDc = new SafeWindowDc(hwnd, NativeMethods.GetWindowDC(hwnd));
                Serilog.Log.Debug("WindowDC handle: {Handle}, IsInvalid={IsInvalid}", windowDc.DangerousGetHandle(), windowDc.IsInvalid);
                using var memDc = new SafeCompatibleDc(windowDc);
                using var bitmap = new SafeBitmap(memDc, width, height);
                using var selectGuard = new SafeSelectObject(memDc, bitmap.DangerousGetHandle());

                if (!NativeMethods.BitBlt(memDc, 0, 0, width, height, windowDc, 0, 0,
                        NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT))
                {
                    throw new InvalidOperationException("BitBlt failed.");
                }

                using var img = System.Drawing.Image.FromHbitmap(bitmap.DangerousGetHandle());
                img.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
                Serilog.Log.Debug("Window image saved to {SavePath}, size={Size} bytes", savePath, new FileInfo(savePath).Length);

                sw.Stop();
                Serilog.Log.Debug("CaptureWindowAsync completed in {Elapsed}ms", sw.ElapsedMilliseconds);

                return new CaptureResult
                {
                    FilePath = savePath,
                    Width = (uint)width,
                    Height = (uint)height
                };
            });
        }

        private static (int Left, int Top, int Right, int Bottom) GetWindowBounds(nint hwnd)
        {
            var rect = new NativeMethods.RECT();
            int hr = NativeMethods.DwmGetWindowAttribute(hwnd,
                NativeMethods.DWMWA_EXTENDED_FRAME_BOUNDS,
                ref rect, Marshal.SizeOf<NativeMethods.RECT>());
            if (hr >= 0 && rect.Right > rect.Left && rect.Bottom > rect.Top)
            {
                return (rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            var fallback = new NativeMethods.RECT();
            _ = NativeMethods.GetWindowRect(hwnd, ref fallback);
            return (fallback.Left, fallback.Top, fallback.Right, fallback.Bottom);
        }
    }

    file static class NativeMethods
    {
        public const int SM_CXSCREEN = 0;
        public const int SM_CYSCREEN = 1;
        public const int SRCCOPY = 0x00CC0020;
        public const int CAPTUREBLT = 0x40000000;
        public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr ho);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdc, int x, int y, int w, int h,
            IntPtr hdcSrc, int x1, int y1, int rop);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
            ref RECT pvAttribute, int cbAttribute);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        public const int GWL_STYLE = -16;
        public const long WS_DISABLED = 0x08000000L;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    }

    file sealed class SafeScreenDc : SafeHandle
    {
        public SafeScreenDc(IntPtr dc) : base(IntPtr.Zero, true)
        {
            SetHandle(dc);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
        public static implicit operator IntPtr(SafeScreenDc s)
        {
            return s.handle;
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.ReleaseDC(IntPtr.Zero, handle) != 0;
        }
    }

    file sealed class SafeWindowDc : SafeHandle
    {
        private readonly nint _hwnd;
        public SafeWindowDc(nint hwnd, IntPtr dc) : base(IntPtr.Zero, true) { _hwnd = hwnd; SetHandle(dc); }
        public override bool IsInvalid => handle == IntPtr.Zero;
        public static implicit operator IntPtr(SafeWindowDc s)
        {
            return s.handle;
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.ReleaseDC(_hwnd, handle) != 0;
        }
    }

    file sealed class SafeCompatibleDc : SafeHandle
    {
        public SafeCompatibleDc(IntPtr sourceDc) : base(IntPtr.Zero, true)
        {
            SetHandle(NativeMethods.CreateCompatibleDC(sourceDc));
        }
        public override bool IsInvalid => handle == IntPtr.Zero;
        public static implicit operator IntPtr(SafeCompatibleDc s)
        {
            return s.handle;
        }

        protected override bool ReleaseHandle()
        {
            return NativeMethods.DeleteDC(handle);
        }
    }

    file sealed class SafeBitmap : SafeHandle
    {
        public SafeBitmap(IntPtr sourceDc, int w, int h) : base(IntPtr.Zero, true)
        {
            SetHandle(NativeMethods.CreateCompatibleBitmap(sourceDc, w, h));
        }
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle()
        {
            return NativeMethods.DeleteObject(handle);
        }

        public new IntPtr DangerousGetHandle()
        {
            return handle;
        }
    }

    file sealed class SafeSelectObject(IntPtr hdc, IntPtr bmpHandle) : SafeHandle(IntPtr.Zero, true)
    {
        private readonly IntPtr _hdc = hdc;
        private readonly IntPtr _old = NativeMethods.SelectObject(hdc, bmpHandle);

        public override bool IsInvalid => false;
        protected override bool ReleaseHandle() { _ = NativeMethods.SelectObject(_hdc, _old); return true; }
    }
}

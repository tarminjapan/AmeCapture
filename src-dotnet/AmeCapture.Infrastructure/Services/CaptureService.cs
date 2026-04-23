using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AmeCapture.Application.Interfaces;
using AmeCapture.Application.Models;

namespace AmeCapture.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public class CaptureService : ICaptureService
{
    public async Task<CaptureResult> CaptureFullScreenAsync(string savePath)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Screen capture is only supported on Windows.");

        return await Task.Run(() =>
        {
            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Invalid screen dimensions.");

            using var screenDc = new SafeScreenDc(NativeMethods.GetDC(IntPtr.Zero));
            using var memDc = new SafeCompatibleDc(screenDc);
            using var bitmap = new SafeBitmap(memDc, width, height);
            using var selectGuard = new SafeSelectObject(memDc, bitmap.DangerousGetHandle());

            if (!NativeMethods.BitBlt(memDc, 0, 0, width, height, screenDc, 0, 0, NativeMethods.SRCCOPY))
                throw new InvalidOperationException("BitBlt failed.");

            using var img = System.Drawing.Image.FromHbitmap(bitmap.DangerousGetHandle());
            img.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);

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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            throw new PlatformNotSupportedException("Window capture is only supported on Windows.");

        return await Task.Run(() =>
        {
            var dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var (left, top, right, bottom) = GetWindowBounds(hwnd);
            int width = right - left;
            int height = bottom - top;

            if (width <= 0 || height <= 0)
                throw new InvalidOperationException("Invalid window dimensions.");

            using var windowDc = new SafeWindowDc(hwnd, NativeMethods.GetWindowDC(hwnd));
            using var memDc = new SafeCompatibleDc(windowDc);
            using var bitmap = new SafeBitmap(memDc, width, height);
            using var selectGuard = new SafeSelectObject(memDc, bitmap.DangerousGetHandle());

            if (!NativeMethods.BitBlt(memDc, 0, 0, width, height, windowDc, 0, 0,
                    NativeMethods.SRCCOPY | NativeMethods.CAPTUREBLT))
                throw new InvalidOperationException("BitBlt failed.");

            using var img = System.Drawing.Image.FromHbitmap(bitmap.DangerousGetHandle());
            img.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);

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
            return (rect.Left, rect.Top, rect.Right, rect.Bottom);

        var fallback = new NativeMethods.RECT();
        NativeMethods.GetWindowRect(hwnd, ref fallback);
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

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    public static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    public static extern bool BitBlt(IntPtr hdc, int x, int y, int w, int h,
        IntPtr hdcSrc, int x1, int y1, int rop);

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute,
        ref RECT pvAttribute, int cbAttribute);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, char[] lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, char[] lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    public const int GWL_STYLE = -16;
    public const long WS_DISABLED = 0x08000000L;

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
}

file sealed class SafeScreenDc : SafeHandle
{
    private readonly IntPtr _dc;

    public SafeScreenDc(IntPtr dc) : base(IntPtr.Zero, true) => _dc = dc;
    public override bool IsInvalid => _dc == IntPtr.Zero;
    public static implicit operator IntPtr(SafeScreenDc s) => s._dc;
    protected override bool ReleaseHandle() => NativeMethods.ReleaseDC(IntPtr.Zero, _dc) != 0;
}

file sealed class SafeWindowDc : SafeHandle
{
    private readonly nint _hwnd;
    private readonly IntPtr _dc;

    public SafeWindowDc(nint hwnd, IntPtr dc) : base(IntPtr.Zero, true) { _hwnd = hwnd; _dc = dc; }
    public override bool IsInvalid => _dc == IntPtr.Zero;
    public static implicit operator IntPtr(SafeWindowDc s) => s._dc;
    protected override bool ReleaseHandle() => NativeMethods.ReleaseDC(_hwnd, _dc) != 0;
}

file sealed class SafeCompatibleDc(IntPtr sourceDc) : SafeHandle(IntPtr.Zero, true)
{
    private readonly IntPtr _dc = NativeMethods.CreateCompatibleDC(sourceDc);
    public override bool IsInvalid => _dc == IntPtr.Zero;
    public static implicit operator IntPtr(SafeCompatibleDc s) => s._dc;
    protected override bool ReleaseHandle() => NativeMethods.DeleteDC(_dc);
}

file sealed class SafeBitmap(IntPtr sourceDc, int w, int h) : SafeHandle(IntPtr.Zero, true)
{
    private readonly IntPtr _bmp = NativeMethods.CreateCompatibleBitmap(sourceDc, w, h);
    public override bool IsInvalid => _bmp == IntPtr.Zero;
    protected override bool ReleaseHandle() => NativeMethods.DeleteObject(_bmp);
    public new IntPtr DangerousGetHandle() => _bmp;
}

file sealed class SafeSelectObject(IntPtr hdc, IntPtr bmpHandle) : SafeHandle(IntPtr.Zero, true)
{
    private readonly IntPtr _old = NativeMethods.SelectObject(hdc, bmpHandle);
    public override bool IsInvalid => false;
    protected override bool ReleaseHandle() { NativeMethods.SelectObject(hdc, _old); return true; }
}

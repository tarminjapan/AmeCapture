use crate::error::{AppError, AppResult};
use crate::models::capture::{CaptureRegion, CaptureResult};

#[allow(dead_code)]
pub trait CaptureService: Send + Sync {
    fn capture_full_screen(&self, save_path: &str) -> AppResult<CaptureResult>;
    fn capture_region(&self, region: &CaptureRegion, save_path: &str) -> AppResult<CaptureResult>;
    fn capture_window(&self, hwnd: isize, save_path: &str) -> AppResult<CaptureResult>;
}

pub struct DefaultCaptureService;

impl DefaultCaptureService {
    pub fn new() -> Self {
        Self
    }
}

#[cfg(target_os = "windows")]
impl CaptureService for DefaultCaptureService {
    fn capture_full_screen(&self, save_path: &str) -> AppResult<CaptureResult> {
        capture_screen(save_path)
    }

    fn capture_region(&self, region: &CaptureRegion, _save_path: &str) -> AppResult<CaptureResult> {
        tracing::info!(
            "Region capture requested: x={}, y={}, width={}, height={}",
            region.x,
            region.y,
            region.width,
            region.height
        );
        Err(AppError::Capture(
            "Region capture not yet implemented".into(),
        ))
    }

    fn capture_window(&self, hwnd: isize, save_path: &str) -> AppResult<CaptureResult> {
        tracing::info!("Window capture requested for hwnd={}", hwnd);
        capture_window_by_hwnd(hwnd, save_path)
    }
}

#[cfg(not(target_os = "windows"))]
impl CaptureService for DefaultCaptureService {
    fn capture_full_screen(&self, _save_path: &str) -> AppResult<CaptureResult> {
        Err(AppError::Capture(
            "Full screen capture is only supported on Windows".into(),
        ))
    }

    fn capture_region(
        &self,
        _region: &CaptureRegion,
        _save_path: &str,
    ) -> AppResult<CaptureResult> {
        Err(AppError::Capture(
            "Region capture is only supported on Windows".into(),
        ))
    }

    fn capture_window(&self, _hwnd: isize, _save_path: &str) -> AppResult<CaptureResult> {
        Err(AppError::Capture(
            "Window capture is only supported on Windows".into(),
        ))
    }
}

#[cfg(target_os = "windows")]
mod gdi_guard {
    use windows::Win32::Foundation::HWND;
    use windows::Win32::Graphics::Gdi::{
        DeleteDC, DeleteObject, ReleaseDC, SelectObject, HBITMAP, HDC, HGDIOBJ,
    };

    pub struct ScreenDcGuard {
        dc: HDC,
        released: bool,
    }

    impl ScreenDcGuard {
        pub fn new(dc: HDC) -> Self {
            Self {
                dc,
                released: false,
            }
        }

        pub fn get(&self) -> HDC {
            self.dc
        }

        pub fn release(mut self) {
            unsafe {
                let _ = ReleaseDC(None, self.dc);
            }
            self.released = true;
        }
    }

    impl Drop for ScreenDcGuard {
        fn drop(&mut self) {
            if !self.released {
                unsafe {
                    let _ = ReleaseDC(None, self.dc);
                }
            }
        }
    }

    pub struct WindowDcGuard {
        hwnd: HWND,
        dc: HDC,
        released: bool,
    }

    impl WindowDcGuard {
        pub fn new(hwnd: HWND, dc: HDC) -> Self {
            Self {
                hwnd,
                dc,
                released: false,
            }
        }

        pub fn get(&self) -> HDC {
            self.dc
        }
    }

    impl Drop for WindowDcGuard {
        fn drop(&mut self) {
            if !self.released {
                unsafe {
                    let _ = ReleaseDC(Some(self.hwnd), self.dc);
                }
                self.released = true;
            }
        }
    }

    pub struct GdiCleanup {
        mem_dc: Option<HDC>,
        bitmap: Option<HBITMAP>,
        old_obj: Option<HGDIOBJ>,
    }

    impl GdiCleanup {
        pub fn new(mem_dc: HDC, bitmap: HBITMAP, old_obj: HGDIOBJ) -> Self {
            Self {
                mem_dc: Some(mem_dc),
                bitmap: Some(bitmap),
                old_obj: Some(old_obj),
            }
        }
    }

    impl Drop for GdiCleanup {
        fn drop(&mut self) {
            unsafe {
                if let (Some(dc), Some(old)) = (self.mem_dc, self.old_obj) {
                    let _ = SelectObject(dc, old);
                }
                if let Some(bmp) = self.bitmap.take() {
                    let _ = DeleteObject(bmp.into());
                }
                if let Some(dc) = self.mem_dc.take() {
                    let _ = DeleteDC(dc);
                }
            }
        }
    }
}

#[cfg(target_os = "windows")]
fn capture_screen(save_path: &str) -> AppResult<CaptureResult> {
    use windows::Win32::Graphics::Gdi::*;
    use windows::Win32::UI::WindowsAndMessaging::*;

    let save = std::path::Path::new(save_path);
    if let Some(parent) = save.parent() {
        std::fs::create_dir_all(parent)?;
    }

    unsafe {
        let screen_dc = gdi_guard::ScreenDcGuard::new(GetDC(None));
        let width = GetSystemMetrics(SM_CXSCREEN);
        let height = GetSystemMetrics(SM_CYSCREEN);

        if width <= 0 || height <= 0 {
            return Err(AppError::Capture("Invalid screen dimensions".into()));
        }

        let mem_dc = CreateCompatibleDC(Some(screen_dc.get()));
        let bitmap = CreateCompatibleBitmap(screen_dc.get(), width, height);
        let old = SelectObject(mem_dc, bitmap.into());

        let _gdi = gdi_guard::GdiCleanup::new(mem_dc, bitmap, old);

        if let Err(e) = BitBlt(
            mem_dc,
            0,
            0,
            width,
            height,
            Some(screen_dc.get()),
            0,
            0,
            SRCCOPY,
        ) {
            return Err(AppError::Capture(format!("BitBlt failed: {e}")));
        }

        let mut bmi: BITMAPINFO = std::mem::zeroed();
        bmi.bmiHeader.biSize = std::mem::size_of::<BITMAPINFOHEADER>() as u32;
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height;
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;

        let buf_size = (width as usize) * (height as usize) * 4;
        let mut pixels = vec![0u8; buf_size];

        let scan_lines = GetDIBits(
            mem_dc,
            bitmap,
            0,
            height as u32,
            Some(pixels.as_mut_ptr() as *mut _),
            &mut bmi,
            DIB_RGB_COLORS,
        );

        if scan_lines == 0 {
            return Err(AppError::Capture("GetDIBits returned 0 scan lines".into()));
        }

        for chunk in pixels.chunks_exact_mut(4) {
            chunk.swap(0, 2);
        }

        let img =
            image::RgbaImage::from_raw(width as u32, height as u32, pixels).ok_or_else(|| {
                AppError::Capture("Failed to create image from captured pixels".into())
            })?;

        img.save_with_format(save, image::ImageFormat::Png)
            .map_err(|e| AppError::Capture(format!("Failed to save captured image: {e}")))?;

        screen_dc.release();

        Ok(CaptureResult {
            file_path: save_path.to_string(),
            width: width as u32,
            height: height as u32,
        })
    }
}

#[cfg(target_os = "windows")]
fn capture_window_by_hwnd(hwnd: isize, save_path: &str) -> AppResult<CaptureResult> {
    use windows::Win32::Foundation::HWND;
    use windows::Win32::Graphics::Dwm::{DwmGetWindowAttribute, DWMWA_EXTENDED_FRAME_BOUNDS};
    use windows::Win32::Graphics::Gdi::*;
    use windows::Win32::UI::WindowsAndMessaging::GetWindowRect;

    let save = std::path::Path::new(save_path);
    if let Some(parent) = save.parent() {
        std::fs::create_dir_all(parent)?;
    }

    let hwnd = HWND(hwnd as *mut _);

    let (width, height) = unsafe {
        let mut rect: windows::Win32::Foundation::RECT = std::mem::zeroed();
        let hr = DwmGetWindowAttribute(
            hwnd,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            &mut rect as *mut _ as *mut _,
            std::mem::size_of::<windows::Win32::Foundation::RECT>() as u32,
        );
        if hr.is_ok() && rect.right > rect.left && rect.bottom > rect.top {
            (rect.right - rect.left, rect.bottom - rect.top)
        } else {
            let mut fallback: windows::Win32::Foundation::RECT = std::mem::zeroed();
            let _ = GetWindowRect(hwnd, &mut fallback);
            (
                fallback.right - fallback.left,
                fallback.bottom - fallback.top,
            )
        }
    };

    if width <= 0 || height <= 0 {
        return Err(AppError::Capture("Invalid window dimensions".into()));
    }

    unsafe {
        let window_dc_guard = gdi_guard::WindowDcGuard::new(hwnd, GetWindowDC(Some(hwnd)));
        let window_dc = window_dc_guard.get();
        let mem_dc = CreateCompatibleDC(Some(window_dc));
        let bitmap = CreateCompatibleBitmap(window_dc, width, height);
        let old = SelectObject(mem_dc, bitmap.into());

        let _gdi = gdi_guard::GdiCleanup::new(mem_dc, bitmap, old);

        if let Err(e) = BitBlt(
            mem_dc,
            0,
            0,
            width,
            height,
            Some(window_dc),
            0,
            0,
            SRCCOPY | CAPTUREBLT,
        ) {
            return Err(AppError::Capture(format!("BitBlt failed: {e}")));
        }

        let mut bmi: BITMAPINFO = std::mem::zeroed();
        bmi.bmiHeader.biSize = std::mem::size_of::<BITMAPINFOHEADER>() as u32;
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height;
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32;

        let buf_size = (width as usize) * (height as usize) * 4;
        let mut pixels = vec![0u8; buf_size];

        let scan_lines = GetDIBits(
            mem_dc,
            bitmap,
            0,
            height as u32,
            Some(pixels.as_mut_ptr() as *mut _),
            &mut bmi,
            DIB_RGB_COLORS,
        );

        drop(window_dc_guard);

        if scan_lines == 0 {
            return Err(AppError::Capture("GetDIBits returned 0 scan lines".into()));
        }

        for chunk in pixels.chunks_exact_mut(4) {
            chunk.swap(0, 2);
        }

        let img =
            image::RgbaImage::from_raw(width as u32, height as u32, pixels).ok_or_else(|| {
                AppError::Capture("Failed to create image from captured pixels".into())
            })?;

        img.save_with_format(save, image::ImageFormat::Png)
            .map_err(|e| AppError::Capture(format!("Failed to save captured image: {e}")))?;

        Ok(CaptureResult {
            file_path: save_path.to_string(),
            width: width as u32,
            height: height as u32,
        })
    }
}

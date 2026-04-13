use crate::error::{AppError, AppResult};
use crate::models::capture::{CaptureRegion, CaptureResult};

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

    fn capture_window(&self, hwnd: isize, _save_path: &str) -> AppResult<CaptureResult> {
        tracing::info!("Window capture requested for hwnd={}", hwnd);
        Err(AppError::Capture(
            "Window capture not yet implemented".into(),
        ))
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
fn capture_screen(save_path: &str) -> AppResult<CaptureResult> {
    use windows::Win32::Graphics::Gdi::*;
    use windows::Win32::UI::WindowsAndMessaging::*;

    let save = std::path::Path::new(save_path);
    if let Some(parent) = save.parent() {
        std::fs::create_dir_all(parent)?;
    }

    unsafe {
        let screen_dc = GetDC(None);
        let width = GetSystemMetrics(SM_CXSCREEN);
        let height = GetSystemMetrics(SM_CYSCREEN);

        if width <= 0 || height <= 0 {
            let _ = ReleaseDC(None, screen_dc);
            return Err(AppError::Capture("Invalid screen dimensions".into()));
        }

        let mem_dc = CreateCompatibleDC(Some(screen_dc));
        let bitmap = CreateCompatibleBitmap(screen_dc, width, height);
        let _old = SelectObject(mem_dc, bitmap.into());

        if let Err(e) = BitBlt(mem_dc, 0, 0, width, height, Some(screen_dc), 0, 0, SRCCOPY) {
            let _ = ReleaseDC(None, screen_dc);
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
            let _ = ReleaseDC(None, screen_dc);
            return Err(AppError::Capture("GetDIBits returned 0 scan lines".into()));
        }

        for chunk in pixels.chunks_exact_mut(4) {
            chunk.swap(0, 2);
        }

        let img = match image::RgbaImage::from_raw(width as u32, height as u32, pixels) {
            Some(i) => i,
            None => {
                let _ = ReleaseDC(None, screen_dc);
                return Err(AppError::Capture(
                    "Failed to create image from captured pixels".into(),
                ));
            }
        };

        if let Err(e) = img.save_with_format(save, image::ImageFormat::Png) {
            let _ = ReleaseDC(None, screen_dc);
            return Err(AppError::Capture(format!(
                "Failed to save captured image: {e}"
            )));
        }

        let _ = DeleteObject(bitmap.into());
        let _ = DeleteDC(mem_dc);
        let _ = ReleaseDC(None, screen_dc);

        Ok(CaptureResult {
            file_path: save_path.to_string(),
            width: width as u32,
            height: height as u32,
        })
    }
}

use crate::utils::error::{AppError, AppResult};

/// Capture a specific window
/// TODO: Implement using Win32 API window enumeration + capture
pub fn capture_window(_hwnd: isize) -> AppResult<image::RgbaImage> {
    Err(AppError::Capture(
        "Window capture not yet implemented".into(),
    ))
}

/// Enumerate all visible windows
/// TODO: Implement using Win32 EnumWindows API
pub fn enumerate_windows() -> AppResult<Vec<WindowInfo>> {
    Ok(vec![])
}

/// Window information
pub struct WindowInfo {
    pub hwnd: isize,
    pub title: String,
    pub class_name: String,
    pub bounds: (i32, i32, i32, i32),
}

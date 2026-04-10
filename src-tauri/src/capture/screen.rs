use crate::utils::error::{AppError, AppResult};

/// Capture the full screen
/// TODO: Implement using Windows DXGI / GDI API
pub fn capture_full_screen() -> AppResult<image::RgbaImage> {
    // Placeholder - will be implemented with Win32 API
    Err(AppError::Capture(
        "Full screen capture not yet implemented".into(),
    ))
}

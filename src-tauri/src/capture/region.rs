use crate::utils::error::{AppError, AppResult};

/// Capture a specific region of the screen
/// TODO: Implement region selection overlay + capture
pub fn capture_region(_x: i32, _y: i32, _width: u32, _height: u32) -> AppResult<image::RgbaImage> {
    Err(AppError::Capture("Region capture not yet implemented".into()))
}
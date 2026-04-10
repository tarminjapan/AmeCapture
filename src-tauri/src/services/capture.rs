use crate::error::AppResult;
use crate::models::capture::{CaptureRegion, CaptureResult};

/// Service trait for capture operations
pub trait CaptureService: Send + Sync {
    fn capture_full_screen(&self) -> AppResult<CaptureResult>;
    fn capture_region(&self, region: &CaptureRegion) -> AppResult<CaptureResult>;
    fn capture_window(&self, hwnd: isize) -> AppResult<CaptureResult>;
}

/// Default capture service (placeholder for actual implementation)
pub struct DefaultCaptureService;

impl DefaultCaptureService {
    pub fn new() -> Self {
        Self
    }
}

impl CaptureService for DefaultCaptureService {
    fn capture_full_screen(&self) -> AppResult<CaptureResult> {
        tracing::info!("Full screen capture requested (not yet implemented)");
        Err(crate::error::AppError::Capture(
            "Full screen capture not yet implemented".into(),
        ))
    }

    fn capture_region(&self, region: &CaptureRegion) -> AppResult<CaptureResult> {
        tracing::info!(
            "Region capture requested: x={}, y={}, width={}, height={} (not yet implemented)",
            region.x,
            region.y,
            region.width,
            region.height
        );
        Err(crate::error::AppError::Capture(
            "Region capture not yet implemented".into(),
        ))
    }

    fn capture_window(&self, hwnd: isize) -> AppResult<CaptureResult> {
        tracing::info!(
            "Window capture requested for hwnd={} (not yet implemented)",
            hwnd
        );
        Err(crate::error::AppError::Capture(
            "Window capture not yet implemented".into(),
        ))
    }
}

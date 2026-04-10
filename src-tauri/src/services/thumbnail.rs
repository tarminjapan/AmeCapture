use crate::error::AppResult;

/// Service trait for thumbnail generation
pub trait ThumbnailService: Send + Sync {
    fn generate_thumbnail(&self, source_path: &str) -> AppResult<String>;
}

/// Default thumbnail service (placeholder)
pub struct DefaultThumbnailService;

impl DefaultThumbnailService {
    pub fn new() -> Self {
        Self
    }
}

impl ThumbnailService for DefaultThumbnailService {
    fn generate_thumbnail(&self, source_path: &str) -> AppResult<String> {
        tracing::info!(
            "Thumbnail generation requested for: {} (not yet implemented)",
            source_path
        );
        Err(crate::error::AppError::Capture(
            "Thumbnail generation not yet implemented".into(),
        ))
    }
}

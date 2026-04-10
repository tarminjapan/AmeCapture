use crate::error::AppResult;

/// Service trait for image editing operations
pub trait EditorService: Send + Sync {
    fn save_edited_image(&self, source_path: &str, edit_data: &str) -> AppResult<String>;
}

/// Default editor service (placeholder)
pub struct DefaultEditorService;

impl DefaultEditorService {
    pub fn new() -> Self {
        Self
    }
}

impl EditorService for DefaultEditorService {
    fn save_edited_image(&self, source_path: &str, _edit_data: &str) -> AppResult<String> {
        tracing::info!(
            "Save edited image requested for: {} (not yet implemented)",
            source_path
        );
        Err(crate::error::AppError::Editor(
            "Editor not yet implemented".into(),
        ))
    }
}

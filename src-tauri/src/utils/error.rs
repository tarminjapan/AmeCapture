use serde::Serialize;

/// Unified error type for the application
#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error("Database error: {0}")]
    Database(#[from] rusqlite::Error),

    #[error("IO error: {0}")]
    Io(#[from] std::io::Error),

    #[error("Image error: {0}")]
    Image(#[from] image::ImageError),

    #[error("Capture error: {0}")]
    Capture(String),

    #[error("Editor error: {0}")]
    Editor(String),

    #[error("Not found: {0}")]
    NotFound(String),

    #[error("Serialization error: {0}")]
    Serialization(#[from] serde_json::Error),
}

impl Serialize for AppError {
    fn serialize<S>(&self, serializer: S) -> Result<S::Ok, S::Error>
    where
        S: serde::Serializer,
    {
        serializer.serialize_str(&self.to_string())
    }
}

/// Result type alias
pub type AppResult<T> = Result<T, AppError>;

/// Command result for Tauri IPC
#[derive(Debug, Serialize)]
pub struct CommandResult<T: Serialize> {
    pub success: bool,
    pub data: Option<T>,
    pub error: Option<String>,
}

impl<T: Serialize> CommandResult<T> {
    pub fn ok(data: T) -> Self {
        Self {
            success: true,
            data: Some(data),
            error: None,
        }
    }

    pub fn err(msg: impl ToString) -> CommandResult<T> {
        CommandResult {
            success: false,
            data: None,
            error: Some(msg.to_string()),
        }
    }
}

impl CommandResult<()> {
    pub fn success() -> Self {
        Self {
            success: true,
            data: None,
            error: None,
        }
    }
}

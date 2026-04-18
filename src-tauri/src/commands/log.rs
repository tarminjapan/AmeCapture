/// Frontend log command — receives log messages from the webview
/// and writes them through the Rust tracing system to the log file.
use serde::Deserialize;

#[derive(Debug, Clone, Copy, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum FrontendLogLevel {
    Trace,
    Debug,
    Info,
    Warn,
    Error,
}

#[derive(Debug, Deserialize)]
pub struct FrontendLogEntry {
    pub level: FrontendLogLevel,
    pub message: String,
}

#[tauri::command]
pub fn frontend_log(entry: FrontendLogEntry) {
    match entry.level {
        FrontendLogLevel::Trace => {
            tracing::trace!(target: "frontend", "[FRONTEND] {}", entry.message)
        }
        FrontendLogLevel::Debug => {
            tracing::debug!(target: "frontend", "[FRONTEND] {}", entry.message)
        }
        FrontendLogLevel::Info => {
            tracing::info!(target: "frontend", "[FRONTEND] {}", entry.message)
        }
        FrontendLogLevel::Warn => {
            tracing::warn!(target: "frontend", "[FRONTEND] {}", entry.message)
        }
        FrontendLogLevel::Error => {
            tracing::error!(target: "frontend", "[FRONTEND] {}", entry.message)
        }
    }
}

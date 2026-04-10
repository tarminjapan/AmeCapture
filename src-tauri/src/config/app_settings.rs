use serde::{Deserialize, Serialize};

/// Application settings
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AppSettings {
    pub save_path: String,
    pub image_format: String,
    pub start_minimized: bool,
    pub hotkey_capture_region: String,
    pub hotkey_capture_fullscreen: String,
    pub hotkey_capture_window: String,
}

impl Default for AppSettings {
    fn default() -> Self {
        let default_save_path = dirs::picture_dir()
            .unwrap_or_else(|| std::path::PathBuf::from("."))
            .join("AmeCapture")
            .to_string_lossy()
            .to_string();

        Self {
            save_path: default_save_path,
            image_format: "png".to_string(),
            start_minimized: false,
            hotkey_capture_region: "Ctrl+Shift+S".to_string(),
            hotkey_capture_fullscreen: "Ctrl+Shift+F".to_string(),
            hotkey_capture_window: "Ctrl+Shift+W".to_string(),
        }
    }
}
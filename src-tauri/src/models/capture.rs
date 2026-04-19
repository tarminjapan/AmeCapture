use serde::{Deserialize, Serialize};

/// Capture region specification
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CaptureRegion {
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
}

/// Window information for capture
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct WindowInfo {
    pub hwnd: isize,
    pub title: String,
    pub class_name: String,
    pub bounds: (i32, i32, i32, i32),
}

/// Result of a capture operation
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CaptureResult {
    pub file_path: String,
    pub width: u32,
    pub height: u32,
}

/// Info returned after preparing a region capture
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct RegionCaptureInfo {
    pub temp_path: String,
    pub screen_width: u32,
    pub screen_height: u32,
    pub image_data_uri: String,
}

/// Info returned when preparing a window capture (list of available windows)
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WindowCaptureInfo {
    pub windows: Vec<WindowInfo>,
}

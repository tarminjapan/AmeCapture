/// Platform-specific operations module.
///
/// This module provides abstractions for OS-specific functionality
/// such as screen capture via Windows API, window enumeration,
/// and global hotkey registration.
///
/// Actual implementations will use the `windows` crate for Win32 API calls.
use crate::error::AppResult;

/// Get the list of monitors (placeholder)
pub fn enumerate_monitors() -> AppResult<Vec<MonitorInfo>> {
    tracing::debug!("Enumerating monitors (not yet implemented)");
    Ok(vec![])
}

/// Monitor information
#[derive(Debug, Clone)]
pub struct MonitorInfo {
    pub id: u32,
    pub name: String,
    pub bounds: (i32, i32, i32, i32),
    pub is_primary: bool,
}

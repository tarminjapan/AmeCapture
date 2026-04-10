use std::path::Path;

use serde::Serialize;
use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;

fn sanitize_filename(filename: &str) -> Result<String, String> {
    match Path::new(filename).file_name() {
        Some(name) => {
            let safe = name.to_string_lossy();
            if safe.is_empty() {
                Err("Invalid filename".to_string())
            } else {
                Ok(safe.to_string())
            }
        }
        None => Err("Invalid filename".to_string()),
    }
}

/// Response payload for storage directory paths.
#[derive(Debug, Serialize)]
pub struct StoragePaths {
    pub base_path: String,
    pub originals_dir: String,
    pub edited_dir: String,
    pub thumbnails_dir: String,
    pub videos_dir: String,
}

/// Get all storage directory paths.
#[tauri::command]
pub fn get_storage_paths(state: State<'_, AppState>) -> CommandResult<StoragePaths> {
    let resolver = state.storage_service.resolver();
    CommandResult::ok(StoragePaths {
        base_path: resolver.base_path().to_string_lossy().to_string(),
        originals_dir: resolver.originals_dir().to_string_lossy().to_string(),
        edited_dir: resolver.edited_dir().to_string_lossy().to_string(),
        thumbnails_dir: resolver.thumbnails_dir().to_string_lossy().to_string(),
        videos_dir: resolver.videos_dir().to_string_lossy().to_string(),
    })
}

/// Resolve a specific file path within the storage structure.
#[tauri::command]
pub fn resolve_storage_path(
    category: String,
    filename: String,
    state: State<'_, AppState>,
) -> CommandResult<String> {
    let safe_filename = match sanitize_filename(&filename) {
        Ok(name) => name,
        Err(e) => return CommandResult::err(e),
    };

    let path = match category.as_str() {
        "originals" => state.storage_service.resolve_original_path(&safe_filename),
        "edited" => state.storage_service.resolve_edited_path(&safe_filename),
        "thumbnails" => state.storage_service.resolve_thumbnail_path(&safe_filename),
        "videos" => state.storage_service.resolve_video_path(&safe_filename),
        _ => {
            return CommandResult::err(format!(
                "Unknown storage category: '{}'. Valid categories: originals, edited, thumbnails, videos",
                category
            ));
        }
    };
    CommandResult::ok(path.to_string_lossy().to_string())
}

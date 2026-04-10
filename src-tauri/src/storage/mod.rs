use std::path::PathBuf;

use crate::error::AppResult;

/// Ensure that the storage directories exist under the given base path.
/// Creates: originals/, edited/, thumbnails/
pub fn ensure_storage_dirs(base_path: &std::path::Path) -> AppResult<()> {
    let subdirs = ["originals", "edited", "thumbnails"];
    for dir in &subdirs {
        let path = base_path.join(dir);
        if !path.exists() {
            std::fs::create_dir_all(&path)?;
            tracing::info!("Created storage directory: {:?}", path);
        }
    }
    Ok(())
}

/// Get the originals directory path
pub fn originals_dir(base_path: &std::path::Path) -> PathBuf {
    base_path.join("originals")
}

/// Get the edited directory path
pub fn edited_dir(base_path: &std::path::Path) -> PathBuf {
    base_path.join("edited")
}

/// Get the thumbnails directory path
pub fn thumbnails_dir(base_path: &std::path::Path) -> PathBuf {
    base_path.join("thumbnails")
}

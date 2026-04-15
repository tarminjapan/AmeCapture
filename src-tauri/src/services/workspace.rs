use std::path::Path;

use crate::error::{AppError, AppResult};
use crate::models::workspace_item::WorkspaceItem;
use crate::repositories::workspace::WorkspaceRepository;

pub trait WorkspaceService: Send + Sync {
    fn get_all_items(&self) -> AppResult<Vec<WorkspaceItem>>;
    fn get_item(&self, id: &str) -> AppResult<Option<WorkspaceItem>>;
    fn add_item(&self, item: &WorkspaceItem) -> AppResult<()>;
    #[allow(dead_code)]
    fn update_item(&self, item: &WorkspaceItem) -> AppResult<()>;
    fn delete_item(&self, id: &str) -> AppResult<()>;
    fn rename_item(&self, id: &str, title: &str) -> AppResult<WorkspaceItem>;
    fn toggle_favorite(&self, id: &str, is_favorite: bool) -> AppResult<()>;
}

pub struct DefaultWorkspaceService<R: WorkspaceRepository> {
    repo: R,
}

impl<R: WorkspaceRepository> DefaultWorkspaceService<R> {
    pub fn new(repo: R) -> Self {
        Self { repo }
    }
}

fn delete_file_if_exists(path: &str) {
    let p = Path::new(path);
    if p.exists() {
        if let Err(e) = std::fs::remove_file(p) {
            tracing::warn!("Failed to delete file {}: {}", path, e);
        }
    }
}

fn sanitize_filename(title: &str) -> String {
    let sanitized: String = title
        .chars()
        .map(|c| match c {
            '<' | '>' | ':' | '"' | '/' | '\\' | '|' | '?' | '*' | '\0'..='\x1F' => '_',
            _ => c,
        })
        .collect();
    let trimmed = sanitized.trim_end_matches(['.', ' ']);
    if trimmed.is_empty() {
        return "unnamed".to_string();
    }
    let reserved = [
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8",
        "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    ];
    if reserved.contains(&trimmed.to_uppercase().as_str()) {
        format!("{}_", trimmed)
    } else {
        trimmed.to_string()
    }
}

fn rename_file_on_disk(old_path: &str, new_stem: &str) -> AppResult<String> {
    let old = Path::new(old_path);
    if !old.exists() {
        return Ok(old_path.to_string());
    }

    let parent = old.parent().unwrap_or(Path::new("."));
    let extension = old
        .extension()
        .map(|e| format!(".{}", e.to_string_lossy()))
        .unwrap_or_default();

    let safe_stem = sanitize_filename(new_stem);
    let new_filename = format!("{}{}", safe_stem, extension);
    let new_path = parent.join(&new_filename);

    if new_path == old {
        return Ok(old_path.to_string());
    }

    if new_path.exists() {
        for i in 1..1000u32 {
            let candidate_name = format!("{}_{}{}", safe_stem, i, extension);
            let candidate = parent.join(&candidate_name);
            if !candidate.exists() {
                std::fs::rename(old, &candidate)?;
                return Ok(candidate.to_string_lossy().to_string());
            }
        }
        return Err(AppError::Io(std::io::Error::new(
            std::io::ErrorKind::AlreadyExists,
            format!(
                "Could not find unique filename after 999 attempts for base name: {}",
                new_stem
            ),
        )));
    }

    std::fs::rename(old, &new_path)?;
    Ok(new_path.to_string_lossy().to_string())
}

fn rename_thumbnail_on_disk(old_path: &str, new_stem: &str) -> AppResult<String> {
    let thumb_stem = format!("{}_thumb", new_stem);
    rename_file_on_disk(old_path, &thumb_stem)
}

impl<R: WorkspaceRepository> WorkspaceService for DefaultWorkspaceService<R> {
    fn get_all_items(&self) -> AppResult<Vec<WorkspaceItem>> {
        tracing::debug!("Fetching all workspace items");
        let items = self.repo.get_all()?;
        tracing::debug!("Found {} workspace items", items.len());
        Ok(items)
    }

    fn get_item(&self, id: &str) -> AppResult<Option<WorkspaceItem>> {
        tracing::debug!("Fetching workspace item: {}", id);
        self.repo.get_by_id(id)
    }

    fn add_item(&self, item: &WorkspaceItem) -> AppResult<()> {
        tracing::info!("Adding workspace item: {} ({})", item.id, item.title);
        self.repo.add(item)
    }

    fn update_item(&self, item: &WorkspaceItem) -> AppResult<()> {
        tracing::info!("Updating workspace item: {}", item.id);
        self.repo.update(item)
    }

    fn delete_item(&self, id: &str) -> AppResult<()> {
        tracing::info!("Deleting workspace item: {}", id);
        let item_opt = self.repo.get_by_id(id)?;
        self.repo.delete(id)?;
        if let Some(item) = item_opt {
            delete_file_if_exists(&item.current_path);
            delete_file_if_exists(&item.original_path);
            if let Some(thumb) = &item.thumbnail_path {
                delete_file_if_exists(thumb);
            }
        }
        Ok(())
    }

    fn rename_item(&self, id: &str, title: &str) -> AppResult<WorkspaceItem> {
        tracing::info!("Renaming workspace item {} to '{}'", id, title);
        let mut item = self
            .repo
            .get_by_id(id)?
            .ok_or_else(|| AppError::NotFound(format!("Item not found: {}", id)))?;

        let safe_title = sanitize_filename(title);
        let same_file = item.original_path == item.current_path;

        let old_current = item.current_path.clone();
        let old_original = item.original_path.clone();
        let old_thumbnail = item.thumbnail_path.clone();

        item.current_path = rename_file_on_disk(&item.current_path, &safe_title)?;

        if same_file {
            item.original_path = item.current_path.clone();
        } else {
            item.original_path =
                rename_file_on_disk(&item.original_path, &safe_title).map_err(|e| {
                    tracing::error!(
                        "Failed to rename original file, rolling back current file: {}",
                        e
                    );
                    let _ = std::fs::rename(&item.current_path, &old_current);
                    e
                })?;
        }

        item.thumbnail_path = match &old_thumbnail {
            Some(thumb) => match rename_thumbnail_on_disk(thumb, &safe_title) {
                Ok(p) => Some(p),
                Err(e) => {
                    tracing::warn!("Failed to rename thumbnail file: {}", e);
                    Some(thumb.clone())
                }
            },
            None => None,
        };

        item.title = title.to_string();
        item.updated_at = chrono::Utc::now().to_rfc3339();

        match self.repo.update(&item) {
            Ok(()) => Ok(item),
            Err(db_err) => {
                tracing::error!(
                    "DB update failed after file rename, rolling back: {}",
                    db_err
                );
                if item.current_path != old_current {
                    let _ = std::fs::rename(&item.current_path, &old_current);
                }
                if !same_file && item.original_path != old_original {
                    let _ = std::fs::rename(&item.original_path, &old_original);
                }
                if let Some(ref new_thumb) = item.thumbnail_path {
                    if let Some(ref old_thumb) = old_thumbnail {
                        if new_thumb != old_thumb {
                            let _ = std::fs::rename(new_thumb, old_thumb);
                        }
                    }
                }
                Err(db_err)
            }
        }
    }

    fn toggle_favorite(&self, id: &str, is_favorite: bool) -> AppResult<()> {
        tracing::info!("Toggling favorite for item {} to {}", id, is_favorite);
        let mut item = self
            .repo
            .get_by_id(id)?
            .ok_or_else(|| AppError::NotFound(format!("Item not found: {}", id)))?;
        item.is_favorite = is_favorite;
        item.updated_at = chrono::Utc::now().to_rfc3339();
        self.repo.update(&item)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::db::migrations::run_migrations;
    use crate::models::workspace_item::WorkspaceItemType;
    use crate::repositories::workspace::SqliteWorkspaceRepository;
    use rusqlite::Connection;
    use std::sync::{Arc, Mutex};
    use tempfile::TempDir;

    fn setup_service() -> (DefaultWorkspaceService<SqliteWorkspaceRepository>, TempDir) {
        let conn = Connection::open_in_memory().unwrap();
        conn.execute_batch("PRAGMA foreign_keys=ON;").unwrap();
        run_migrations(&conn).unwrap();
        let repo = SqliteWorkspaceRepository::new(Arc::new(Mutex::new(conn)));
        let temp_dir = TempDir::new().unwrap();
        (DefaultWorkspaceService::new(repo), temp_dir)
    }

    fn create_test_file(dir: &Path, name: &str) -> String {
        let path = dir.join(name);
        std::fs::write(&path, b"test").unwrap();
        path.to_string_lossy().to_string()
    }

    fn sample_item_with_files(id: &str, base_dir: &Path) -> WorkspaceItem {
        let originals_dir = base_dir.join("originals");
        let edited_dir = base_dir.join("edited");
        let thumbnails_dir = base_dir.join("thumbnails");
        std::fs::create_dir_all(&originals_dir).unwrap();
        std::fs::create_dir_all(&edited_dir).unwrap();
        std::fs::create_dir_all(&thumbnails_dir).unwrap();

        let current = create_test_file(&edited_dir, "capture.png");
        let original = create_test_file(&originals_dir, "capture.png");
        let thumb = create_test_file(&thumbnails_dir, "capture_thumb.png");
        WorkspaceItem {
            id: id.to_string(),
            item_type: WorkspaceItemType::Image,
            original_path: original,
            current_path: current,
            thumbnail_path: Some(thumb),
            title: "Test Item".to_string(),
            created_at: "2026-01-01T00:00:00Z".to_string(),
            updated_at: "2026-01-01T00:00:00Z".to_string(),
            is_favorite: false,
            metadata_json: None,
        }
    }

    #[test]
    fn test_delete_removes_files_from_disk() {
        let (service, temp_dir) = setup_service();
        let item = sample_item_with_files("id-1", temp_dir.path());
        service.add_item(&item).unwrap();

        let original_path = item.original_path.clone();
        let current_path = item.current_path.clone();
        let thumb_path = item.thumbnail_path.clone().unwrap();

        assert!(Path::new(&original_path).exists());
        assert!(Path::new(&current_path).exists());
        assert!(Path::new(&thumb_path).exists());

        service.delete_item("id-1").unwrap();

        assert!(!Path::new(&original_path).exists());
        assert!(!Path::new(&current_path).exists());
        assert!(!Path::new(&thumb_path).exists());
        assert!(service.get_item("id-1").unwrap().is_none());
    }

    #[test]
    fn test_delete_nonexistent_item_is_ok() {
        let (service, _temp_dir) = setup_service();
        service.delete_item("nonexistent").unwrap();
    }

    #[test]
    fn test_delete_item_without_thumbnail() {
        let (service, temp_dir) = setup_service();
        let mut item = sample_item_with_files("id-1", temp_dir.path());
        item.thumbnail_path = None;
        service.add_item(&item).unwrap();

        service.delete_item("id-1").unwrap();
        assert!(service.get_item("id-1").unwrap().is_none());
    }

    #[test]
    fn test_rename_updates_title_and_files() {
        let (service, temp_dir) = setup_service();
        let item = sample_item_with_files("id-1", temp_dir.path());
        service.add_item(&item).unwrap();

        let updated = service.rename_item("id-1", "New Title").unwrap();
        assert_eq!(updated.title, "New Title");

        assert!(!Path::new(&item.current_path).exists());
        assert!(Path::new(&updated.current_path).exists());
        assert!(updated.current_path.contains("New Title"));
        assert!(updated.original_path.contains("New Title"));
        assert!(updated.thumbnail_path.unwrap().contains("New Title"));
    }

    #[test]
    fn test_rename_same_original_and_current() {
        let (service, temp_dir) = setup_service();
        let mut item = sample_item_with_files("id-1", temp_dir.path());
        item.original_path = item.current_path.clone();
        service.add_item(&item).unwrap();

        let updated = service.rename_item("id-1", "Renamed").unwrap();
        assert_eq!(updated.original_path, updated.current_path);
    }

    #[test]
    fn test_rename_nonexistent_item_fails() {
        let (service, _temp_dir) = setup_service();
        let result = service.rename_item("nonexistent", "Title");
        assert!(result.is_err());
    }

    #[test]
    fn test_sanitize_filename() {
        assert_eq!(sanitize_filename("Hello World"), "Hello World");
        assert_eq!(sanitize_filename("test<>:file"), "test___file");
        assert_eq!(sanitize_filename("valid-name_123"), "valid-name_123");
        assert_eq!(sanitize_filename(""), "unnamed");
        assert_eq!(sanitize_filename("..."), "unnamed");
        assert_eq!(sanitize_filename("file..."), "file");
        assert_eq!(sanitize_filename("a/b\\c"), "a_b_c");
    }

    #[test]
    fn test_rename_file_on_disk_nonexistent() {
        let result = rename_file_on_disk("/nonexistent/path/file.png", "new_name");
        assert_eq!(result.unwrap(), "/nonexistent/path/file.png");
    }

    #[test]
    fn test_rename_file_collision() {
        let temp = TempDir::new().unwrap();
        let path1 = temp.path().join("file.txt");
        let path2 = temp.path().join("target.txt");
        std::fs::write(&path1, b"data1").unwrap();
        std::fs::write(&path2, b"data2").unwrap();

        let result = rename_file_on_disk(&path1.to_string_lossy(), "target").unwrap();

        assert!(result.contains("target_1.txt"));
        assert!(Path::new(&result).exists());
    }

    #[test]
    fn test_toggle_favorite() {
        let (service, temp_dir) = setup_service();
        let item = sample_item_with_files("id-1", temp_dir.path());
        service.add_item(&item).unwrap();

        let found = service.get_item("id-1").unwrap().unwrap();
        assert!(!found.is_favorite);

        service.toggle_favorite("id-1", true).unwrap();
        let found = service.get_item("id-1").unwrap().unwrap();
        assert!(found.is_favorite);

        service.toggle_favorite("id-1", false).unwrap();
        let found = service.get_item("id-1").unwrap().unwrap();
        assert!(!found.is_favorite);
    }

    #[test]
    fn test_toggle_favorite_nonexistent_fails() {
        let (service, _temp_dir) = setup_service();
        let result = service.toggle_favorite("nonexistent", true);
        assert!(result.is_err());
    }
}

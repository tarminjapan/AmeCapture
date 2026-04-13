use std::sync::{Arc, Mutex};

use rusqlite::params;

use crate::error::AppResult;
use crate::models::workspace_item::{WorkspaceItem, WorkspaceItemType};

/// Repository trait for workspace item data access
pub trait WorkspaceRepository: Send + Sync {
    fn get_all(&self) -> AppResult<Vec<WorkspaceItem>>;
    fn get_by_id(&self, id: &str) -> AppResult<Option<WorkspaceItem>>;
    fn add(&self, item: &WorkspaceItem) -> AppResult<()>;
    fn update(&self, item: &WorkspaceItem) -> AppResult<()>;
    fn delete(&self, id: &str) -> AppResult<()>;
    fn get_by_ids(&self, ids: &[String]) -> AppResult<Vec<WorkspaceItem>>;
}

/// SQLite implementation of WorkspaceRepository
pub struct SqliteWorkspaceRepository {
    conn: Arc<Mutex<rusqlite::Connection>>,
}

impl SqliteWorkspaceRepository {
    pub fn new(conn: Arc<Mutex<rusqlite::Connection>>) -> Self {
        Self { conn }
    }
}

impl WorkspaceRepository for SqliteWorkspaceRepository {
    fn get_all(&self) -> AppResult<Vec<WorkspaceItem>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare(
            "SELECT id, type, original_path, current_path, thumbnail_path,
                    title, created_at, updated_at, is_favorite, metadata_json
             FROM workspace_items
             ORDER BY created_at DESC",
        )?;

        let items = stmt
            .query_map([], |row| {
                let item_type_str: String = row.get(1)?;
                let item_type = match item_type_str.as_str() {
                    "video" => WorkspaceItemType::Video,
                    _ => WorkspaceItemType::Image,
                };
                Ok(WorkspaceItem {
                    id: row.get(0)?,
                    item_type,
                    original_path: row.get(2)?,
                    current_path: row.get(3)?,
                    thumbnail_path: row.get(4)?,
                    title: row.get(5)?,
                    created_at: row.get(6)?,
                    updated_at: row.get(7)?,
                    is_favorite: row.get::<_, i32>(8)? != 0,
                    metadata_json: row.get(9)?,
                })
            })?
            .filter_map(|i| i.ok())
            .collect();

        Ok(items)
    }

    fn get_by_id(&self, id: &str) -> AppResult<Option<WorkspaceItem>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare(
            "SELECT id, type, original_path, current_path, thumbnail_path,
                    title, created_at, updated_at, is_favorite, metadata_json
             FROM workspace_items WHERE id = ?1",
        )?;

        let result = stmt.query_row(params![id], |row| {
            let item_type_str: String = row.get(1)?;
            let item_type = match item_type_str.as_str() {
                "video" => WorkspaceItemType::Video,
                _ => WorkspaceItemType::Image,
            };
            Ok(WorkspaceItem {
                id: row.get(0)?,
                item_type,
                original_path: row.get(2)?,
                current_path: row.get(3)?,
                thumbnail_path: row.get(4)?,
                title: row.get(5)?,
                created_at: row.get(6)?,
                updated_at: row.get(7)?,
                is_favorite: row.get::<_, i32>(8)? != 0,
                metadata_json: row.get(9)?,
            })
        });

        match result {
            Ok(item) => Ok(Some(item)),
            Err(rusqlite::Error::QueryReturnedNoRows) => Ok(None),
            Err(e) => Err(e.into()),
        }
    }

    fn add(&self, item: &WorkspaceItem) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT INTO workspace_items
                (id, type, original_path, current_path, thumbnail_path,
                 title, created_at, updated_at, is_favorite, metadata_json)
             VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10)",
            params![
                item.id,
                item.item_type.to_string(),
                item.original_path,
                item.current_path,
                item.thumbnail_path,
                item.title,
                item.created_at,
                item.updated_at,
                item.is_favorite as i32,
                item.metadata_json,
            ],
        )?;
        Ok(())
    }

    fn update(&self, item: &WorkspaceItem) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "UPDATE workspace_items SET
                type = ?1, original_path = ?2, current_path = ?3, thumbnail_path = ?4,
                title = ?5, updated_at = ?6, is_favorite = ?7, metadata_json = ?8
             WHERE id = ?9",
            params![
                item.item_type.to_string(),
                item.original_path,
                item.current_path,
                item.thumbnail_path,
                item.title,
                item.updated_at,
                item.is_favorite as i32,
                item.metadata_json,
                item.id,
            ],
        )?;
        Ok(())
    }

    fn delete(&self, id: &str) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM workspace_items WHERE id = ?1", params![id])?;
        Ok(())
    }

    fn get_by_ids(&self, ids: &[String]) -> AppResult<Vec<WorkspaceItem>> {
        if ids.is_empty() {
            return Ok(Vec::new());
        }
        let conn = self.conn.lock().unwrap();
        let placeholders: Vec<String> = ids
            .iter()
            .enumerate()
            .map(|(i, _)| format!("?{}", i + 1))
            .collect();
        let sql = format!(
            "SELECT id, type, original_path, current_path, thumbnail_path,
                    title, created_at, updated_at, is_favorite, metadata_json
             FROM workspace_items WHERE id IN ({})",
            placeholders.join(", ")
        );
        let mut stmt = conn.prepare(&sql)?;
        let param_refs: Vec<&dyn rusqlite::ToSql> =
            ids.iter().map(|id| id as &dyn rusqlite::ToSql).collect();
        let items = stmt
            .query_map(param_refs.as_slice(), |row| {
                let item_type_str: String = row.get(1)?;
                let item_type = match item_type_str.as_str() {
                    "video" => WorkspaceItemType::Video,
                    _ => WorkspaceItemType::Image,
                };
                Ok(WorkspaceItem {
                    id: row.get(0)?,
                    item_type,
                    original_path: row.get(2)?,
                    current_path: row.get(3)?,
                    thumbnail_path: row.get(4)?,
                    title: row.get(5)?,
                    created_at: row.get(6)?,
                    updated_at: row.get(7)?,
                    is_favorite: row.get::<_, i32>(8)? != 0,
                    metadata_json: row.get(9)?,
                })
            })?
            .collect::<Result<Vec<_>, _>>()?;
        Ok(items)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::db::migrations::run_migrations;
    use rusqlite::Connection;

    fn setup_repo() -> SqliteWorkspaceRepository {
        let conn = Connection::open_in_memory().unwrap();
        conn.execute_batch("PRAGMA foreign_keys=ON;").unwrap();
        run_migrations(&conn).unwrap();
        SqliteWorkspaceRepository::new(Arc::new(Mutex::new(conn)))
    }

    fn sample_item(id: &str) -> WorkspaceItem {
        WorkspaceItem {
            id: id.to_string(),
            item_type: WorkspaceItemType::Image,
            original_path: "/original/test.png".to_string(),
            current_path: "/current/test.png".to_string(),
            thumbnail_path: Some("/thumb/test_thumb.png".to_string()),
            title: "Test Item".to_string(),
            created_at: "2026-01-01T00:00:00Z".to_string(),
            updated_at: "2026-01-01T00:00:00Z".to_string(),
            is_favorite: false,
            metadata_json: None,
        }
    }

    #[test]
    fn test_add_and_get_by_id() {
        let repo = setup_repo();
        let item = sample_item("id-1");
        repo.add(&item).unwrap();

        let found = repo.get_by_id("id-1").unwrap();
        assert!(found.is_some());
        let found = found.unwrap();
        assert_eq!(found.id, "id-1");
        assert_eq!(found.title, "Test Item");
        assert_eq!(found.item_type, WorkspaceItemType::Image);
        assert_eq!(found.original_path, "/original/test.png");
        assert_eq!(found.current_path, "/current/test.png");
        assert_eq!(
            found.thumbnail_path,
            Some("/thumb/test_thumb.png".to_string())
        );
        assert!(!found.is_favorite);
    }

    #[test]
    fn test_get_by_id_not_found() {
        let repo = setup_repo();
        let result = repo.get_by_id("nonexistent").unwrap();
        assert!(result.is_none());
    }

    #[test]
    fn test_get_all_empty() {
        let repo = setup_repo();
        let items = repo.get_all().unwrap();
        assert!(items.is_empty());
    }

    #[test]
    fn test_get_all_multiple_items() {
        let repo = setup_repo();
        repo.add(&sample_item("id-1")).unwrap();
        repo.add(&sample_item("id-2")).unwrap();
        repo.add(&sample_item("id-3")).unwrap();

        let items = repo.get_all().unwrap();
        assert_eq!(items.len(), 3);
    }

    #[test]
    fn test_update() {
        let repo = setup_repo();
        let mut item = sample_item("id-1");
        repo.add(&item).unwrap();

        item.title = "Updated Title".to_string();
        item.is_favorite = true;
        item.updated_at = "2026-01-02T00:00:00Z".to_string();
        repo.update(&item).unwrap();

        let found = repo.get_by_id("id-1").unwrap().unwrap();
        assert_eq!(found.title, "Updated Title");
        assert!(found.is_favorite);
        assert_eq!(found.updated_at, "2026-01-02T00:00:00Z");
    }

    #[test]
    fn test_delete() {
        let repo = setup_repo();
        repo.add(&sample_item("id-1")).unwrap();
        assert!(repo.get_by_id("id-1").unwrap().is_some());

        repo.delete("id-1").unwrap();
        assert!(repo.get_by_id("id-1").unwrap().is_none());
    }

    #[test]
    fn test_delete_nonexistent_is_ok() {
        let repo = setup_repo();
        repo.delete("nonexistent").unwrap();
    }

    #[test]
    fn test_video_type_roundtrip() {
        let repo = setup_repo();
        let mut item = sample_item("id-v1");
        item.item_type = WorkspaceItemType::Video;
        repo.add(&item).unwrap();

        let found = repo.get_by_id("id-v1").unwrap().unwrap();
        assert_eq!(found.item_type, WorkspaceItemType::Video);
    }

    #[test]
    fn test_null_optional_fields() {
        let repo = setup_repo();
        let item = WorkspaceItem {
            id: "id-opt".to_string(),
            item_type: WorkspaceItemType::Image,
            original_path: "/original/test.png".to_string(),
            current_path: "/current/test.png".to_string(),
            thumbnail_path: None,
            title: "No Thumbnail".to_string(),
            created_at: "2026-01-01T00:00:00Z".to_string(),
            updated_at: "2026-01-01T00:00:00Z".to_string(),
            is_favorite: false,
            metadata_json: None,
        };
        repo.add(&item).unwrap();

        let found = repo.get_by_id("id-opt").unwrap().unwrap();
        assert!(found.thumbnail_path.is_none());
        assert!(found.metadata_json.is_none());
    }
}

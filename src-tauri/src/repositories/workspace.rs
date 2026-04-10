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
}

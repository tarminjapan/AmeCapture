use rusqlite::{params, Connection};
use crate::utils::error::AppResult;
use super::models::WorkspaceItem;

/// Get all workspace items ordered by created_at desc
pub fn get_all_items(conn: &Connection) -> AppResult<Vec<WorkspaceItem>> {
    let mut stmt = conn.prepare(
        "SELECT id, type, original_path, current_path, thumbnail_path,
                title, created_at, updated_at, is_favorite, metadata_json
         FROM workspace_items
         ORDER BY created_at DESC"
    )?;

    let items = stmt.query_map([], |row| {
        Ok(WorkspaceItem {
            id: row.get(0)?,
            item_type: row.get(1)?,
            original_path: row.get(2)?,
            current_path: row.get(3)?,
            thumbnail_path: row.get(4)?,
            title: row.get(5)?,
            created_at: row.get(6)?,
            updated_at: row.get(7)?,
            is_favorite: row.get::<_, i32>(8)? != 0,
            metadata_json: row.get(9)?,
        })
    })?.filter_map(|i| i.ok()).collect();

    Ok(items)
}

/// Insert a new workspace item
pub fn insert_item(conn: &Connection, item: &WorkspaceItem) -> AppResult<()> {
    conn.execute(
        "INSERT INTO workspace_items
            (id, type, original_path, current_path, thumbnail_path,
             title, created_at, updated_at, is_favorite, metadata_json)
         VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10)",
        params![
            item.id,
            item.item_type,
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

/// Delete a workspace item by id
pub fn delete_item(conn: &Connection, id: &str) -> AppResult<()> {
    conn.execute("DELETE FROM workspace_items WHERE id = ?1", params![id])?;
    Ok(())
}

/// Rename a workspace item
pub fn rename_item(conn: &Connection, id: &str, title: &str) -> AppResult<()> {
    conn.execute(
        "UPDATE workspace_items SET title = ?1, updated_at = ?2 WHERE id = ?3",
        params![title, chrono::Utc::now().to_rfc3339(), id],
    )?;
    Ok(())
}

/// Toggle favorite status
pub fn toggle_favorite(conn: &Connection, id: &str, is_favorite: bool) -> AppResult<()> {
    conn.execute(
        "UPDATE workspace_items SET is_favorite = ?1, updated_at = ?2 WHERE id = ?3",
        params![is_favorite as i32, chrono::Utc::now().to_rfc3339(), id],
    )?;
    Ok(())
}
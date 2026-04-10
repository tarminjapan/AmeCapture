use std::sync::{Arc, Mutex};

use rusqlite::params;

use crate::error::AppResult;
use crate::models::tag::Tag;

/// Repository trait for tag data access
pub trait TagRepository: Send + Sync {
    fn get_all(&self) -> AppResult<Vec<Tag>>;
    fn get_by_id(&self, id: &str) -> AppResult<Option<Tag>>;
    fn add(&self, tag: &Tag) -> AppResult<()>;
    fn delete(&self, id: &str) -> AppResult<()>;
}

/// SQLite implementation of TagRepository
pub struct SqliteTagRepository {
    conn: Arc<Mutex<rusqlite::Connection>>,
}

impl SqliteTagRepository {
    pub fn new(conn: Arc<Mutex<rusqlite::Connection>>) -> Self {
        Self { conn }
    }
}

impl TagRepository for SqliteTagRepository {
    fn get_all(&self) -> AppResult<Vec<Tag>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT id, name FROM tags ORDER BY name")?;

        let tags = stmt
            .query_map([], |row| {
                Ok(Tag {
                    id: row.get(0)?,
                    name: row.get(1)?,
                })
            })?
            .filter_map(|t| t.ok())
            .collect();

        Ok(tags)
    }

    fn get_by_id(&self, id: &str) -> AppResult<Option<Tag>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT id, name FROM tags WHERE id = ?1")?;

        let result = stmt.query_row(params![id], |row| {
            Ok(Tag {
                id: row.get(0)?,
                name: row.get(1)?,
            })
        });

        match result {
            Ok(tag) => Ok(Some(tag)),
            Err(rusqlite::Error::QueryReturnedNoRows) => Ok(None),
            Err(e) => Err(e.into()),
        }
    }

    fn add(&self, tag: &Tag) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT INTO tags (id, name) VALUES (?1, ?2)",
            params![tag.id, tag.name],
        )?;
        Ok(())
    }

    fn delete(&self, id: &str) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM tags WHERE id = ?1", params![id])?;
        Ok(())
    }
}

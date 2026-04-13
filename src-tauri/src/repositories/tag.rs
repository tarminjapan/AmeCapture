use std::sync::{Arc, Mutex};

use rusqlite::params;

use crate::error::AppResult;
use crate::models::tag::Tag;

/// Repository trait for tag data access
pub trait TagRepository: Send + Sync {
    fn get_all(&self) -> AppResult<Vec<Tag>>;
    fn get_by_id(&self, id: &str) -> AppResult<Option<Tag>>;
    fn find_by_name(&self, name: &str) -> AppResult<Option<Tag>>;
    fn add(&self, tag: &Tag) -> AppResult<()>;
    fn delete(&self, id: &str) -> AppResult<()>;
    fn get_tags_for_item(&self, item_id: &str) -> AppResult<Vec<Tag>>;
    fn add_tag_to_item(&self, item_id: &str, tag_id: &str) -> AppResult<()>;
    fn remove_tag_from_item(&self, item_id: &str, tag_id: &str) -> AppResult<()>;
    fn set_tags_for_item(&self, item_id: &str, tag_ids: &[String]) -> AppResult<()>;
    fn get_item_ids_by_tag(&self, tag_id: &str) -> AppResult<Vec<String>>;
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

    fn find_by_name(&self, name: &str) -> AppResult<Option<Tag>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT id, name FROM tags WHERE name = ?1")?;

        let result = stmt.query_row(params![name], |row| {
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

    fn get_tags_for_item(&self, item_id: &str) -> AppResult<Vec<Tag>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare(
            "SELECT t.id, t.name FROM tags t
             INNER JOIN workspace_item_tags wit ON t.id = wit.tag_id
             WHERE wit.workspace_item_id = ?1
             ORDER BY t.name",
        )?;

        let tags = stmt
            .query_map(params![item_id], |row| {
                Ok(Tag {
                    id: row.get(0)?,
                    name: row.get(1)?,
                })
            })?
            .filter_map(|t| t.ok())
            .collect();

        Ok(tags)
    }

    fn add_tag_to_item(&self, item_id: &str, tag_id: &str) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT OR IGNORE INTO workspace_item_tags (workspace_item_id, tag_id) VALUES (?1, ?2)",
            params![item_id, tag_id],
        )?;
        Ok(())
    }

    fn remove_tag_from_item(&self, item_id: &str, tag_id: &str) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "DELETE FROM workspace_item_tags WHERE workspace_item_id = ?1 AND tag_id = ?2",
            params![item_id, tag_id],
        )?;
        Ok(())
    }

    fn set_tags_for_item(&self, item_id: &str, tag_ids: &[String]) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "DELETE FROM workspace_item_tags WHERE workspace_item_id = ?1",
            params![item_id],
        )?;
        for tag_id in tag_ids {
            conn.execute(
                "INSERT OR IGNORE INTO workspace_item_tags (workspace_item_id, tag_id) VALUES (?1, ?2)",
                params![item_id, tag_id],
            )?;
        }
        Ok(())
    }

    fn get_item_ids_by_tag(&self, tag_id: &str) -> AppResult<Vec<String>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt =
            conn.prepare("SELECT workspace_item_id FROM workspace_item_tags WHERE tag_id = ?1")?;

        let ids = stmt
            .query_map(params![tag_id], |row| row.get(0))?
            .filter_map(|r| r.ok())
            .collect();

        Ok(ids)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::db::migrations::run_migrations;
    use rusqlite::Connection;

    fn setup_repo() -> SqliteTagRepository {
        let conn = Connection::open_in_memory().unwrap();
        conn.execute_batch("PRAGMA foreign_keys=ON;").unwrap();
        run_migrations(&conn).unwrap();
        SqliteTagRepository::new(Arc::new(Mutex::new(conn)))
    }

    fn insert_workspace_item(conn: &Connection, id: &str) {
        conn.execute(
            "INSERT INTO workspace_items (id, type, original_path, current_path, title, created_at, updated_at)
             VALUES (?1, 'image', '/a.png', '/a.png', 'test', '2026-01-01', '2026-01-01')",
            params![id],
        )
        .unwrap();
    }

    #[test]
    fn test_add_and_get_all() {
        let repo = setup_repo();
        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();
        repo.add(&Tag {
            id: "t2".into(),
            name: "tag2".into(),
        })
        .unwrap();

        let tags = repo.get_all().unwrap();
        assert_eq!(tags.len(), 2);
        assert_eq!(tags[0].name, "tag1");
        assert_eq!(tags[1].name, "tag2");
    }

    #[test]
    fn test_get_by_id() {
        let repo = setup_repo();
        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();

        let found = repo.get_by_id("t1").unwrap();
        assert!(found.is_some());
        assert_eq!(found.unwrap().name, "tag1");

        assert!(repo.get_by_id("nonexistent").unwrap().is_none());
    }

    #[test]
    fn test_find_by_name() {
        let repo = setup_repo();
        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();

        let found = repo.find_by_name("tag1").unwrap();
        assert!(found.is_some());

        assert!(repo.find_by_name("nonexistent").unwrap().is_none());
    }

    #[test]
    fn test_delete() {
        let repo = setup_repo();
        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();
        repo.delete("t1").unwrap();
        assert!(repo.get_by_id("t1").unwrap().is_none());
    }

    #[test]
    fn test_add_tag_to_item_and_get() {
        let repo = setup_repo();
        let conn = repo.conn.lock().unwrap();
        insert_workspace_item(&conn, "w1");
        drop(conn);

        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();
        repo.add(&Tag {
            id: "t2".into(),
            name: "tag2".into(),
        })
        .unwrap();

        repo.add_tag_to_item("w1", "t1").unwrap();
        repo.add_tag_to_item("w1", "t2").unwrap();

        let tags = repo.get_tags_for_item("w1").unwrap();
        assert_eq!(tags.len(), 2);
    }

    #[test]
    fn test_remove_tag_from_item() {
        let repo = setup_repo();
        let conn = repo.conn.lock().unwrap();
        insert_workspace_item(&conn, "w1");
        drop(conn);

        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();
        repo.add_tag_to_item("w1", "t1").unwrap();
        assert_eq!(repo.get_tags_for_item("w1").unwrap().len(), 1);

        repo.remove_tag_from_item("w1", "t1").unwrap();
        assert_eq!(repo.get_tags_for_item("w1").unwrap().len(), 0);
    }

    #[test]
    fn test_set_tags_for_item() {
        let repo = setup_repo();
        let conn = repo.conn.lock().unwrap();
        insert_workspace_item(&conn, "w1");
        drop(conn);

        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();
        repo.add(&Tag {
            id: "t2".into(),
            name: "tag2".into(),
        })
        .unwrap();
        repo.add(&Tag {
            id: "t3".into(),
            name: "tag3".into(),
        })
        .unwrap();

        repo.set_tags_for_item("w1", &["t1".into(), "t2".into()])
            .unwrap();
        let tags = repo.get_tags_for_item("w1").unwrap();
        assert_eq!(tags.len(), 2);

        repo.set_tags_for_item("w1", &["t3".into()]).unwrap();
        let tags = repo.get_tags_for_item("w1").unwrap();
        assert_eq!(tags.len(), 1);
        assert_eq!(tags[0].name, "tag3");
    }

    #[test]
    fn test_get_item_ids_by_tag() {
        let repo = setup_repo();
        let conn = repo.conn.lock().unwrap();
        insert_workspace_item(&conn, "w1");
        insert_workspace_item(&conn, "w2");
        drop(conn);

        repo.add(&Tag {
            id: "t1".into(),
            name: "tag1".into(),
        })
        .unwrap();
        repo.add_tag_to_item("w1", "t1").unwrap();
        repo.add_tag_to_item("w2", "t1").unwrap();

        let ids = repo.get_item_ids_by_tag("t1").unwrap();
        assert_eq!(ids.len(), 2);
        assert!(ids.contains(&"w1".to_string()));
        assert!(ids.contains(&"w2".to_string()));
    }
}

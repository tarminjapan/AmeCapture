use crate::error::AppResult;
use rusqlite::Connection;

/// Run database migrations
pub fn run_migrations(conn: &Connection) -> AppResult<()> {
    conn.execute_batch(
        "CREATE TABLE IF NOT EXISTS workspace_items (
            id TEXT PRIMARY KEY,
            type TEXT NOT NULL DEFAULT 'image',
            original_path TEXT NOT NULL,
            current_path TEXT NOT NULL,
            thumbnail_path TEXT,
            title TEXT NOT NULL,
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL,
            is_favorite INTEGER NOT NULL DEFAULT 0,
            metadata_json TEXT
        );

        CREATE TABLE IF NOT EXISTS tags (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL UNIQUE
        );

        CREATE TABLE IF NOT EXISTS workspace_item_tags (
            workspace_item_id TEXT NOT NULL,
            tag_id TEXT NOT NULL,
            PRIMARY KEY (workspace_item_id, tag_id),
            FOREIGN KEY (workspace_item_id) REFERENCES workspace_items(id) ON DELETE CASCADE,
            FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS app_settings (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_workspace_items_created_at
            ON workspace_items(created_at DESC);
        CREATE INDEX IF NOT EXISTS idx_workspace_items_is_favorite
            ON workspace_items(is_favorite);",
    )?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    use rusqlite::Connection;

    fn setup_db() -> Connection {
        let conn = Connection::open_in_memory().unwrap();
        conn.execute_batch("PRAGMA foreign_keys=ON;").unwrap();
        conn
    }

    #[test]
    fn test_migrations_create_all_tables() {
        let conn = setup_db();
        run_migrations(&conn).unwrap();

        let tables: Vec<String> = {
            let mut stmt = conn
                .prepare("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")
                .unwrap();
            stmt.query_map([], |row| row.get::<_, String>(0))
                .unwrap()
                .filter_map(|r| r.ok())
                .collect()
        };

        assert!(tables.contains(&"workspace_items".to_string()));
        assert!(tables.contains(&"tags".to_string()));
        assert!(tables.contains(&"workspace_item_tags".to_string()));
        assert!(tables.contains(&"app_settings".to_string()));
    }

    #[test]
    fn test_migrations_create_indexes() {
        let conn = setup_db();
        run_migrations(&conn).unwrap();

        let indexes: Vec<String> = {
            let mut stmt = conn
                .prepare("SELECT name FROM sqlite_master WHERE type='index' AND name LIKE 'idx_%' ORDER BY name")
                .unwrap();
            stmt.query_map([], |row| row.get::<_, String>(0))
                .unwrap()
                .filter_map(|r| r.ok())
                .collect()
        };

        assert!(indexes.contains(&"idx_workspace_items_created_at".to_string()));
        assert!(indexes.contains(&"idx_workspace_items_is_favorite".to_string()));
    }

    #[test]
    fn test_migrations_idempotent() {
        let conn = setup_db();
        run_migrations(&conn).unwrap();
        run_migrations(&conn).unwrap();

        let count: i64 = conn
            .query_row(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'",
                [],
                |row| row.get(0),
            )
            .unwrap();
        assert_eq!(count, 4);
    }

    #[test]
    fn test_foreign_key_cascade_delete() {
        let conn = setup_db();
        run_migrations(&conn).unwrap();

        conn.execute(
            "INSERT INTO workspace_items (id, type, original_path, current_path, title, created_at, updated_at) VALUES ('w1', 'image', '/a.png', '/a.png', 'test', '2026-01-01', '2026-01-01')",
            [],
        )
        .unwrap();
        conn.execute("INSERT INTO tags (id, name) VALUES ('t1', 'tag1')", [])
            .unwrap();
        conn.execute(
            "INSERT INTO workspace_item_tags (workspace_item_id, tag_id) VALUES ('w1', 't1')",
            [],
        )
        .unwrap();

        conn.execute("DELETE FROM workspace_items WHERE id = 'w1'", [])
            .unwrap();

        let count: i64 = conn
            .query_row("SELECT COUNT(*) FROM workspace_item_tags", [], |row| {
                row.get(0)
            })
            .unwrap();
        assert_eq!(count, 0);
    }

    #[test]
    fn test_foreign_key_constraint_prevents_invalid_tag() {
        let conn = setup_db();
        run_migrations(&conn).unwrap();

        conn.execute(
            "INSERT INTO workspace_items (id, type, original_path, current_path, title, created_at, updated_at) VALUES ('w1', 'image', '/a.png', '/a.png', 'test', '2026-01-01', '2026-01-01')",
            [],
        )
        .unwrap();

        let result = conn.execute(
            "INSERT INTO workspace_item_tags (workspace_item_id, tag_id) VALUES ('w1', 'nonexistent')",
            [],
        );

        assert!(result.is_err());
    }
}

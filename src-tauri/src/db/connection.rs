use rusqlite::Connection;
use std::path::Path;

use crate::error::AppResult;

/// Create a new database connection with WAL journal mode and foreign keys enabled
pub fn create_connection(db_path: &Path) -> AppResult<Connection> {
    let conn = Connection::open(db_path)?;
    conn.execute_batch("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;")?;
    Ok(conn)
}

#[cfg(test)]
mod tests {
    use super::*;
    use std::fs;

    #[test]
    fn test_db_created_when_not_exists() {
        let temp = tempfile::tempdir().unwrap();
        let db_path = temp.path().join("test.db");
        assert!(!db_path.exists());

        let conn = create_connection(&db_path).unwrap();
        conn.close().unwrap();

        assert!(db_path.exists());
    }

    #[test]
    fn test_existing_db_not_recreated() {
        let temp = tempfile::tempdir().unwrap();
        let db_path = temp.path().join("test.db");

        let conn1 = create_connection(&db_path).unwrap();
        conn1
            .execute_batch("CREATE TABLE test_marker (id INTEGER);")
            .unwrap();
        conn1
            .execute_batch("INSERT INTO test_marker VALUES (42);")
            .unwrap();
        conn1.close().unwrap();

        let original_metadata = fs::metadata(&db_path).unwrap();
        let original_modified = original_metadata.modified().unwrap();

        std::thread::sleep(std::time::Duration::from_millis(50));

        let conn2 = create_connection(&db_path).unwrap();
        let count: i64 = conn2
            .query_row("SELECT COUNT(*) FROM test_marker", [], |row| row.get(0))
            .unwrap();
        conn2.close().unwrap();

        assert_eq!(count, 1);

        let new_metadata = fs::metadata(&db_path).unwrap();
        let new_modified = new_metadata.modified().unwrap();
        assert_eq!(original_modified, new_modified);
    }

    #[test]
    fn test_foreign_keys_enabled() {
        let temp = tempfile::tempdir().unwrap();
        let db_path = temp.path().join("test.db");
        let conn = create_connection(&db_path).unwrap();

        let fk_enabled: i64 = conn
            .query_row("PRAGMA foreign_keys;", [], |row| row.get(0))
            .unwrap();
        conn.close().unwrap();

        assert_eq!(fk_enabled, 1);
    }

    #[test]
    fn test_wal_journal_mode() {
        let temp = tempfile::tempdir().unwrap();
        let db_path = temp.path().join("test.db");
        let conn = create_connection(&db_path).unwrap();

        let journal_mode: String = conn
            .query_row("PRAGMA journal_mode;", [], |row| row.get(0))
            .unwrap();
        conn.close().unwrap();

        assert_eq!(journal_mode.to_lowercase(), "wal");
    }
}

use rusqlite::Connection;
use std::path::Path;

use crate::utils::error::AppResult;

/// Create a new database connection
pub fn create_connection(db_path: &Path) -> AppResult<Connection> {
    let conn = Connection::open(db_path)?;
    conn.execute_batch("PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;")?;
    Ok(conn)
}
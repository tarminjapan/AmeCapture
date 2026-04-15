use std::sync::{Arc, Mutex};

use rusqlite::params;

use crate::error::AppResult;

/// Repository trait for settings data access
pub trait SettingsRepository: Send + Sync {
    fn set(&self, key: &str, value: &str) -> AppResult<()>;
    fn get_all(&self) -> AppResult<Vec<(String, String)>>;
}

/// SQLite implementation of SettingsRepository
pub struct SqliteSettingsRepository {
    conn: Arc<Mutex<rusqlite::Connection>>,
}

impl SqliteSettingsRepository {
    pub fn new(conn: Arc<Mutex<rusqlite::Connection>>) -> Self {
        Self { conn }
    }
}

impl SettingsRepository for SqliteSettingsRepository {
    fn set(&self, key: &str, value: &str) -> AppResult<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute(
            "INSERT INTO app_settings (key, value) VALUES (?1, ?2)
             ON CONFLICT(key) DO UPDATE SET value = ?2",
            params![key, value],
        )?;
        Ok(())
    }

    fn get_all(&self) -> AppResult<Vec<(String, String)>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT key, value FROM app_settings ORDER BY key")?;

        let settings = stmt
            .query_map([], |row| {
                Ok((row.get::<_, String>(0)?, row.get::<_, String>(1)?))
            })?
            .filter_map(|s| s.ok())
            .collect();

        Ok(settings)
    }
}

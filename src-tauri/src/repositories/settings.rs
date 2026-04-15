use std::sync::{Arc, Mutex};

use rusqlite::params;

use crate::error::{AppError, AppResult};

/// Repository trait for settings data access
#[allow(dead_code)]
pub trait SettingsRepository: Send + Sync {
    fn get(&self, key: &str) -> AppResult<Option<String>>;
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
    fn get(&self, key: &str) -> AppResult<Option<String>> {
        let conn = self.conn.lock().unwrap();
        let mut stmt = conn.prepare("SELECT value FROM app_settings WHERE key = ?1")?;

        let result = stmt.query_row(params![key], |row| row.get::<_, String>(0));

        match result {
            Ok(value) => Ok(Some(value)),
            Err(rusqlite::Error::QueryReturnedNoRows) => Ok(None),
            Err(e) => Err(AppError::from(e)),
        }
    }

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

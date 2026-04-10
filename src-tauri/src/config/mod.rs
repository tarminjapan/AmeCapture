pub mod app_settings;

use std::path::Path;

use crate::error::AppResult;
use app_settings::AppSettings;

/// Load settings from appsettings.json, falling back to defaults if the file doesn't exist.
pub fn load_settings_from_file(settings_path: &Path) -> AppResult<AppSettings> {
    if settings_path.exists() {
        let content = std::fs::read_to_string(settings_path)?;
        let settings: AppSettings = serde_json::from_str(&content)?;
        tracing::info!("Loaded settings from {:?}", settings_path);
        Ok(settings)
    } else {
        tracing::info!(
            "Settings file not found at {:?}, using defaults",
            settings_path
        );
        let settings = AppSettings::default();
        // Create the file with defaults so it exists next time
        save_settings_to_file(settings_path, &settings).ok();
        Ok(settings)
    }
}

/// Save settings to appsettings.json
pub fn save_settings_to_file(settings_path: &Path, settings: &AppSettings) -> AppResult<()> {
    if let Some(parent) = settings_path.parent() {
        std::fs::create_dir_all(parent)?;
    }
    let content = serde_json::to_string_pretty(settings)?;
    std::fs::write(settings_path, content)?;
    tracing::info!("Settings saved to {:?}", settings_path);
    Ok(())
}

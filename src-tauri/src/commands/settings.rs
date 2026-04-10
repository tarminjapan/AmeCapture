use tauri::State;

use crate::config::app_settings::AppSettings;
use crate::db::DbState;
use crate::utils::error::CommandResult;

#[tauri::command]
pub fn get_settings() -> CommandResult<AppSettings> {
    // TODO: Load from database
    CommandResult::ok(AppSettings::default())
}

#[tauri::command]
pub fn save_settings(_settings: AppSettings, _state: State<'_, DbState>) -> CommandResult<()> {
    // TODO: Save to database
    log::info!("Settings save requested");
    CommandResult::success()
}

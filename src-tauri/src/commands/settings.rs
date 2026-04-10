use tauri::State;

use crate::app_state::AppState;
use crate::config::app_settings::AppSettings;
use crate::error::CommandResult;

#[tauri::command]
pub fn get_settings(state: State<'_, AppState>) -> CommandResult<AppSettings> {
    match state.settings_service.get_settings() {
        Ok(settings) => CommandResult::ok(settings),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn save_settings(settings: AppSettings, state: State<'_, AppState>) -> CommandResult<()> {
    match state.settings_service.save_settings(&settings) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

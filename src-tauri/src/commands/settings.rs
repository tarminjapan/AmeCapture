use std::path::PathBuf;

use tauri::{AppHandle, Manager, State};

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
pub fn save_settings(
    settings: AppSettings,
    state: State<'_, AppState>,
    app: AppHandle,
) -> CommandResult<()> {
    let new_save_path = PathBuf::from(&settings.save_path);

    match state.settings_service.save_settings(&settings) {
        Ok(_) => {
            // Update the asset protocol scope to include the new save path
            let scope = app.asset_protocol_scope();
            if let Err(e) = scope.allow_directory(&new_save_path, true) {
                tracing::warn!(
                    "Failed to update asset protocol scope for new save path: {}",
                    e
                );
            } else {
                tracing::info!("Asset protocol scope updated for: {:?}", new_save_path);
            }
            CommandResult::success()
        }
        Err(e) => CommandResult::err(e.to_string()),
    }
}

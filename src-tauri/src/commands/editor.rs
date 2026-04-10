use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;

#[tauri::command]
pub fn save_edit(
    item_id: String,
    edit_data: String,
    state: State<'_, AppState>,
) -> CommandResult<String> {
    tracing::info!("Save edit requested for item: {}", item_id);
    match state.editor_service.save_edited_image(&item_id, &edit_data) {
        Ok(path) => CommandResult::ok(path),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn undo(_state: State<'_, AppState>) -> CommandResult<()> {
    // TODO: Implement undo with editor service
    CommandResult::err("Undo not yet implemented")
}

#[tauri::command]
pub fn redo(_state: State<'_, AppState>) -> CommandResult<()> {
    // TODO: Implement redo with editor service
    CommandResult::err("Redo not yet implemented")
}

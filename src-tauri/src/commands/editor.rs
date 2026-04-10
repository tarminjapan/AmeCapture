use tauri::State;

use crate::db::DbState;
use crate::utils::error::CommandResult;

#[tauri::command]
pub fn save_edit(item_id: String, state: State<'_, DbState>) -> CommandResult<String> {
    // TODO: Implement save edit logic
    log::info!("Save edit requested for item: {}", item_id);
    CommandResult::err("Editor not yet implemented")
}

#[tauri::command]
pub fn undo(state: State<'_, DbState>) -> CommandResult<()> {
    // TODO: Implement undo
    CommandResult::err("Undo not yet implemented")
}

#[tauri::command]
pub fn redo(state: State<'_, DbState>) -> CommandResult<()> {
    // TODO: Implement redo
    CommandResult::err("Redo not yet implemented")
}

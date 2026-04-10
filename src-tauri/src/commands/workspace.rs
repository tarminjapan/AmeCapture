use tauri::State;

use crate::db::DbState;
use crate::utils::error::CommandResult;
use crate::workspace::models::WorkspaceItem;
use crate::workspace::service;

#[tauri::command]
pub fn get_workspace_items(state: State<'_, DbState>) -> CommandResult<Vec<WorkspaceItem>> {
    let conn = state.0.lock().unwrap();
    match service::get_all_items(&conn) {
        Ok(items) => CommandResult::ok(items),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn delete_workspace_item(id: String, state: State<'_, DbState>) -> CommandResult<()> {
    let conn = state.0.lock().unwrap();
    match service::delete_item(&conn, &id) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn rename_workspace_item(
    id: String,
    title: String,
    state: State<'_, DbState>,
) -> CommandResult<()> {
    let conn = state.0.lock().unwrap();
    match service::rename_item(&conn, &id, &title) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn toggle_favorite(
    id: String,
    is_favorite: bool,
    state: State<'_, DbState>,
) -> CommandResult<()> {
    let conn = state.0.lock().unwrap();
    match service::toggle_favorite(&conn, &id, is_favorite) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

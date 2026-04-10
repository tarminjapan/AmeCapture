use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;
use crate::models::workspace_item::WorkspaceItem;

#[tauri::command]
pub fn get_workspace_items(state: State<'_, AppState>) -> CommandResult<Vec<WorkspaceItem>> {
    match state.workspace_service.get_all_items() {
        Ok(items) => CommandResult::ok(items),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn delete_workspace_item(id: String, state: State<'_, AppState>) -> CommandResult<()> {
    match state.workspace_service.delete_item(&id) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn rename_workspace_item(
    id: String,
    title: String,
    state: State<'_, AppState>,
) -> CommandResult<()> {
    match state.workspace_service.rename_item(&id, &title) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn toggle_favorite(
    id: String,
    is_favorite: bool,
    state: State<'_, AppState>,
) -> CommandResult<()> {
    match state.workspace_service.toggle_favorite(&id, is_favorite) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

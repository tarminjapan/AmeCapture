use std::collections::HashMap;

use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;
use crate::models::tag::Tag;
use crate::models::workspace_item::WorkspaceItem;

#[tauri::command]
pub fn get_tags(state: State<'_, AppState>) -> CommandResult<Vec<Tag>> {
    match state.tag_service.get_all_tags() {
        Ok(tags) => CommandResult::ok(tags),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn create_tag(name: String, state: State<'_, AppState>) -> CommandResult<Tag> {
    match state.tag_service.create_tag(&name) {
        Ok(tag) => CommandResult::ok(tag),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn delete_tag(id: String, state: State<'_, AppState>) -> CommandResult<()> {
    match state.tag_service.delete_tag(&id) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn get_tags_for_item(item_id: String, state: State<'_, AppState>) -> CommandResult<Vec<Tag>> {
    match state.tag_service.get_tags_for_item(&item_id) {
        Ok(tags) => CommandResult::ok(tags),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn add_tag_to_item(
    item_id: String,
    tag_id: String,
    state: State<'_, AppState>,
) -> CommandResult<()> {
    match state.tag_service.add_tag_to_item(&item_id, &tag_id) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn remove_tag_from_item(
    item_id: String,
    tag_id: String,
    state: State<'_, AppState>,
) -> CommandResult<()> {
    match state.tag_service.remove_tag_from_item(&item_id, &tag_id) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn set_tags_for_item(
    item_id: String,
    tag_ids: Vec<String>,
    state: State<'_, AppState>,
) -> CommandResult<()> {
    match state.tag_service.set_tags_for_item(&item_id, &tag_ids) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn get_items_by_tag(
    tag_id: String,
    state: State<'_, AppState>,
) -> CommandResult<Vec<WorkspaceItem>> {
    match state.tag_service.get_items_by_tag(&tag_id) {
        Ok(items) => CommandResult::ok(items),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn get_all_tags_for_items(
    item_ids: Vec<String>,
    state: State<'_, AppState>,
) -> CommandResult<HashMap<String, Vec<Tag>>> {
    match state.tag_service.get_all_tags_for_items(&item_ids) {
        Ok(map) => CommandResult::ok(map),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

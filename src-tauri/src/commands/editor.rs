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

    let item = match state.workspace_service.get_item(&item_id) {
        Ok(Some(item)) => item,
        Ok(None) => return CommandResult::err(format!("Item not found: {item_id}")),
        Err(e) => return CommandResult::err(e.to_string()),
    };

    let source_path = std::path::Path::new(&item.current_path);
    let filename = source_path
        .file_name()
        .map(|f| f.to_string_lossy().to_string())
        .unwrap_or_default();

    let output_path = state.storage_service.resolve_edited_path(&filename);

    if let Err(e) = state.editor_service.apply_annotations(
        &item.current_path,
        &output_path.to_string_lossy(),
        &edit_data,
    ) {
        return CommandResult::err(format!("Failed to apply annotations: {e}"));
    }

    let mut updated = item;
    updated.current_path = output_path.to_string_lossy().to_string();
    updated.updated_at = chrono::Utc::now().to_rfc3339();

    if let Err(e) = state.workspace_service.update_item(&updated) {
        return CommandResult::err(format!("Failed to update item: {e}"));
    }

    if let Some(thumb_path) = &updated.thumbnail_path {
        if let Err(e) = state
            .thumbnail_service
            .generate_thumbnail(&output_path.to_string_lossy(), thumb_path)
        {
            tracing::warn!("Failed to regenerate thumbnail: {}", e);
        }
    }

    CommandResult::ok(output_path.to_string_lossy().to_string())
}

#[tauri::command]
pub fn undo(_state: State<'_, AppState>) -> CommandResult<()> {
    CommandResult::err("Undo not yet implemented")
}

#[tauri::command]
pub fn redo(_state: State<'_, AppState>) -> CommandResult<()> {
    CommandResult::err("Redo not yet implemented")
}

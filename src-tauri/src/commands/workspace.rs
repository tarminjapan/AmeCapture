use std::path::Path;

use tauri::State;
use tauri_plugin_clipboard_manager::ClipboardExt;

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

#[tauri::command]
pub fn show_item_in_folder(id: String, state: State<'_, AppState>) -> CommandResult<()> {
    let item = match state.workspace_service.get_item(&id) {
        Ok(Some(item)) => item,
        Ok(None) => return CommandResult::err(format!("Item not found: {id}")),
        Err(e) => return CommandResult::err(e.to_string()),
    };

    let path = Path::new(&item.current_path);
    if !path.exists() {
        return CommandResult::err(format!("Path does not exist: {}", path.to_string_lossy()));
    }

    let result = open_in_file_manager(path);
    match result {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(e.to_string()),
    }
}

#[tauri::command]
pub fn copy_image_to_clipboard(
    app: tauri::AppHandle,
    id: String,
    state: State<'_, AppState>,
) -> CommandResult<()> {
    let item = match state.workspace_service.get_item(&id) {
        Ok(Some(item)) => item,
        Ok(None) => return CommandResult::err(format!("Item not found: {id}")),
        Err(e) => return CommandResult::err(e.to_string()),
    };

    let path = Path::new(&item.current_path);
    if !path.exists() {
        return CommandResult::err(format!("File does not exist: {}", path.to_string_lossy()));
    }

    let img = match image::open(path) {
        Ok(i) => i.to_rgba8(),
        Err(e) => return CommandResult::err(format!("Failed to load image: {e}")),
    };
    let (width, height) = img.dimensions();
    let tauri_image = tauri::image::Image::new_owned(img.into_raw(), width, height);

    let clipboard = app.clipboard();
    match clipboard.write_image(&tauri_image) {
        Ok(_) => CommandResult::success(),
        Err(e) => CommandResult::err(format!("Failed to write to clipboard: {e}")),
    }
}

fn open_in_file_manager(path: &Path) -> std::io::Result<()> {
    #[cfg(target_os = "windows")]
    {
        let path_str = path.to_string_lossy().to_string();
        std::process::Command::new("explorer")
            .arg(format!("/select,{path_str}"))
            .spawn()?;
    }

    #[cfg(target_os = "macos")]
    {
        let path_str = path.to_string_lossy().to_string();
        std::process::Command::new("open")
            .args(["-R", &path_str])
            .spawn()?;
    }

    #[cfg(target_os = "linux")]
    {
        let parent = path.parent().unwrap_or(path);
        std::process::Command::new("xdg-open").arg(parent).spawn()?;
    }

    Ok(())
}

use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;
use crate::models::capture::CaptureRegion;
use crate::models::workspace_item::{WorkspaceItem, WorkspaceItemType};

#[tauri::command]
pub fn capture(
    r#type: String,
    region: Option<CaptureRegion>,
    state: State<'_, AppState>,
) -> CommandResult<WorkspaceItem> {
    match r#type.as_str() {
        "fullscreen" => {
            tracing::info!("Full screen capture requested");

            if let Err(e) = state.storage_service.ensure_directories() {
                return CommandResult::err(format!("Failed to create storage directories: {e}"));
            }

            let filename = format!(
                "capture_{}.png",
                chrono::Local::now().format("%Y%m%d_%H%M%S")
            );

            let original_path = state.storage_service.resolve_original_path(&filename);
            let original_str = match original_path.to_str() {
                Some(s) => s.to_string(),
                None => return CommandResult::err("Invalid original path encoding"),
            };

            let capture_result = match state.capture_service.capture_full_screen(&original_str) {
                Ok(r) => r,
                Err(e) => return CommandResult::err(e.to_string()),
            };

            let edited_path = state.storage_service.resolve_edited_path(&filename);
            if let Err(e) = std::fs::copy(&original_path, &edited_path) {
                tracing::warn!("Failed to copy original to edited directory: {}", e);
            }

            let thumb_path = state.storage_service.resolve_thumbnail_path(&filename);
            let thumb_str = match thumb_path.to_str() {
                Some(s) => s.to_string(),
                None => return CommandResult::err("Invalid thumbnail path encoding"),
            };

            if let Err(e) = state
                .thumbnail_service
                .generate_thumbnail(&original_str, &thumb_str)
            {
                tracing::warn!("Failed to generate thumbnail: {}", e);
            }

            let now = chrono::Utc::now().to_rfc3339();
            let title = format!(
                "Capture {}",
                chrono::Local::now().format("%Y/%m/%d %H:%M:%S")
            );

            let item = WorkspaceItem {
                id: uuid::Uuid::new_v4().to_string(),
                item_type: WorkspaceItemType::Image,
                original_path: original_str,
                current_path: edited_path.to_string_lossy().to_string(),
                thumbnail_path: Some(thumb_str),
                title,
                created_at: now.clone(),
                updated_at: now,
                is_favorite: false,
                metadata_json: None,
            };

            match state.workspace_service.add_item(&item) {
                Ok(()) => {
                    tracing::info!(
                        "Full screen capture saved: {} ({}x{})",
                        item.id,
                        capture_result.width,
                        capture_result.height
                    );
                    CommandResult::ok(item)
                }
                Err(e) => CommandResult::err(e.to_string()),
            }
        }
        "region" => {
            if let Some(r) = region {
                tracing::info!(
                    "Region capture requested: x={}, y={}, width={}, height={}",
                    r.x,
                    r.y,
                    r.width,
                    r.height
                );
                CommandResult::err("Region capture not yet implemented")
            } else {
                CommandResult::err("Region not specified")
            }
        }
        "window" => {
            tracing::info!("Window capture requested");
            CommandResult::err("Window capture not yet implemented")
        }
        _ => CommandResult::err(format!("Unknown capture type: {}", r#type)),
    }
}

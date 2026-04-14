use std::time::Duration;

use tauri::Manager;
use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;
use crate::models::capture::{CaptureRegion, RegionCaptureInfo};
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
            let edited_str = match edited_path.to_str() {
                Some(s) => s.to_string(),
                None => return CommandResult::err("Invalid edited path encoding"),
            };

            if let Err(e) = std::fs::copy(&original_path, &edited_path) {
                return CommandResult::err(format!(
                    "Failed to copy original to edited directory: {e}"
                ));
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
                return CommandResult::err(format!("Failed to generate thumbnail: {e}"));
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
                current_path: edited_str,
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

#[tauri::command]
pub fn prepare_region_capture(
    app: tauri::AppHandle,
    state: State<'_, AppState>,
) -> CommandResult<RegionCaptureInfo> {
    tracing::info!("Preparing region capture");

    let window = match app.get_webview_window("main") {
        Some(w) => w,
        None => return CommandResult::err("Main window not found"),
    };

    if let Err(e) = window.minimize() {
        tracing::warn!("Failed to minimize window: {}", e);
    }

    std::thread::sleep(Duration::from_millis(200));

    let temp_dir = std::env::temp_dir();
    let temp_path = temp_dir.join(format!("amecapture_region_{}.png", uuid::Uuid::new_v4()));
    let temp_str = match temp_path.to_str() {
        Some(s) => s.to_string(),
        None => return CommandResult::err("Invalid temp path encoding"),
    };

    let capture_result = match state.capture_service.capture_full_screen(&temp_str) {
        Ok(r) => r,
        Err(e) => {
            let _ = window.unminimize();
            let _ = window.set_focus();
            return CommandResult::err(e.to_string());
        }
    };

    if let Err(e) = window.unminimize() {
        tracing::warn!("Failed to unminimize window: {}", e);
    }
    if let Err(e) = window.set_focus() {
        tracing::warn!("Failed to focus window: {}", e);
    }

    tracing::info!(
        "Region capture prepared: {} ({}x{})",
        temp_str,
        capture_result.width,
        capture_result.height
    );

    CommandResult::ok(RegionCaptureInfo {
        temp_path: temp_str,
        screen_width: capture_result.width,
        screen_height: capture_result.height,
    })
}

#[tauri::command]
pub fn finalize_region_capture(
    source_path: String,
    region: CaptureRegion,
    state: State<'_, AppState>,
) -> CommandResult<WorkspaceItem> {
    tracing::info!(
        "Finalizing region capture: x={}, y={}, width={}, height={}",
        region.x,
        region.y,
        region.width,
        region.height
    );

    if let Err(e) = state.storage_service.ensure_directories() {
        return CommandResult::err(format!("Failed to create storage directories: {e}"));
    }

    let source = std::path::Path::new(&source_path);
    if !source.exists() {
        return CommandResult::err("Source screenshot file not found");
    }

    let img = match image::open(source) {
        Ok(i) => i,
        Err(e) => return CommandResult::err(format!("Failed to open source image: {e}")),
    };

    let x = (region.x.max(0) as u32).min(img.width().saturating_sub(1));
    let y = (region.y.max(0) as u32).min(img.height().saturating_sub(1));
    let max_w = img.width().saturating_sub(x);
    let max_h = img.height().saturating_sub(y);
    let w = region.width.min(max_w);
    let h = region.height.min(max_h);

    if w == 0 || h == 0 {
        let _ = std::fs::remove_file(&source_path);
        return CommandResult::err("Selected region is too small");
    }

    let cropped = img.crop_imm(x, y, w, h);

    let filename = format!(
        "capture_{}.png",
        chrono::Local::now().format("%Y%m%d_%H%M%S")
    );

    let original_path = state.storage_service.resolve_original_path(&filename);
    let original_str = match original_path.to_str() {
        Some(s) => s.to_string(),
        None => return CommandResult::err("Invalid original path encoding"),
    };

    if let Err(e) = cropped.save_with_format(&original_path, image::ImageFormat::Png) {
        let _ = std::fs::remove_file(&source_path);
        return CommandResult::err(format!("Failed to save cropped image: {e}"));
    }

    let edited_path = state.storage_service.resolve_edited_path(&filename);
    let edited_str = match edited_path.to_str() {
        Some(s) => s.to_string(),
        None => return CommandResult::err("Invalid edited path encoding"),
    };

    if let Err(e) = std::fs::copy(&original_path, &edited_path) {
        return CommandResult::err(format!("Failed to copy original to edited directory: {e}"));
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
        return CommandResult::err(format!("Failed to generate thumbnail: {e}"));
    }

    let now = chrono::Utc::now().to_rfc3339();
    let title = format!(
        "Region Capture {}",
        chrono::Local::now().format("%Y/%m/%d %H:%M:%S")
    );

    let item = WorkspaceItem {
        id: uuid::Uuid::new_v4().to_string(),
        item_type: WorkspaceItemType::Image,
        original_path: original_str,
        current_path: edited_str,
        thumbnail_path: Some(thumb_str),
        title,
        created_at: now.clone(),
        updated_at: now,
        is_favorite: false,
        metadata_json: None,
    };

    match state.workspace_service.add_item(&item) {
        Ok(()) => {
            tracing::info!("Region capture saved: {} ({}x{})", item.id, w, h);
            let _ = std::fs::remove_file(&source_path);
            CommandResult::ok(item)
        }
        Err(e) => {
            let _ = std::fs::remove_file(&source_path);
            CommandResult::err(e.to_string())
        }
    }
}

#[tauri::command]
pub fn cancel_region_capture(source_path: String) -> CommandResult<()> {
    tracing::info!("Cancelling region capture");
    let _ = std::fs::remove_file(&source_path);
    CommandResult::success()
}

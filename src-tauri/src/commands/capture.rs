use tauri::State;

use crate::app_state::AppState;
use crate::error::CommandResult;
use crate::models::capture::CaptureRegion;

#[tauri::command]
pub fn capture(
    r#type: String,
    region: Option<CaptureRegion>,
    state: State<'_, AppState>,
) -> CommandResult<String> {
    match r#type.as_str() {
        "fullscreen" => {
            tracing::info!("Full screen capture requested");
            match state.capture_service.capture_full_screen() {
                Ok(result) => CommandResult::ok(result.file_path),
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
                match state.capture_service.capture_region(&r) {
                    Ok(result) => CommandResult::ok(result.file_path),
                    Err(e) => CommandResult::err(e.to_string()),
                }
            } else {
                CommandResult::err("Region not specified")
            }
        }
        "window" => {
            tracing::info!("Window capture requested");
            // Window capture requires hwnd, will be handled in future implementation
            CommandResult::err("Window capture not yet implemented")
        }
        _ => CommandResult::err(format!("Unknown capture type: {}", r#type)),
    }
}

use tauri::State;

use crate::db::DbState;
use crate::utils::error::CommandResult;

#[derive(Debug, serde::Deserialize)]
pub struct CaptureRegion {
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
}

#[tauri::command]
pub fn capture(
    r#type: String,
    region: Option<CaptureRegion>,
    state: State<'_, DbState>,
) -> CommandResult<String> {
    let _conn = state.0.lock().unwrap();

    match r#type.as_str() {
        "fullscreen" => {
            // TODO: Implement full screen capture
            log::info!("Full screen capture requested");
            CommandResult::err("Full screen capture not yet implemented")
        }
        "region" => {
            if let Some(_r) = region {
                // TODO: Implement region capture
                log::info!("Region capture requested");
                CommandResult::err("Region capture not yet implemented")
            } else {
                CommandResult::err("Region not specified")
            }
        }
        "window" => {
            // TODO: Implement window capture
            log::info!("Window capture requested");
            CommandResult::err("Window capture not yet implemented")
        }
        _ => CommandResult::err(format!("Unknown capture type: {}", r#type)),
    }
}
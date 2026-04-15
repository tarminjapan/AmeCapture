use std::sync::{Arc, Mutex};

use crate::services::capture::CaptureService;
use crate::services::editor::EditorService;
use crate::services::settings::SettingsService;
use crate::services::storage::StorageService;
use crate::services::tag::TagService;
use crate::services::thumbnail::ThumbnailService;
use crate::services::workspace::WorkspaceService;

/// Application state managed by Tauri.
///
/// This serves as the DI container, holding trait-object references
/// to all services. Services are resolved through this state in commands.
pub struct AppState {
    pub capture_service: Box<dyn CaptureService>,
    pub workspace_service: Box<dyn WorkspaceService>,
    pub tag_service: Box<dyn TagService>,
    pub settings_service: Box<dyn SettingsService>,
    pub editor_service: Box<dyn EditorService>,
    pub thumbnail_service: Box<dyn ThumbnailService>,
    pub storage_service: Box<dyn StorageService>,
    #[allow(dead_code)]
    pub db_conn: Arc<Mutex<rusqlite::Connection>>,
}

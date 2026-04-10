mod app_state;
mod commands;
mod config;
mod db;
mod error;
mod logging;
mod models;
mod platform;
mod repositories;
mod services;
mod storage;

use std::sync::{Arc, Mutex};

use tauri::Manager;

use crate::app_state::AppState;
use crate::db::connection::create_connection;
use crate::db::migrations::run_migrations;
use crate::repositories::settings::SqliteSettingsRepository;
use crate::repositories::workspace::SqliteWorkspaceRepository;
use crate::services::capture::DefaultCaptureService;
use crate::services::editor::DefaultEditorService;
use crate::services::settings::DefaultSettingsService;
use crate::services::storage::DefaultStorageService;
use crate::services::thumbnail::DefaultThumbnailService;
use crate::services::workspace::DefaultWorkspaceService;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_clipboard_manager::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_store::Builder::new().build())
        .setup(|app| {
            // === Initialize Logging ===
            let log_dir = app
                .path()
                .app_data_dir()
                .expect("Failed to get app data dir")
                .join("logs");

            // The WorkerGuard must be kept alive for the lifetime of the app.
            // We leak it intentionally so it's never dropped and logs are flushed.
            let guard = logging::init_logging(log_dir);
            if let Some(guard) = guard {
                Box::leak(Box::new(guard));
            }

            // === Initialize Database ===
            let app_data_dir = app
                .path()
                .app_data_dir()
                .expect("Failed to get app data dir");
            std::fs::create_dir_all(&app_data_dir).ok();

            let db_path = app_data_dir.join("amecapture.db");
            let conn = create_connection(&db_path).expect("Failed to initialize database");
            run_migrations(&conn).expect("Failed to run database migrations");

            let conn = Arc::new(Mutex::new(conn));

            // === Load appsettings.json ===
            let settings_path = app_data_dir.join("appsettings.json");
            let file_settings = config::load_settings_from_file(&settings_path).unwrap_or_default();
            tracing::info!("Application settings loaded");

            // === Initialize Storage Directories ===
            let save_path = std::path::PathBuf::from(&file_settings.save_path);
            if let Err(e) = storage::ensure_storage_dirs(&save_path) {
                tracing::warn!("Failed to create storage directories: {}", e);
            }

            // === Build DI Container ===
            let workspace_repo = SqliteWorkspaceRepository::new(Arc::clone(&conn));
            let settings_repo = SqliteSettingsRepository::new(Arc::clone(&conn));

            let storage_service = DefaultStorageService::new(save_path);

            let app_state = AppState {
                capture_service: Box::new(DefaultCaptureService::new()),
                workspace_service: Box::new(DefaultWorkspaceService::new(workspace_repo)),
                settings_service: Box::new(DefaultSettingsService::new(settings_repo)),
                editor_service: Box::new(DefaultEditorService::new()),
                thumbnail_service: Box::new(DefaultThumbnailService::new()),
                storage_service: Box::new(storage_service),
                db_conn: conn,
            };

            app.manage(app_state);

            tracing::info!("AmeCapture initialized successfully");
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            commands::capture::capture,
            commands::workspace::get_workspace_items,
            commands::workspace::delete_workspace_item,
            commands::workspace::rename_workspace_item,
            commands::workspace::toggle_favorite,
            commands::editor::save_edit,
            commands::editor::undo,
            commands::editor::redo,
            commands::settings::get_settings,
            commands::settings::save_settings,
            commands::storage::get_storage_paths,
            commands::storage::resolve_storage_path,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#![deny(clippy::all)]
#![deny(warnings)]

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

use tauri::{
    menu::{Menu, MenuItem, PredefinedMenuItem, Submenu},
    tray::TrayIconBuilder,
    Emitter, Manager,
};

use crate::app_state::AppState;
use crate::db::connection::create_connection;
use crate::db::migrations::run_migrations;
use crate::repositories::settings::SqliteSettingsRepository;
use crate::repositories::tag::SqliteTagRepository;
use crate::repositories::workspace::SqliteWorkspaceRepository;
use crate::services::capture::DefaultCaptureService;
use crate::services::editor::DefaultEditorService;
use crate::services::settings::DefaultSettingsService;
use crate::services::storage::DefaultStorageService;
use crate::services::tag::DefaultTagService;
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

            // === Configure Asset Protocol Scope ===
            let scope = app.asset_protocol_scope();
            if let Err(e) = scope.allow_directory(&save_path, true) {
                tracing::warn!("Failed to add save path to asset protocol scope: {}", e);
            } else {
                tracing::info!("Asset protocol scope configured for: {:?}", save_path);
            }

            // === Build DI Container ===
            let workspace_repo = SqliteWorkspaceRepository::new(Arc::clone(&conn));
            let settings_repo = SqliteSettingsRepository::new(Arc::clone(&conn));
            let tag_repo = SqliteTagRepository::new(Arc::clone(&conn));

            let storage_service = DefaultStorageService::new(save_path);

            let app_state = AppState {
                capture_service: Box::new(DefaultCaptureService::new()),
                workspace_service: Box::new(DefaultWorkspaceService::new(workspace_repo)),
                tag_service: Box::new(DefaultTagService::new(
                    tag_repo,
                    SqliteWorkspaceRepository::new(Arc::clone(&conn)),
                )),
                settings_service: Box::new(DefaultSettingsService::new(settings_repo)),
                editor_service: Box::new(DefaultEditorService::new()),
                thumbnail_service: Box::new(DefaultThumbnailService::new()),
                storage_service: Box::new(storage_service),
                _db_conn: conn,
            };

            app.manage(app_state);

            // === System Tray ===
            let show = MenuItem::with_id(app, "show", "ウィンドウを表示", true, None::<&str>)?;
            let capture_submenu = Submenu::with_items(
                app,
                "キャプチャ",
                true,
                &[
                    &MenuItem::with_id(
                        app,
                        "capture_region",
                        "範囲キャプチャ",
                        true,
                        None::<&str>,
                    )?,
                    &MenuItem::with_id(
                        app,
                        "capture_fullscreen",
                        "全画面キャプチャ",
                        true,
                        None::<&str>,
                    )?,
                    &MenuItem::with_id(
                        app,
                        "capture_window",
                        "ウィンドウキャプチャ",
                        true,
                        None::<&str>,
                    )?,
                ],
            )?;
            let separator = PredefinedMenuItem::separator(app)?;
            let quit = MenuItem::with_id(app, "quit", "終了", true, None::<&str>)?;

            let menu = Menu::with_items(app, &[&show, &capture_submenu, &separator, &quit])?;

            let tray = TrayIconBuilder::new()
                .icon(
                    app.default_window_icon()
                        .cloned()
                        .ok_or("Failed to get default window icon")?,
                )
                .menu(&menu)
                .show_menu_on_left_click(false)
                .tooltip("AmeCapture")
                .on_menu_event(move |app, event| match event.id.as_ref() {
                    "show" => {
                        if let Some(window) = app.get_webview_window("main") {
                            let _ = window.show();
                            let _ = window.unminimize();
                            let _ = window.set_focus();
                        }
                    }
                    "capture_region" => {
                        let _ = app.emit("tray-capture", "region");
                    }
                    "capture_fullscreen" => {
                        let _ = app.emit("tray-capture", "fullscreen");
                    }
                    "capture_window" => {
                        let _ = app.emit("tray-capture", "window");
                    }
                    "quit" => {
                        app.exit(0);
                    }
                    _ => {}
                })
                .build(app)?;
            Box::leak(Box::new(tray));

            tracing::info!("AmeCapture initialized successfully");
            Ok(())
        })
        .on_window_event(|window, event| {
            if let tauri::WindowEvent::CloseRequested { api, .. } = event {
                let _ = window.hide();
                api.prevent_close();
            }
        })
        .invoke_handler(tauri::generate_handler![
            commands::capture::capture,
            commands::capture::prepare_region_capture,
            commands::capture::finalize_region_capture,
            commands::capture::cancel_region_capture,
            commands::capture::prepare_window_capture,
            commands::workspace::get_workspace_items,
            commands::workspace::delete_workspace_item,
            commands::workspace::rename_workspace_item,
            commands::workspace::toggle_favorite,
            commands::workspace::show_item_in_folder,
            commands::workspace::copy_image_to_clipboard,
            commands::editor::save_edit,
            commands::editor::undo,
            commands::editor::redo,
            commands::settings::get_settings,
            commands::settings::save_settings,
            commands::storage::get_storage_paths,
            commands::storage::resolve_storage_path,
            commands::tag::get_tags,
            commands::tag::create_tag,
            commands::tag::delete_tag,
            commands::tag::get_tags_for_item,
            commands::tag::add_tag_to_item,
            commands::tag::remove_tag_from_item,
            commands::tag::set_tags_for_item,
            commands::tag::get_items_by_tag,
            commands::tag::get_all_tags_for_items,
            commands::log::frontend_log,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

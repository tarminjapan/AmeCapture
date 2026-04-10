mod commands;
mod capture;
mod workspace;
mod editor;
mod db;
mod config;
mod utils;

use tauri::Manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_clipboard_manager::init())
        .plugin(tauri_plugin_global_shortcut::Builder::new().build())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_store::Builder::new().build())
        .setup(|app| {
            // Initialize database
            let app_data_dir = app
                .path()
                .app_data_dir()
                .expect("Failed to get app data dir");
            std::fs::create_dir_all(&app_data_dir).ok();

            let db_path = app_data_dir.join("amecapture.db");
            let conn = db::connection::create_connection(&db_path)
                .expect("Failed to initialize database");
            db::migrations::run_migrations(&conn)
                .expect("Failed to run database migrations");

            // Store the connection as managed state
            app.manage(db::DbState(std::sync::Mutex::new(conn)));

            log::info!("AmeCapture initialized successfully");
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
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
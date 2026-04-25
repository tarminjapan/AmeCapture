// ⚠️ DEPRECATED: This Tauri/Rust implementation is frozen. Use src-dotnet/ (.NET MAUI) instead.
// See src-tauri/FROZEN.md for details.
#![deny(clippy::all)]
#![deny(warnings)]
// Prevents additional console window on Windows in release
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

fn main() {
    ame_capture_lib::run()
}

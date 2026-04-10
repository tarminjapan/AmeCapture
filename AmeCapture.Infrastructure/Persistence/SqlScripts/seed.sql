INSERT OR IGNORE INTO app_settings (key, value) VALUES
('storage.root', ''),
('capture.default_format', 'png'),
('capture.auto_copy_to_clipboard', 'true'),
('capture.include_cursor', 'false'),
('app.start_minimized', 'false'),
('app.minimize_to_tray', 'true'),
('workspace.sort_by', 'created_at_desc');

INSERT OR IGNORE INTO capture_presets (
    id, name, capture_mode, image_format, delay_seconds, include_cursor, auto_copy_to_clipboard, save_to_workspace
) VALUES
('preset-region-default', 'Region Default', 'region', 'png', 0, 0, 1, 1),
('preset-fullscreen-default', 'Fullscreen Default', 'fullscreen', 'png', 0, 0, 1, 1),
('preset-window-default', 'Window Default', 'window', 'png', 0, 0, 1, 1);

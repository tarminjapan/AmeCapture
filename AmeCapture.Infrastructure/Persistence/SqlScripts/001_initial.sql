PRAGMA foreign_keys = ON;

-- ----------------------------
-- workspace_items
-- ----------------------------
CREATE TABLE IF NOT EXISTS workspace_items (
    id TEXT PRIMARY KEY,
    item_type TEXT NOT NULL CHECK (item_type IN ('image', 'video')),
    title TEXT,
    original_path TEXT NOT NULL,
    current_path TEXT NOT NULL,
    thumbnail_path TEXT,
    file_extension TEXT,
    mime_type TEXT,
    width INTEGER,
    height INTEGER,
    duration_ms INTEGER,
    file_size_bytes INTEGER,
    is_favorite INTEGER NOT NULL DEFAULT 0 CHECK (is_favorite IN (0, 1)),
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    deleted_at TEXT,
    metadata_json TEXT
);

CREATE INDEX IF NOT EXISTS idx_workspace_items_created_at
    ON workspace_items(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_workspace_items_item_type
    ON workspace_items(item_type);

CREATE INDEX IF NOT EXISTS idx_workspace_items_is_favorite
    ON workspace_items(is_favorite);

CREATE INDEX IF NOT EXISTS idx_workspace_items_title
    ON workspace_items(title);

-- ----------------------------
-- tags
-- ----------------------------
CREATE TABLE IF NOT EXISTS tags (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_tags_name
    ON tags(name);

-- ----------------------------
-- workspace_item_tags
-- ----------------------------
CREATE TABLE IF NOT EXISTS workspace_item_tags (
    workspace_item_id TEXT NOT NULL,
    tag_id TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (workspace_item_id, tag_id),
    FOREIGN KEY (workspace_item_id) REFERENCES workspace_items(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_workspace_item_tags_tag_id
    ON workspace_item_tags(tag_id);

-- ----------------------------
-- app_settings
-- ----------------------------
CREATE TABLE IF NOT EXISTS app_settings (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- ----------------------------
-- capture_presets
-- ----------------------------
CREATE TABLE IF NOT EXISTS capture_presets (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    capture_mode TEXT NOT NULL CHECK (
        capture_mode IN ('region', 'fullscreen', 'window', 'last_region', 'delayed')
    ),
    image_format TEXT NOT NULL CHECK (image_format IN ('png', 'jpg', 'webp')),
    delay_seconds INTEGER NOT NULL DEFAULT 0,
    include_cursor INTEGER NOT NULL DEFAULT 0 CHECK (include_cursor IN (0, 1)),
    auto_copy_to_clipboard INTEGER NOT NULL DEFAULT 1 CHECK (auto_copy_to_clipboard IN (0, 1)),
    save_to_workspace INTEGER NOT NULL DEFAULT 1 CHECK (save_to_workspace IN (0, 1)),
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- ----------------------------
-- recent_actions
-- ----------------------------
CREATE TABLE IF NOT EXISTS recent_actions (
    id TEXT PRIMARY KEY,
    action_type TEXT NOT NULL,
    target_item_id TEXT,
    payload_json TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (target_item_id) REFERENCES workspace_items(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_recent_actions_created_at
    ON recent_actions(created_at DESC);

CREATE INDEX IF NOT EXISTS idx_recent_actions_target_item_id
    ON recent_actions(target_item_id);

-- ----------------------------
-- editor_documents
-- ----------------------------
CREATE TABLE IF NOT EXISTS editor_documents (
    id TEXT PRIMARY KEY,
    workspace_item_id TEXT NOT NULL,
    document_json TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (workspace_item_id) REFERENCES workspace_items(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_editor_documents_workspace_item_id
    ON editor_documents(workspace_item_id);

-- ----------------------------
-- trigger: updated_at auto-update
-- ----------------------------
CREATE TRIGGER IF NOT EXISTS trg_workspace_items_updated_at
AFTER UPDATE ON workspace_items
FOR EACH ROW
BEGIN
    UPDATE workspace_items
    SET updated_at = datetime('now')
    WHERE id = OLD.id;
END;

CREATE TRIGGER IF NOT EXISTS trg_app_settings_updated_at
AFTER UPDATE ON app_settings
FOR EACH ROW
BEGIN
    UPDATE app_settings
    SET updated_at = datetime('now')
    WHERE key = OLD.key;
END;

CREATE TRIGGER IF NOT EXISTS trg_capture_presets_updated_at
AFTER UPDATE ON capture_presets
FOR EACH ROW
BEGIN
    UPDATE capture_presets
    SET updated_at = datetime('now')
    WHERE id = OLD.id;
END;

CREATE TRIGGER IF NOT EXISTS trg_editor_documents_updated_at
AFTER UPDATE ON editor_documents
FOR EACH ROW
BEGIN
    UPDATE editor_documents
    SET updated_at = datetime('now')
    WHERE id = OLD.id;
END;

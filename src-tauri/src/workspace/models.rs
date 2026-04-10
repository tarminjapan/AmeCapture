use serde::{Deserialize, Serialize};

/// Workspace item entity
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WorkspaceItem {
    pub id: String,
    #[serde(rename = "type")]
    pub item_type: String,
    pub original_path: String,
    pub current_path: String,
    pub thumbnail_path: Option<String>,
    pub title: String,
    pub created_at: String,
    pub updated_at: String,
    pub is_favorite: bool,
    pub metadata_json: Option<String>,
}

/// Tag entity
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Tag {
    pub id: String,
    pub name: String,
}

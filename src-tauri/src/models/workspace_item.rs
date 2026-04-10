use serde::{Deserialize, Serialize};

/// Workspace item entity
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct WorkspaceItem {
    pub id: String,
    #[serde(rename = "type")]
    pub item_type: WorkspaceItemType,
    pub original_path: String,
    pub current_path: String,
    pub thumbnail_path: Option<String>,
    pub title: String,
    pub created_at: String,
    pub updated_at: String,
    pub is_favorite: bool,
    pub metadata_json: Option<String>,
}

/// Type of workspace item
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
#[serde(rename_all = "lowercase")]
pub enum WorkspaceItemType {
    Image,
    Video,
}

impl std::fmt::Display for WorkspaceItemType {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        match self {
            WorkspaceItemType::Image => write!(f, "image"),
            WorkspaceItemType::Video => write!(f, "video"),
        }
    }
}

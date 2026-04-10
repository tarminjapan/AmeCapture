use crate::error::AppResult;
use crate::models::workspace_item::WorkspaceItem;
use crate::repositories::workspace::WorkspaceRepository;

/// Service trait for workspace operations
pub trait WorkspaceService: Send + Sync {
    fn get_all_items(&self) -> AppResult<Vec<WorkspaceItem>>;
    fn get_item(&self, id: &str) -> AppResult<Option<WorkspaceItem>>;
    fn add_item(&self, item: &WorkspaceItem) -> AppResult<()>;
    fn update_item(&self, item: &WorkspaceItem) -> AppResult<()>;
    fn delete_item(&self, id: &str) -> AppResult<()>;
    fn rename_item(&self, id: &str, title: &str) -> AppResult<()>;
    fn toggle_favorite(&self, id: &str, is_favorite: bool) -> AppResult<()>;
}

/// Default workspace service implementation
pub struct DefaultWorkspaceService<R: WorkspaceRepository> {
    repo: R,
}

impl<R: WorkspaceRepository> DefaultWorkspaceService<R> {
    pub fn new(repo: R) -> Self {
        Self { repo }
    }
}

impl<R: WorkspaceRepository> WorkspaceService for DefaultWorkspaceService<R> {
    fn get_all_items(&self) -> AppResult<Vec<WorkspaceItem>> {
        tracing::debug!("Fetching all workspace items");
        let items = self.repo.get_all()?;
        tracing::debug!("Found {} workspace items", items.len());
        Ok(items)
    }

    fn get_item(&self, id: &str) -> AppResult<Option<WorkspaceItem>> {
        tracing::debug!("Fetching workspace item: {}", id);
        self.repo.get_by_id(id)
    }

    fn add_item(&self, item: &WorkspaceItem) -> AppResult<()> {
        tracing::info!("Adding workspace item: {} ({})", item.id, item.title);
        self.repo.add(item)
    }

    fn update_item(&self, item: &WorkspaceItem) -> AppResult<()> {
        tracing::info!("Updating workspace item: {}", item.id);
        self.repo.update(item)
    }

    fn delete_item(&self, id: &str) -> AppResult<()> {
        tracing::info!("Deleting workspace item: {}", id);
        self.repo.delete(id)
    }

    fn rename_item(&self, id: &str, title: &str) -> AppResult<()> {
        tracing::info!("Renaming workspace item {} to '{}'", id, title);
        let mut item = self
            .repo
            .get_by_id(id)?
            .ok_or_else(|| crate::error::AppError::NotFound(format!("Item not found: {}", id)))?;
        item.title = title.to_string();
        item.updated_at = chrono::Utc::now().to_rfc3339();
        self.repo.update(&item)
    }

    fn toggle_favorite(&self, id: &str, is_favorite: bool) -> AppResult<()> {
        tracing::info!("Toggling favorite for item {} to {}", id, is_favorite);
        let mut item = self
            .repo
            .get_by_id(id)?
            .ok_or_else(|| crate::error::AppError::NotFound(format!("Item not found: {}", id)))?;
        item.is_favorite = is_favorite;
        item.updated_at = chrono::Utc::now().to_rfc3339();
        self.repo.update(&item)
    }
}

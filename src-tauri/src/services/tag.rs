use crate::error::{AppError, AppResult};
use crate::models::tag::Tag;
use crate::models::workspace_item::WorkspaceItem;
use crate::repositories::tag::TagRepository;
use crate::repositories::workspace::WorkspaceRepository;

pub trait TagService: Send + Sync {
    fn get_all_tags(&self) -> AppResult<Vec<Tag>>;
    fn create_tag(&self, name: &str) -> AppResult<Tag>;
    fn delete_tag(&self, id: &str) -> AppResult<()>;
    fn get_tags_for_item(&self, item_id: &str) -> AppResult<Vec<Tag>>;
    fn add_tag_to_item(&self, item_id: &str, tag_id: &str) -> AppResult<()>;
    fn remove_tag_from_item(&self, item_id: &str, tag_id: &str) -> AppResult<()>;
    fn set_tags_for_item(&self, item_id: &str, tag_ids: &[String]) -> AppResult<()>;
    fn get_items_by_tag(&self, tag_id: &str) -> AppResult<Vec<WorkspaceItem>>;
}

pub struct DefaultTagService<T: TagRepository, W: WorkspaceRepository> {
    tag_repo: T,
    workspace_repo: W,
}

impl<T: TagRepository, W: WorkspaceRepository> DefaultTagService<T, W> {
    pub fn new(tag_repo: T, workspace_repo: W) -> Self {
        Self {
            tag_repo,
            workspace_repo,
        }
    }
}

impl<T: TagRepository, W: WorkspaceRepository> TagService for DefaultTagService<T, W> {
    fn get_all_tags(&self) -> AppResult<Vec<Tag>> {
        tracing::debug!("Fetching all tags");
        self.tag_repo.get_all()
    }

    fn create_tag(&self, name: &str) -> AppResult<Tag> {
        let trimmed = name.trim();
        if trimmed.is_empty() {
            return Err(AppError::Config("Tag name cannot be empty".to_string()));
        }
        tracing::info!("Creating tag: {}", trimmed);

        if let Some(existing) = self.tag_repo.find_by_name(trimmed)? {
            return Ok(existing);
        }

        let tag = Tag {
            id: uuid::Uuid::new_v4().to_string(),
            name: trimmed.to_string(),
        };
        self.tag_repo.add(&tag)?;
        Ok(tag)
    }

    fn delete_tag(&self, id: &str) -> AppResult<()> {
        tracing::info!("Deleting tag: {}", id);
        self.tag_repo.delete(id)
    }

    fn get_tags_for_item(&self, item_id: &str) -> AppResult<Vec<Tag>> {
        tracing::debug!("Fetching tags for item: {}", item_id);
        self.tag_repo.get_tags_for_item(item_id)
    }

    fn add_tag_to_item(&self, item_id: &str, tag_id: &str) -> AppResult<()> {
        tracing::info!("Adding tag {} to item {}", tag_id, item_id);
        self.workspace_repo
            .get_by_id(item_id)?
            .ok_or_else(|| AppError::NotFound(format!("Item not found: {}", item_id)))?;
        self.tag_repo
            .get_by_id(tag_id)?
            .ok_or_else(|| AppError::NotFound(format!("Tag not found: {}", tag_id)))?;
        self.tag_repo.add_tag_to_item(item_id, tag_id)
    }

    fn remove_tag_from_item(&self, item_id: &str, tag_id: &str) -> AppResult<()> {
        tracing::info!("Removing tag {} from item {}", tag_id, item_id);
        self.tag_repo.remove_tag_from_item(item_id, tag_id)
    }

    fn set_tags_for_item(&self, item_id: &str, tag_ids: &[String]) -> AppResult<()> {
        tracing::info!("Setting tags for item {}: {:?}", item_id, tag_ids);
        self.workspace_repo
            .get_by_id(item_id)?
            .ok_or_else(|| AppError::NotFound(format!("Item not found: {}", item_id)))?;
        for tag_id in tag_ids {
            self.tag_repo
                .get_by_id(tag_id)?
                .ok_or_else(|| AppError::NotFound(format!("Tag not found: {}", tag_id)))?;
        }
        self.tag_repo.set_tags_for_item(item_id, tag_ids)
    }

    fn get_items_by_tag(&self, tag_id: &str) -> AppResult<Vec<WorkspaceItem>> {
        tracing::debug!("Fetching items for tag: {}", tag_id);
        let item_ids = self.tag_repo.get_item_ids_by_tag(tag_id)?;
        let mut items = Vec::with_capacity(item_ids.len());
        for id in &item_ids {
            if let Some(item) = self.workspace_repo.get_by_id(id)? {
                items.push(item);
            }
        }
        Ok(items)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::db::migrations::run_migrations;
    use crate::models::workspace_item::WorkspaceItemType;
    use crate::repositories::tag::SqliteTagRepository;
    use crate::repositories::workspace::SqliteWorkspaceRepository;
    use rusqlite::Connection;
    use std::sync::{Arc, Mutex};

    fn setup_service() -> DefaultTagService<SqliteTagRepository, SqliteWorkspaceRepository> {
        let conn = Connection::open_in_memory().unwrap();
        conn.execute_batch("PRAGMA foreign_keys=ON;").unwrap();
        run_migrations(&conn).unwrap();
        let conn = Arc::new(Mutex::new(conn));
        let tag_repo = SqliteTagRepository::new(Arc::clone(&conn));
        let workspace_repo = SqliteWorkspaceRepository::new(conn);
        DefaultTagService::new(tag_repo, workspace_repo)
    }

    fn sample_item(id: &str) -> WorkspaceItem {
        WorkspaceItem {
            id: id.to_string(),
            item_type: WorkspaceItemType::Image,
            original_path: "/original/test.png".to_string(),
            current_path: "/current/test.png".to_string(),
            thumbnail_path: None,
            title: "Test Item".to_string(),
            created_at: "2026-01-01T00:00:00Z".to_string(),
            updated_at: "2026-01-01T00:00:00Z".to_string(),
            is_favorite: false,
            metadata_json: None,
        }
    }

    #[test]
    fn test_create_and_get_tag() {
        let service = setup_service();
        let tag = service.create_tag("test-tag").unwrap();
        assert_eq!(tag.name, "test-tag");
        assert!(!tag.id.is_empty());

        let tags = service.get_all_tags().unwrap();
        assert_eq!(tags.len(), 1);
    }

    #[test]
    fn test_create_tag_deduplication() {
        let service = setup_service();
        let tag1 = service.create_tag("tag1").unwrap();
        let tag2 = service.create_tag("tag1").unwrap();
        assert_eq!(tag1.id, tag2.id);
    }

    #[test]
    fn test_create_tag_empty_name_fails() {
        let service = setup_service();
        assert!(service.create_tag("").is_err());
        assert!(service.create_tag("  ").is_err());
    }

    #[test]
    fn test_delete_tag() {
        let service = setup_service();
        let tag = service.create_tag("tag1").unwrap();
        service.delete_tag(&tag.id).unwrap();
        assert!(service.get_all_tags().unwrap().is_empty());
    }

    #[test]
    fn test_add_and_remove_tag_from_item() {
        let service = setup_service();

        let item = sample_item("w1");
        service.workspace_repo.add(&item).unwrap();

        let tag = service.create_tag("tag1").unwrap();
        service.add_tag_to_item("w1", &tag.id).unwrap();

        let tags = service.get_tags_for_item("w1").unwrap();
        assert_eq!(tags.len(), 1);
        assert_eq!(tags[0].name, "tag1");

        service.remove_tag_from_item("w1", &tag.id).unwrap();
        assert!(service.get_tags_for_item("w1").unwrap().is_empty());
    }

    #[test]
    fn test_set_tags_for_item() {
        let service = setup_service();

        let item = sample_item("w1");
        service.workspace_repo.add(&item).unwrap();

        let t1 = service.create_tag("tag1").unwrap();
        let t2 = service.create_tag("tag2").unwrap();
        let t3 = service.create_tag("tag3").unwrap();

        service
            .set_tags_for_item("w1", &[t1.id.clone(), t2.id.clone()])
            .unwrap();
        let tags = service.get_tags_for_item("w1").unwrap();
        assert_eq!(tags.len(), 2);

        service.set_tags_for_item("w1", &[t3.id.clone()]).unwrap();
        let tags = service.get_tags_for_item("w1").unwrap();
        assert_eq!(tags.len(), 1);
        assert_eq!(tags[0].name, "tag3");
    }

    #[test]
    fn test_get_items_by_tag() {
        let service = setup_service();

        service.workspace_repo.add(&sample_item("w1")).unwrap();
        service.workspace_repo.add(&sample_item("w2")).unwrap();

        let tag = service.create_tag("tag1").unwrap();
        service.add_tag_to_item("w1", &tag.id).unwrap();
        service.add_tag_to_item("w2", &tag.id).unwrap();

        let items = service.get_items_by_tag(&tag.id).unwrap();
        assert_eq!(items.len(), 2);
    }

    #[test]
    fn test_add_tag_to_nonexistent_item_fails() {
        let service = setup_service();
        let tag = service.create_tag("tag1").unwrap();
        assert!(service.add_tag_to_item("nonexistent", &tag.id).is_err());
    }

    #[test]
    fn test_add_nonexistent_tag_to_item_fails() {
        let service = setup_service();
        service.workspace_repo.add(&sample_item("w1")).unwrap();
        assert!(service.add_tag_to_item("w1", "nonexistent").is_err());
    }
}

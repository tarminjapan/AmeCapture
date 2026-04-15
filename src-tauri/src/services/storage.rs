use std::path::PathBuf;

use crate::error::AppResult;
use crate::storage::StoragePathResolver;

/// Service trait for storage path resolution and directory management.
pub trait StorageService: Send + Sync {
    /// Get the base save path.
    #[allow(dead_code)]
    fn get_base_path(&self) -> PathBuf;

    /// Ensure all storage directories exist. Creates them if missing.
    fn ensure_directories(&self) -> AppResult<()>;

    /// Get the path resolver for this storage configuration.
    fn resolver(&self) -> &StoragePathResolver;

    /// Resolve the full path for an original file.
    fn resolve_original_path(&self, filename: &str) -> PathBuf;

    /// Resolve the full path for an edited file.
    fn resolve_edited_path(&self, filename: &str) -> PathBuf;

    /// Resolve the unique thumbnail path for a given original filename.
    fn resolve_thumbnail_path(&self, original_filename: &str) -> PathBuf;

    /// Resolve the full path for a video file.
    fn resolve_video_path(&self, filename: &str) -> PathBuf;
}

/// Default storage service backed by a `StoragePathResolver`.
pub struct DefaultStorageService {
    resolver: StoragePathResolver,
}

impl DefaultStorageService {
    /// Create a new storage service with the given base save path.
    pub fn new(base_path: PathBuf) -> Self {
        Self {
            resolver: StoragePathResolver::new(base_path),
        }
    }
}

impl StorageService for DefaultStorageService {
    fn get_base_path(&self) -> PathBuf {
        self.resolver.base_path().to_path_buf()
    }

    fn ensure_directories(&self) -> AppResult<()> {
        crate::storage::ensure_storage_dirs(self.resolver.base_path())
    }

    fn resolver(&self) -> &StoragePathResolver {
        &self.resolver
    }

    fn resolve_original_path(&self, filename: &str) -> PathBuf {
        self.resolver.original_path(filename)
    }

    fn resolve_edited_path(&self, filename: &str) -> PathBuf {
        self.resolver.edited_path(filename)
    }

    fn resolve_thumbnail_path(&self, original_filename: &str) -> PathBuf {
        self.resolver.thumbnail_path(original_filename)
    }

    fn resolve_video_path(&self, filename: &str) -> PathBuf {
        self.resolver.video_path(filename)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_default_storage_service_paths() {
        let svc = DefaultStorageService::new(PathBuf::from("/tmp/test"));

        assert_eq!(
            svc.resolve_original_path("a.png"),
            PathBuf::from("/tmp/test/originals/a.png")
        );
        assert_eq!(
            svc.resolve_edited_path("a.png"),
            PathBuf::from("/tmp/test/edited/a.png")
        );
        assert_eq!(
            svc.resolve_thumbnail_path("a.png"),
            PathBuf::from("/tmp/test/thumbnails/a_thumb.png")
        );
        assert_eq!(
            svc.resolve_video_path("b.mp4"),
            PathBuf::from("/tmp/test/videos/b.mp4")
        );
    }

    #[test]
    fn test_ensure_directories() {
        let temp = tempfile::tempdir().unwrap();
        let svc = DefaultStorageService::new(temp.path().to_path_buf());

        svc.ensure_directories().unwrap();

        assert!(temp.path().join("originals").exists());
        assert!(temp.path().join("edited").exists());
        assert!(temp.path().join("thumbnails").exists());
        assert!(temp.path().join("videos").exists());
    }
}

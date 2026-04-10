use std::path::{Path, PathBuf};

use crate::error::AppResult;

/// Subdirectory names for organized storage
const DIR_ORIGINALS: &str = "originals";
const DIR_EDITED: &str = "edited";
const DIR_THUMBNAILS: &str = "thumbnails";
const DIR_VIDEOS: &str = "videos";

/// Ensure that the storage directories exist under the given base path.
/// Creates: originals/, edited/, thumbnails/, videos/
pub fn ensure_storage_dirs(base_path: &Path) -> AppResult<()> {
    let subdirs = [DIR_ORIGINALS, DIR_EDITED, DIR_THUMBNAILS, DIR_VIDEOS];
    for dir in &subdirs {
        let path = base_path.join(dir);
        if !path.exists() {
            std::fs::create_dir_all(&path)?;
            tracing::info!("Created storage directory: {:?}", path);
        }
    }
    Ok(())
}

/// Get the originals directory path
pub fn originals_dir(base_path: &Path) -> PathBuf {
    base_path.join(DIR_ORIGINALS)
}

/// Get the edited directory path
pub fn edited_dir(base_path: &Path) -> PathBuf {
    base_path.join(DIR_EDITED)
}

/// Get the thumbnails directory path
pub fn thumbnails_dir(base_path: &Path) -> PathBuf {
    base_path.join(DIR_THUMBNAILS)
}

/// Get the videos directory path
pub fn videos_dir(base_path: &Path) -> PathBuf {
    base_path.join(DIR_VIDEOS)
}

/// Resolves storage paths for captured content.
///
/// Given a base save path, this struct provides deterministic path resolution
/// for all content types: originals, edited images, thumbnails, and videos.
#[derive(Debug, Clone)]
pub struct StoragePathResolver {
    base_path: PathBuf,
}

impl StoragePathResolver {
    /// Create a new resolver with the given base save path.
    pub fn new(base_path: PathBuf) -> Self {
        Self { base_path }
    }

    /// Returns the root save directory.
    pub fn base_path(&self) -> &Path {
        &self.base_path
    }

    /// Returns the path to the originals directory.
    pub fn originals_dir(&self) -> PathBuf {
        originals_dir(&self.base_path)
    }

    /// Returns the path to the edited directory.
    pub fn edited_dir(&self) -> PathBuf {
        edited_dir(&self.base_path)
    }

    /// Returns the path to the thumbnails directory.
    pub fn thumbnails_dir(&self) -> PathBuf {
        thumbnails_dir(&self.base_path)
    }

    /// Returns the path to the videos directory.
    pub fn videos_dir(&self) -> PathBuf {
        videos_dir(&self.base_path)
    }

    /// Resolve a path for an original file with the given filename.
    pub fn original_path(&self, filename: &str) -> PathBuf {
        self.originals_dir().join(filename)
    }

    /// Resolve a path for an edited file with the given filename.
    pub fn edited_path(&self, filename: &str) -> PathBuf {
        self.edited_dir().join(filename)
    }

    /// Resolve a unique thumbnail path for the given original filename.
    ///
    /// The thumbnail filename is derived from the original filename,
    /// ensuring a one-to-one mapping between originals and thumbnails.
    /// If the original is `capture_001.png`, the thumbnail will be
    /// `capture_001_thumb.png`.
    pub fn thumbnail_path(&self, original_filename: &str) -> PathBuf {
        let thumb_name = Self::thumbnail_filename(original_filename);
        self.thumbnails_dir().join(thumb_name)
    }

    /// Resolve a path for a video file with the given filename.
    pub fn video_path(&self, filename: &str) -> PathBuf {
        self.videos_dir().join(filename)
    }

    /// Generate a thumbnail filename from an original filename.
    ///
    /// Appends `_thumb` before the extension. For example:
    /// - `capture_001.png` → `capture_001_thumb.png`
    /// - `screenshot.jpg` → `screenshot_thumb.jpg`
    /// - `noext` → `noext_thumb`
    pub fn thumbnail_filename(original_filename: &str) -> String {
        let path = Path::new(original_filename);
        let file_name = path
            .file_name()
            .map(|n| n.to_string_lossy().to_string())
            .unwrap_or_default();
        let name_path = Path::new(&file_name);
        match (name_path.file_stem(), name_path.extension()) {
            (Some(stem), Some(ext)) => {
                format!("{}_thumb.{}", stem.to_string_lossy(), ext.to_string_lossy())
            }
            (Some(stem), None) => format!("{}_thumb", stem.to_string_lossy()),
            (None, _) => format!("{original_filename}_thumb"),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_ensure_storage_dirs_creates_all() {
        let temp = tempfile::tempdir().unwrap();
        let base = temp.path();

        ensure_storage_dirs(base).unwrap();

        assert!(base.join("originals").exists());
        assert!(base.join("edited").exists());
        assert!(base.join("thumbnails").exists());
        assert!(base.join("videos").exists());
    }

    #[test]
    fn test_ensure_storage_dirs_idempotent() {
        let temp = tempfile::tempdir().unwrap();
        let base = temp.path();

        ensure_storage_dirs(base).unwrap();
        ensure_storage_dirs(base).unwrap(); // Should not fail

        assert!(base.join("originals").exists());
    }

    #[test]
    fn test_resolver_originals_path() {
        let resolver = StoragePathResolver::new(PathBuf::from("/data/AmeCapture"));
        assert_eq!(
            resolver.original_path("img_001.png"),
            PathBuf::from("/data/AmeCapture/originals/img_001.png")
        );
    }

    #[test]
    fn test_resolver_edited_path() {
        let resolver = StoragePathResolver::new(PathBuf::from("/data/AmeCapture"));
        assert_eq!(
            resolver.edited_path("img_001.png"),
            PathBuf::from("/data/AmeCapture/edited/img_001.png")
        );
    }

    #[test]
    fn test_resolver_thumbnail_path() {
        let resolver = StoragePathResolver::new(PathBuf::from("/data/AmeCapture"));
        assert_eq!(
            resolver.thumbnail_path("img_001.png"),
            PathBuf::from("/data/AmeCapture/thumbnails/img_001_thumb.png")
        );
    }

    #[test]
    fn test_resolver_video_path() {
        let resolver = StoragePathResolver::new(PathBuf::from("/data/AmeCapture"));
        assert_eq!(
            resolver.video_path("rec_001.mp4"),
            PathBuf::from("/data/AmeCapture/videos/rec_001.mp4")
        );
    }

    #[test]
    fn test_thumbnail_filename_with_extension() {
        assert_eq!(
            StoragePathResolver::thumbnail_filename("capture_001.png"),
            "capture_001_thumb.png"
        );
    }

    #[test]
    fn test_thumbnail_filename_jpg() {
        assert_eq!(
            StoragePathResolver::thumbnail_filename("screenshot.jpg"),
            "screenshot_thumb.jpg"
        );
    }

    #[test]
    fn test_thumbnail_filename_no_extension() {
        assert_eq!(
            StoragePathResolver::thumbnail_filename("noext"),
            "noext_thumb"
        );
    }

    #[test]
    fn test_thumbnail_filename_with_subdirectory() {
        assert_eq!(
            StoragePathResolver::thumbnail_filename("subdir/capture_001.png"),
            "capture_001_thumb.png"
        );
    }

    #[test]
    fn test_thumbnail_filename_with_deep_subdirectory() {
        assert_eq!(
            StoragePathResolver::thumbnail_filename("a/b/c/photo.jpg"),
            "photo_thumb.jpg"
        );
    }

    #[test]
    fn test_thumbnail_filename_with_path_traversal() {
        assert_eq!(
            StoragePathResolver::thumbnail_filename("../../../etc/passwd"),
            "passwd_thumb"
        );
    }

    #[test]
    fn test_resolver_directories() {
        let resolver = StoragePathResolver::new(PathBuf::from("/data/AmeCapture"));
        assert_eq!(
            resolver.originals_dir(),
            PathBuf::from("/data/AmeCapture/originals")
        );
        assert_eq!(
            resolver.edited_dir(),
            PathBuf::from("/data/AmeCapture/edited")
        );
        assert_eq!(
            resolver.thumbnails_dir(),
            PathBuf::from("/data/AmeCapture/thumbnails")
        );
        assert_eq!(
            resolver.videos_dir(),
            PathBuf::from("/data/AmeCapture/videos")
        );
    }
}

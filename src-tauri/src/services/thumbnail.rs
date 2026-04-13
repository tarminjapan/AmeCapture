use crate::error::AppResult;

pub trait ThumbnailService: Send + Sync {
    fn generate_thumbnail(&self, source_path: &str, thumbnail_path: &str) -> AppResult<String>;
}

pub struct DefaultThumbnailService;

impl DefaultThumbnailService {
    pub fn new() -> Self {
        Self
    }
}

impl ThumbnailService for DefaultThumbnailService {
    fn generate_thumbnail(&self, source_path: &str, thumbnail_path: &str) -> AppResult<String> {
        let img = image::open(source_path)?;
        let thumbnail = img.thumbnail(256, 256);

        let path = std::path::Path::new(thumbnail_path);
        if let Some(parent) = path.parent() {
            std::fs::create_dir_all(parent)?;
        }

        thumbnail.save(path)?;

        Ok(thumbnail_path.to_string())
    }
}

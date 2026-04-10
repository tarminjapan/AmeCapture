use crate::config::app_settings::AppSettings;
use crate::error::AppResult;
use crate::repositories::settings::SettingsRepository;

/// Service trait for settings operations
pub trait SettingsService: Send + Sync {
    fn get_settings(&self) -> AppResult<AppSettings>;
    fn save_settings(&self, settings: &AppSettings) -> AppResult<()>;
}

/// Default settings service implementation
pub struct DefaultSettingsService<R: SettingsRepository> {
    repo: R,
}

impl<R: SettingsRepository> DefaultSettingsService<R> {
    pub fn new(repo: R) -> Self {
        Self { repo }
    }
}

impl<R: SettingsRepository> SettingsService for DefaultSettingsService<R> {
    fn get_settings(&self) -> AppResult<AppSettings> {
        tracing::debug!("Loading application settings");

        let settings = self.repo.get_all()?;
        let mut app_settings = AppSettings::default();

        for (key, value) in settings {
            match key.as_str() {
                "save_path" => app_settings.save_path = value,
                "image_format" => app_settings.image_format = value,
                "start_minimized" => app_settings.start_minimized = value == "true",
                "hotkey_capture_region" => app_settings.hotkey_capture_region = value,
                "hotkey_capture_fullscreen" => app_settings.hotkey_capture_fullscreen = value,
                "hotkey_capture_window" => app_settings.hotkey_capture_window = value,
                _ => tracing::warn!("Unknown setting key: {}", key),
            }
        }

        tracing::debug!("Settings loaded successfully");
        Ok(app_settings)
    }

    fn save_settings(&self, settings: &AppSettings) -> AppResult<()> {
        tracing::info!("Saving application settings");

        self.repo.set("save_path", &settings.save_path)?;
        self.repo.set("image_format", &settings.image_format)?;
        self.repo.set(
            "start_minimized",
            if settings.start_minimized {
                "true"
            } else {
                "false"
            },
        )?;
        self.repo
            .set("hotkey_capture_region", &settings.hotkey_capture_region)?;
        self.repo.set(
            "hotkey_capture_fullscreen",
            &settings.hotkey_capture_fullscreen,
        )?;
        self.repo
            .set("hotkey_capture_window", &settings.hotkey_capture_window)?;

        tracing::info!("Settings saved successfully");
        Ok(())
    }
}

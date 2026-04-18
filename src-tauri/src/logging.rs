use std::path::PathBuf;
use tracing_subscriber::EnvFilter;

/// Initialize the tracing/logging system with file and console output.
///
/// Log files are written to the given `log_dir` directory.
/// Returns a `WorkerGuard` that must be kept alive for the duration of the application
/// to ensure log files are flushed on shutdown.
pub fn init_logging(log_dir: PathBuf) -> Option<tracing_appender::non_blocking::WorkerGuard> {
    // Ensure log directory exists
    if let Err(e) = std::fs::create_dir_all(&log_dir) {
        eprintln!("Failed to create log directory {:?}: {}", log_dir, e);
        // Fall back to console-only logging
        init_console_only();
        return None;
    }

    let file_appender = tracing_appender::rolling::daily(&log_dir, "amecapture.log");
    let (non_blocking, guard) = tracing_appender::non_blocking(file_appender);

    // Default filter: DEBUG in dev build, INFO in release build.
    // Can be overridden with RUST_LOG env variable.
    let default_level = if cfg!(debug_assertions) {
        "ame_capture=debug,frontend=debug,warn"
    } else {
        "ame_capture=info,frontend=info,warn"
    };
    let filter =
        EnvFilter::try_from_default_env().unwrap_or_else(|_| EnvFilter::new(default_level));

    // Write to both file and stdout (for dev visibility)
    use tracing_subscriber::layer::SubscriberExt;
    use tracing_subscriber::util::SubscriberInitExt;

    let file_layer = tracing_subscriber::fmt::layer()
        .with_writer(non_blocking)
        .with_ansi(false)
        .with_target(true)
        .with_thread_ids(false)
        .with_file(true)
        .with_line_number(true);

    let console_layer = tracing_subscriber::fmt::layer()
        .with_writer(std::io::stdout)
        .with_ansi(true)
        .with_target(true)
        .with_thread_ids(false)
        .with_file(false)
        .with_line_number(false);

    tracing_subscriber::registry()
        .with(filter)
        .with(file_layer)
        .with(console_layer)
        .init();

    tracing::info!("Logging initialized. Log directory: {:?}", log_dir);

    Some(guard)
}

/// Console-only logging fallback
fn init_console_only() {
    let filter = EnvFilter::try_from_default_env()
        .unwrap_or_else(|_| EnvFilter::new("ame_capture=info,frontend=info,warn"));

    tracing_subscriber::fmt()
        .with_env_filter(filter)
        .with_target(true)
        .init();

    tracing::warn!("Logging initialized (console only - log directory not available)");
}

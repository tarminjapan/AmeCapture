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

    let filter = EnvFilter::try_from_default_env()
        .unwrap_or_else(|_| EnvFilter::new("ame_capture=info,warn"));

    tracing_subscriber::fmt()
        .with_env_filter(filter)
        .with_writer(non_blocking)
        .with_ansi(false)
        .with_target(true)
        .with_thread_ids(false)
        .with_file(true)
        .with_line_number(true)
        .init();

    tracing::info!("Logging initialized. Log directory: {:?}", log_dir);

    Some(guard)
}

/// Console-only logging fallback
fn init_console_only() {
    let filter = EnvFilter::try_from_default_env()
        .unwrap_or_else(|_| EnvFilter::new("ame_capture=info,warn"));

    tracing_subscriber::fmt()
        .with_env_filter(filter)
        .with_target(true)
        .init();

    tracing::warn!("Logging initialized (console only - log directory not available)");
}

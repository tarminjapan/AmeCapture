/**
 * Frontend logger utility.
 *
 * - Outputs to the browser console (always)
 * - Sends to the Rust backend via `frontend_log` Tauri command,
 *   which writes to the same log file as the Rust side.
 * - Log level is configurable and persisted via localStorage.
 */

export type LogLevel = 'trace' | 'debug' | 'info' | 'warn' | 'error';

const LOG_LEVEL_ORDER: LogLevel[] = ['trace', 'debug', 'info', 'warn', 'error'];
const STORAGE_KEY = 'amecapture_log_level';

const DEFAULT_LEVEL: LogLevel = import.meta.env.DEV ? 'debug' : 'info';

let cachedLevel: LogLevel | null = null;

function loadLevel(): LogLevel {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored && LOG_LEVEL_ORDER.includes(stored as LogLevel)) {
      return stored as LogLevel;
    }
  } catch {
    // ignore
  }
  return DEFAULT_LEVEL;
}

/** Get the current minimum log level (persisted in localStorage, cached in memory) */
export function getLogLevel(): LogLevel {
  if (cachedLevel === null) {
    cachedLevel = loadLevel();
  }
  return cachedLevel;
}

/** Set the minimum log level (persisted in localStorage, cached in memory) */
export function setLogLevel(level: LogLevel): void {
  cachedLevel = level;
  try {
    localStorage.setItem(STORAGE_KEY, level);
  } catch {
    // ignore
  }
  console.info(`[Logger] Log level set to: ${level}`);
}

/** Available log levels for UI display */
export const AVAILABLE_LOG_LEVELS: LogLevel[] = [...LOG_LEVEL_ORDER];

function shouldLog(level: LogLevel): boolean {
  const current = getLogLevel();
  return LOG_LEVEL_ORDER.indexOf(level) >= LOG_LEVEL_ORDER.indexOf(current);
}

let invokeCache: ((cmd: string, args?: Record<string, unknown>) => Promise<unknown>) | null = null;

async function getInvoke() {
  if (invokeCache !== null) return invokeCache;
  const { invoke } = await import('@tauri-apps/api/core');
  invokeCache = invoke;
  return invoke;
}

async function sendToBackend(level: LogLevel, args: unknown[]): Promise<void> {
  try {
    const invoke = await getInvoke();
    const message = args
      .map((a) => {
        if (a instanceof Error) {
          return `${a.message}\n${a.stack ?? ''}`;
        }
        if (typeof a === 'object' && a !== null) {
          try {
            return JSON.stringify(a);
          } catch {
            return String(a);
          }
        }
        return String(a);
      })
      .join(' ');

    await invoke('frontend_log', {
      entry: { level, message },
    });
  } catch {
    // Tauri not available (e.g. plain browser dev) — silently ignore
  }
}

export const logger = {
  trace(...args: unknown[]) {
    if (!shouldLog('trace')) return;
    console.trace('[TRACE]', ...args);
    sendToBackend('trace', args);
  },

  debug(...args: unknown[]) {
    if (!shouldLog('debug')) return;
    console.debug('[DEBUG]', ...args);
    sendToBackend('debug', args);
  },

  info(...args: unknown[]) {
    if (!shouldLog('info')) return;
    console.info('[INFO]', ...args);
    sendToBackend('info', args);
  },

  warn(...args: unknown[]) {
    if (!shouldLog('warn')) return;
    console.warn('[WARN]', ...args);
    sendToBackend('warn', args);
  },

  error(...args: unknown[]) {
    if (!shouldLog('error')) return;
    console.error('[ERROR]', ...args);
    sendToBackend('error', args);
  },

  /** Log with a context label */
  withContext(context: string) {
    return {
      trace: (...args: unknown[]) => logger.trace(`[${context}]`, ...args),
      debug: (...args: unknown[]) => logger.debug(`[${context}]`, ...args),
      info: (...args: unknown[]) => logger.info(`[${context}]`, ...args),
      warn: (...args: unknown[]) => logger.warn(`[${context}]`, ...args),
      error: (...args: unknown[]) => logger.error(`[${context}]`, ...args),
    };
  },
};

/**
 * Install global unhandled error/rejection handlers that log via logger.
 * Called once at app startup.
 */
export function installGlobalErrorLogging() {
  window.addEventListener('error', (event) => {
    const { message, filename: source, lineno, colno, error } = event;
    logger.error('Unhandled error:', { message, source, lineno, colno, error: error?.toString() });
  });

  window.addEventListener('unhandledrejection', (event) => {
    logger.error('Unhandled promise rejection:', event.reason);
  });

  logger.info('Frontend logger initialized', { level: getLogLevel() });
}

export default logger;

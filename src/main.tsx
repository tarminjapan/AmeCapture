import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { logger, installGlobalErrorLogging } from './lib/logger';
import './main.css';

// --- グローバルエラーハンドラーの初期化（最優先）---
installGlobalErrorLogging();

logger.info('=== AmeCapture Frontend Bootstrap Start ===');
logger.info(`Environment: ${import.meta.env.DEV ? 'development' : 'production'}`);
logger.info(`Mode: ${import.meta.env.MODE}`);

// --- DOM書き込み用のフォールバック ---
function showErrorInDom(message: string) {
  const root = document.getElementById('root');
  if (root) {
    root.innerHTML = `
      <div style="padding:24px;margin:24px;background:#fef2f2;border:2px solid #ef4444;border-radius:8px;font-family:monospace;font-size:13px;color:#1a1a1a;max-height:90vh;overflow:auto;">
        <h2 style="color:#dc2626;margin-top:0;">⚠️ アプリケーションの起動に失敗しました</h2>
        <pre style="background:#fff;padding:12px;border-radius:4px;border:1px solid #e5e7eb;white-space:pre-wrap;word-break:break-word;">${message}</pre>
        <p style="color:#6b7280;font-size:12px;">このエラーはログファイルにも出力されています。</p>
      </div>
    `;
  }
}

// --- 動的インポートで段階的に起動し、各ステップをログ出力 ---
async function bootstrap() {
  try {
    logger.info('[Bootstrap] Step 1: Loading ErrorBoundary...');
    const { ErrorBoundary } = await import('./components/ErrorBoundary');
    logger.info('[Bootstrap] Step 1: ErrorBoundary loaded OK');

    logger.info('[Bootstrap] Step 2: Loading App...');
    const { default: App } = await import('./App');
    logger.info('[Bootstrap] Step 2: App loaded OK');

    logger.info('[Bootstrap] Step 3: Mounting React...');
    const root = document.getElementById('root');
    if (!root) {
      throw new Error('Root element #root not found in DOM');
    }

    createRoot(root).render(
      <StrictMode>
        <ErrorBoundary>
          <App />
        </ErrorBoundary>
      </StrictMode>,
    );
    logger.info('[Bootstrap] Step 3: React mounted OK');
    logger.info('=== AmeCapture Frontend Bootstrap Complete ===');
  } catch (error) {
    const message =
      error instanceof Error
        ? `${error.message}\n\nStack:\n${error.stack ?? '(no stack)'}`
        : String(error);

    logger.error('[Bootstrap] FATAL: Bootstrap failed', error);
    showErrorInDom(message);
  }
}

bootstrap();

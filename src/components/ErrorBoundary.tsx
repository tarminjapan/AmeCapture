import { Component, type ErrorInfo, type ReactNode } from 'react';
import { logger } from '@/lib/logger';

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    // コンソール＆ログファイルに詳細なエラー情報を出力
    logger.error('=== ErrorBoundary: React rendering error caught ===');
    logger.error('Error:', error);
    logger.error('Error message:', error.message);
    logger.error('Error stack:', error.stack ?? 'No stack trace');
    logger.error('Component stack:', errorInfo.componentStack);
    logger.error('================================================');

    this.setState({ errorInfo });
  }

  render() {
    if (this.state.hasError) {
      const { error, errorInfo } = this.state;

      return (
        <div
          style={{
            padding: '24px',
            margin: '24px',
            backgroundColor: '#fef2f2',
            border: '2px solid #ef4444',
            borderRadius: '8px',
            fontFamily: 'monospace',
            fontSize: '13px',
            color: '#1a1a1a',
            maxHeight: '90vh',
            overflow: 'auto',
          }}
        >
          <h2 style={{ color: '#dc2626', marginTop: 0 }}>
            ⚠️ アプリケーションエラーが発生しました
          </h2>

          <details open style={{ marginBottom: '16px' }}>
            <summary style={{ cursor: 'pointer', fontWeight: 'bold', marginBottom: '8px' }}>
              エラーメッセージ
            </summary>
            <pre
              style={{
                backgroundColor: '#fff',
                padding: '12px',
                borderRadius: '4px',
                border: '1px solid #e5e7eb',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
              }}
            >
              {error?.toString()}
            </pre>
          </details>

          <details open style={{ marginBottom: '16px' }}>
            <summary style={{ cursor: 'pointer', fontWeight: 'bold', marginBottom: '8px' }}>
              エラースタックトレース
            </summary>
            <pre
              style={{
                backgroundColor: '#fff',
                padding: '12px',
                borderRadius: '4px',
                border: '1px solid #e5e7eb',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                maxHeight: '200px',
                overflow: 'auto',
              }}
            >
              {error?.stack}
            </pre>
          </details>

          <details style={{ marginBottom: '16px' }}>
            <summary style={{ cursor: 'pointer', fontWeight: 'bold', marginBottom: '8px' }}>
              コンポーネントスタック
            </summary>
            <pre
              style={{
                backgroundColor: '#fff',
                padding: '12px',
                borderRadius: '4px',
                border: '1px solid #e5e7eb',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
                maxHeight: '200px',
                overflow: 'auto',
              }}
            >
              {errorInfo?.componentStack}
            </pre>
          </details>

          <p style={{ color: '#6b7280', fontSize: '12px' }}>
            このエラーはDevToolsのコンソールにも出力されています。開発者に共有してください。
          </p>
        </div>
      );
    }

    return this.props.children;
  }
}

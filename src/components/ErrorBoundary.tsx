import { Component, type ErrorInfo, type ReactNode } from 'react';
import { logger } from '@/lib/logger';

interface ErrorBoundaryProps {
  children: ReactNode;
}

interface ErrorBoundaryState {
  hasError: boolean;
  error: Error | null;
  errorInfo: ErrorInfo | null;
  copied: boolean;
}

export class ErrorBoundary extends Component<ErrorBoundaryProps, ErrorBoundaryState> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false, error: null, errorInfo: null, copied: false };
  }

  static getDerivedStateFromError(error: Error): Partial<ErrorBoundaryState> {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    logger.error('=== ErrorBoundary: React rendering error caught ===');
    logger.error('Error:', error);
    logger.error('Error message:', error.message);
    logger.error('Error stack:', error.stack ?? 'No stack trace');
    logger.error('Component stack:', errorInfo.componentStack);
    logger.error('================================================');

    this.setState({ errorInfo });
  }

  private handleCopy = () => {
    const { error, errorInfo } = this.state;
    const lines: string[] = [
      '⚠️ アプリケーションエラーが発生しました',
      '',
      '【エラーメッセージ】',
      error?.toString() ?? '',
      '',
      '【エラースタックトレース】',
      error?.stack ?? '',
      '',
      '【コンポーネントスタック】',
      errorInfo?.componentStack ?? '',
    ];
    navigator.clipboard.writeText(lines.join('\n')).then(() => {
      this.setState({ copied: true });
      setTimeout(() => this.setState({ copied: false }), 2000);
    });
  };

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

          <button
            type="button"
            onClick={this.handleCopy}
            style={{
              marginBottom: '16px',
              padding: '6px 16px',
              fontSize: '13px',
              cursor: 'pointer',
              border: '1px solid #d1d5db',
              borderRadius: '4px',
              backgroundColor: this.state.copied ? '#d1fae5' : '#fff',
              color: this.state.copied ? '#065f46' : '#374151',
            }}
          >
            {this.state.copied ? '✅ コピーしました' : '📋 エラー情報をコピー'}
          </button>

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

import { useEffect, useRef } from 'react';
import { listen } from '@tauri-apps/api/event';
import type { CaptureAction } from './useGlobalShortcut';

export function useTrayCapture(onAction: (action: CaptureAction) => void) {
  const onActionRef = useRef(onAction);
  onActionRef.current = onAction;

  useEffect(() => {
    const unlisten = listen<string>('tray-capture', (event) => {
      const action = event.payload as CaptureAction;
      if (action === 'region' || action === 'fullscreen' || action === 'window') {
        onActionRef.current(action);
      }
    });

    return () => {
      unlisten.then((fn) => fn());
    };
  }, []);
}

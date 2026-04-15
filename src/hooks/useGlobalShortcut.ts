import { useEffect, useRef } from 'react';
import { register, unregisterAll } from '@tauri-apps/plugin-global-shortcut';
import { invoke } from '@tauri-apps/api/core';
import type { AppSettings, CommandResult } from '@/types';

export type CaptureAction = 'region' | 'fullscreen' | 'window';

export function useGlobalShortcut(onAction: (action: CaptureAction) => void) {
  const onActionRef = useRef(onAction);
  onActionRef.current = onAction;

  useEffect(() => {
    let cancelled = false;

    const setup = async () => {
      try {
        const result = await invoke<CommandResult<AppSettings>>('get_settings');
        if (!result.success || !result.data) return;

        const { hotkeyCaptureRegion, hotkeyCaptureFullscreen, hotkeyCaptureWindow } = result.data;

        const entries: [string, CaptureAction][] = [];
        if (hotkeyCaptureRegion) entries.push([hotkeyCaptureRegion, 'region']);
        if (hotkeyCaptureFullscreen) entries.push([hotkeyCaptureFullscreen, 'fullscreen']);
        if (hotkeyCaptureWindow) entries.push([hotkeyCaptureWindow, 'window']);

        if (entries.length === 0) return;

        const shortcuts = entries.map(([s]) => s);
        const shortcutMap = Object.fromEntries(entries);

        try {
          await register(shortcuts, (event) => {
            if (event.state !== 'Pressed' || cancelled) return;
            const action = shortcutMap[event.shortcut];
            if (action) {
              onActionRef.current(action);
            }
          });
        } catch {
          for (const [shortcut, action] of entries) {
            try {
              await register(shortcut, (event) => {
                if (event.state !== 'Pressed' || cancelled) return;
                onActionRef.current(action);
              });
            } catch (e) {
              console.warn(`Failed to register hotkey "${shortcut}" for ${action}:`, e);
            }
          }
        }
      } catch (error) {
        console.error('Failed to setup global shortcuts:', error);
      }
    };

    setup();

    return () => {
      cancelled = true;
      unregisterAll().catch(() => {});
    };
  }, []);
}

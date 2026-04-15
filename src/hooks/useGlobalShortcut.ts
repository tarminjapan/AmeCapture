import { useEffect, useRef } from 'react';
import { register, unregister } from '@tauri-apps/plugin-global-shortcut';
import { invoke } from '@tauri-apps/api/core';
import type { AppSettings, CommandResult } from '@/types';

export type CaptureAction = 'region' | 'fullscreen' | 'window';

export function useGlobalShortcut(onAction: (action: CaptureAction) => void) {
  const onActionRef = useRef(onAction);
  onActionRef.current = onAction;

  useEffect(() => {
    let cancelled = false;
    const registeredShortcuts: string[] = [];

    const setup = async () => {
      try {
        const result = await invoke<CommandResult<AppSettings>>('get_settings');
        if (cancelled || !result.success || !result.data) return;

        const { hotkeyCaptureRegion, hotkeyCaptureFullscreen, hotkeyCaptureWindow } = result.data;

        const entries: [string, CaptureAction][] = [];
        if (hotkeyCaptureRegion) entries.push([hotkeyCaptureRegion, 'region']);
        if (hotkeyCaptureFullscreen) entries.push([hotkeyCaptureFullscreen, 'fullscreen']);
        if (hotkeyCaptureWindow) entries.push([hotkeyCaptureWindow, 'window']);

        if (entries.length === 0 || cancelled) return;

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
          registeredShortcuts.push(...shortcuts);
        } catch {
          if (cancelled) return;
          for (const [shortcut, action] of entries) {
            if (cancelled) break;
            try {
              await register(shortcut, (event) => {
                if (event.state !== 'Pressed' || cancelled) return;
                onActionRef.current(action);
              });
              registeredShortcuts.push(shortcut);
            } catch (e) {
              console.warn(`Failed to register hotkey "${shortcut}" for ${action}:`, e);
            }
          }
        }

        if (cancelled && registeredShortcuts.length > 0) {
          unregister(registeredShortcuts).catch(() => {});
        }
      } catch (error) {
        if (!cancelled) {
          console.error('Failed to setup global shortcuts:', error);
        }
      }
    };

    setup();

    return () => {
      cancelled = true;
      if (registeredShortcuts.length > 0) {
        unregister(registeredShortcuts).catch(() => {});
      }
    };
  }, []);
}

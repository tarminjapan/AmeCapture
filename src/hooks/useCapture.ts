import { invoke } from '@tauri-apps/api/core';
import type { CaptureRegion, CaptureType, CommandResult, WorkspaceItem } from '@/types';
import { useWorkspaceStore } from '@/stores/workspaceStore';

export function useCapture() {
  const addItem = useWorkspaceStore((s) => s.addItem);

  const capture = async (type: CaptureType, region?: CaptureRegion) => {
    try {
      const result = await invoke<CommandResult<WorkspaceItem>>('capture', {
        type,
        region: region ?? null,
      });
      if (result.success && result.data) {
        addItem(result.data);
      }
      return result;
    } catch (error) {
      console.error('Capture failed:', error);
      return { success: false, data: null, error: String(error) };
    }
  };

  const captureFullscreen = () => capture('fullscreen');
  const captureRegion = (region: CaptureRegion) => capture('region', region);
  const captureWindow = () => capture('window');

  return {
    captureFullscreen,
    captureRegion,
    captureWindow,
  };
}

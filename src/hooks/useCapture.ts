import { invoke } from '@tauri-apps/api/core';
import type {
  CaptureRegion,
  CaptureType,
  CommandResult,
  RegionCaptureInfo,
  WindowCaptureInfo,
  WorkspaceItem,
} from '@/types';
import { useWorkspaceStore } from '@/stores/workspaceStore';

export function useCapture() {
  const addItem = useWorkspaceStore((s) => s.addItem);

  const capture = async (type: CaptureType, region?: CaptureRegion, hwnd?: number) => {
    try {
      const result = await invoke<CommandResult<WorkspaceItem>>('capture', {
        type,
        region: region ?? null,
        hwnd: hwnd ?? null,
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
  const captureWindow = (hwnd: number) => capture('window', undefined, hwnd);

  const prepareRegionCapture = async (): Promise<CommandResult<RegionCaptureInfo>> => {
    try {
      const result = await invoke<CommandResult<RegionCaptureInfo>>('prepare_region_capture');
      return result;
    } catch (error) {
      console.error('Prepare region capture failed:', error);
      return { success: false, data: null, error: String(error) };
    }
  };

  const finalizeRegionCapture = async (
    sourcePath: string,
    region: CaptureRegion,
  ): Promise<CommandResult<WorkspaceItem>> => {
    try {
      const result = await invoke<CommandResult<WorkspaceItem>>('finalize_region_capture', {
        sourcePath,
        region,
      });
      if (result.success && result.data) {
        addItem(result.data);
      }
      return result;
    } catch (error) {
      console.error('Finalize region capture failed:', error);
      return { success: false, data: null, error: String(error) };
    }
  };

  const cancelRegionCapture = async (sourcePath: string): Promise<void> => {
    try {
      await invoke<CommandResult<null>>('cancel_region_capture', { sourcePath });
    } catch (error) {
      console.error('Cancel region capture failed:', error);
    }
  };

  const prepareWindowCapture = async (): Promise<CommandResult<WindowCaptureInfo>> => {
    try {
      const result = await invoke<CommandResult<WindowCaptureInfo>>('prepare_window_capture');
      return result;
    } catch (error) {
      console.error('Prepare window capture failed:', error);
      return { success: false, data: null, error: String(error) };
    }
  };

  return {
    captureFullscreen,
    captureRegion,
    captureWindow,
    prepareRegionCapture,
    finalizeRegionCapture,
    cancelRegionCapture,
    prepareWindowCapture,
  };
}

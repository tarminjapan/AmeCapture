import { invoke } from '@tauri-apps/api/core';
import type { CommandResult } from '@/types';
import { useEditorStore } from '@/stores/editorStore';

export function useEditor() {
  const store = useEditorStore();

  const saveEdit = async (itemId: string) => {
    try {
      const editData = JSON.stringify({ annotations: store.annotations });
      const result = await invoke<CommandResult<string>>('save_edit', {
        itemId,
        editData,
      });
      if (result.success) {
        store.setDirty(false);
      }
      return result;
    } catch (error) {
      console.error('Failed to save edit:', error);
      return { success: false, data: null, error: String(error) };
    }
  };

  const undo = () => {
    store.undoAnnotations();
  };

  const redo = () => {
    store.redoAnnotations();
  };

  return {
    ...store,
    saveEdit,
    undo,
    redo,
  };
}

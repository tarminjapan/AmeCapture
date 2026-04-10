import { invoke } from "@tauri-apps/api/core";
import type { CommandResult } from "@/types";
import { useEditorStore } from "@/stores/editorStore";

export function useEditor() {
  const store = useEditorStore();

  const saveEdit = async (itemId: string) => {
    try {
      const result = await invoke<CommandResult<string>>("save_edit", {
        itemId,
      });
      if (result.success) {
        store.setDirty(false);
      }
      return result;
    } catch (error) {
      console.error("Failed to save edit:", error);
      return { success: false, data: null, error: String(error) };
    }
  };

  const undo = async () => {
    try {
      const result = await invoke<CommandResult<null>>("undo");
      if (result.success) {
        // Backend will return updated state
      }
    } catch (error) {
      console.error("Undo failed:", error);
    }
  };

  const redo = async () => {
    try {
      const result = await invoke<CommandResult<null>>("redo");
      if (result.success) {
        // Backend will return updated state
      }
    } catch (error) {
      console.error("Redo failed:", error);
    }
  };

  return {
    ...store,
    saveEdit,
    undo,
    redo,
  };
}
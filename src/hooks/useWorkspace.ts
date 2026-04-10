import { invoke } from '@tauri-apps/api/core';
import type { WorkspaceItem, CommandResult } from '@/types';
import { useWorkspaceStore } from '@/stores/workspaceStore';

export function useWorkspace() {
  const { setItems, removeItem, updateItem, setLoading } = useWorkspaceStore();

  const loadItems = async () => {
    setLoading(true);
    try {
      const result = await invoke<CommandResult<WorkspaceItem[]>>('get_workspace_items');
      if (result.success && result.data) {
        setItems(result.data);
      }
    } catch (error) {
      console.error('Failed to load workspace items:', error);
    } finally {
      setLoading(false);
    }
  };

  const deleteItem = async (id: string) => {
    try {
      const result = await invoke<CommandResult<null>>('delete_workspace_item', {
        id,
      });
      if (result.success) {
        removeItem(id);
      }
    } catch (error) {
      console.error('Failed to delete item:', error);
    }
  };

  const renameItem = async (id: string, title: string) => {
    try {
      const result = await invoke<CommandResult<null>>('rename_workspace_item', {
        id,
        title,
      });
      if (result.success) {
        updateItem(id, { title });
      }
    } catch (error) {
      console.error('Failed to rename item:', error);
    }
  };

  const toggleFavorite = async (id: string, isFavorite: boolean) => {
    try {
      const result = await invoke<CommandResult<null>>('toggle_favorite', { id, isFavorite });
      if (result.success) {
        updateItem(id, { isFavorite });
      }
    } catch (error) {
      console.error('Failed to toggle favorite:', error);
    }
  };

  return {
    loadItems,
    deleteItem,
    renameItem,
    toggleFavorite,
  };
}

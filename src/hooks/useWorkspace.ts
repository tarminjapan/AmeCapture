import { useCallback } from 'react';
import { invoke } from '@tauri-apps/api/core';
import type { WorkspaceItem, Tag, CommandResult } from '@/types';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import { useTagStore } from '@/stores/tagStore';

export function useWorkspace() {
  const setItems = useWorkspaceStore((s) => s.setItems);
  const removeItem = useWorkspaceStore((s) => s.removeItem);
  const updateItem = useWorkspaceStore((s) => s.updateItem);
  const setLoading = useWorkspaceStore((s) => s.setLoading);

  const loadItems = useCallback(async () => {
    setLoading(true);
    try {
      const result = await invoke<CommandResult<WorkspaceItem[]>>('get_workspace_items');
      if (result.success && result.data) {
        setItems(result.data);
        try {
          const itemIds = result.data.map((item) => item.id);
          const tagsResult = await invoke<CommandResult<Record<string, Tag[]>>>(
            'get_all_tags_for_items',
            { itemIds },
          );
          if (tagsResult.success && tagsResult.data) {
            const tagStore = useTagStore.getState();
            for (const [itemId, tags] of Object.entries(tagsResult.data)) {
              tagStore.setItemTags(itemId, tags);
            }
          }
        } catch {
          // skip if tags fail
        }
      }
    } catch (error) {
      console.error('Failed to load workspace items:', error);
    } finally {
      setLoading(false);
    }
  }, [setItems, setLoading]);

  const deleteItem = useCallback(
    async (id: string) => {
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
    },
    [removeItem],
  );

  const renameItem = useCallback(
    async (id: string, title: string) => {
      try {
        const result = await invoke<CommandResult<WorkspaceItem>>('rename_workspace_item', {
          id,
          title,
        });
        if (result.success && result.data) {
          updateItem(id, {
            title: result.data.title,
            currentPath: result.data.currentPath,
            originalPath: result.data.originalPath,
            thumbnailPath: result.data.thumbnailPath,
            updatedAt: result.data.updatedAt,
          });
        }
      } catch (error) {
        console.error('Failed to rename item:', error);
      }
    },
    [updateItem],
  );

  const toggleFavorite = useCallback(
    async (id: string, isFavorite: boolean) => {
      try {
        const result = await invoke<CommandResult<null>>('toggle_favorite', { id, isFavorite });
        if (result.success) {
          updateItem(id, { isFavorite });
        }
      } catch (error) {
        console.error('Failed to toggle favorite:', error);
      }
    },
    [updateItem],
  );

  const showInFolder = useCallback(async (id: string) => {
    try {
      const result = await invoke<CommandResult<null>>('show_item_in_folder', { id });
      if (!result.success) {
        console.error('Failed to show item in folder:', result.error);
      }
    } catch (error) {
      console.error('Failed to show item in folder:', error);
    }
  }, []);

  const copyImageToClipboard = useCallback(async (id: string) => {
    try {
      const result = await invoke<CommandResult<null>>('copy_image_to_clipboard', { id });
      if (!result.success) {
        console.error('Failed to copy image to clipboard:', result.error);
      }
    } catch (error) {
      console.error('Failed to copy image to clipboard:', error);
    }
  }, []);

  return {
    loadItems,
    deleteItem,
    renameItem,
    toggleFavorite,
    showInFolder,
    copyImageToClipboard,
  };
}

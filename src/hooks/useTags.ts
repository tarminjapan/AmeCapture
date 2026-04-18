import { useCallback } from 'react';
import { invoke } from '@tauri-apps/api/core';
import type { Tag, CommandResult } from '@/types';
import { useTagStore } from '@/stores/tagStore';

export function useTags() {
  const setTags = useTagStore((s) => s.setTags);
  const addTag = useTagStore((s) => s.addTag);
  const removeTag = useTagStore((s) => s.removeTag);
  const setItemTags = useTagStore((s) => s.setItemTags);
  const setLoading = useTagStore((s) => s.setLoading);

  const loadTags = useCallback(async () => {
    setLoading(true);
    try {
      const result = await invoke<CommandResult<Tag[]>>('get_tags');
      if (result.success && result.data) {
        setTags(result.data);
      }
    } catch (error) {
      console.error('Failed to load tags:', error);
    } finally {
      setLoading(false);
    }
  }, [setTags, setLoading]);

  const createTag = useCallback(
    async (name: string): Promise<Tag | null> => {
      try {
        const result = await invoke<CommandResult<Tag>>('create_tag', { name });
        if (result.success && result.data) {
          addTag(result.data);
          return result.data;
        }
      } catch (error) {
        console.error('Failed to create tag:', error);
      }
      return null;
    },
    [addTag],
  );

  const deleteTag = useCallback(
    async (id: string) => {
      try {
        const result = await invoke<CommandResult<null>>('delete_tag', { id });
        if (result.success) {
          removeTag(id);
        }
      } catch (error) {
        console.error('Failed to delete tag:', error);
      }
    },
    [removeTag],
  );

  const loadTagsForItem = useCallback(
    async (itemId: string) => {
      try {
        const result = await invoke<CommandResult<Tag[]>>('get_tags_for_item', { itemId });
        if (result.success && result.data) {
          setItemTags(itemId, result.data);
        }
      } catch (error) {
        console.error('Failed to load tags for item:', error);
      }
    },
    [setItemTags],
  );

  const addTagToItem = useCallback(
    async (itemId: string, tagId: string) => {
      try {
        const result = await invoke<CommandResult<null>>('add_tag_to_item', { itemId, tagId });
        if (result.success) {
          await loadTagsForItem(itemId);
        }
      } catch (error) {
        console.error('Failed to add tag to item:', error);
      }
    },
    [loadTagsForItem],
  );

  const removeTagFromItem = useCallback(
    async (itemId: string, tagId: string) => {
      try {
        const result = await invoke<CommandResult<null>>('remove_tag_from_item', { itemId, tagId });
        if (result.success) {
          await loadTagsForItem(itemId);
        }
      } catch (error) {
        console.error('Failed to remove tag from item:', error);
      }
    },
    [loadTagsForItem],
  );

  const setTagsForItem = useCallback(
    async (itemId: string, tagIds: string[]) => {
      try {
        const result = await invoke<CommandResult<null>>('set_tags_for_item', { itemId, tagIds });
        if (result.success) {
          await loadTagsForItem(itemId);
        }
      } catch (error) {
        console.error('Failed to set tags for item:', error);
      }
    },
    [loadTagsForItem],
  );

  return {
    loadTags,
    createTag,
    deleteTag,
    loadTagsForItem,
    addTagToItem,
    removeTagFromItem,
    setTagsForItem,
  };
}

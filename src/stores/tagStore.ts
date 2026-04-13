import { create } from 'zustand';
import type { Tag } from '@/types';

interface TagState {
  tags: Tag[];
  selectedTagIds: string[];
  itemTags: Record<string, Tag[]>;
  isLoading: boolean;

  setTags: (tags: Tag[]) => void;
  addTag: (tag: Tag) => void;
  removeTag: (id: string) => void;
  setSelectedTagIds: (ids: string[]) => void;
  setItemTags: (itemId: string, tags: Tag[]) => void;
  setLoading: (loading: boolean) => void;
}

export const useTagStore = create<TagState>((set) => ({
  tags: [],
  selectedTagIds: [],
  itemTags: {},
  isLoading: false,

  setTags: (tags) => set({ tags }),
  addTag: (tag) => set((state) => ({ tags: [...state.tags, tag] })),
  removeTag: (id) =>
    set((state) => ({
      tags: state.tags.filter((t) => t.id !== id),
    })),
  setSelectedTagIds: (ids) => set({ selectedTagIds: ids }),
  setItemTags: (itemId, tags) =>
    set((state) => ({
      itemTags: { ...state.itemTags, [itemId]: tags },
    })),
  setLoading: (loading) => set({ isLoading: loading }),
}));

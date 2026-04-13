import { create } from 'zustand';
import type { WorkspaceItem } from '@/types';

interface WorkspaceState {
  items: WorkspaceItem[];
  selectedItemIds: string[];
  searchQuery: string;
  sortBy: 'createdAt' | 'updatedAt' | 'title';
  sortOrder: 'asc' | 'desc';
  isLoading: boolean;
  showFavoritesOnly: boolean;

  // Actions
  setItems: (items: WorkspaceItem[]) => void;
  addItem: (item: WorkspaceItem) => void;
  removeItem: (id: string) => void;
  updateItem: (id: string, updates: Partial<WorkspaceItem>) => void;
  setSelectedItemIds: (ids: string[]) => void;
  setSearchQuery: (query: string) => void;
  setSortBy: (sortBy: 'createdAt' | 'updatedAt' | 'title') => void;
  setSortOrder: (order: 'asc' | 'desc') => void;
  setLoading: (loading: boolean) => void;
  setShowFavoritesOnly: (show: boolean) => void;
}

export const useWorkspaceStore = create<WorkspaceState>((set) => ({
  items: [],
  selectedItemIds: [],
  searchQuery: '',
  sortBy: 'createdAt',
  sortOrder: 'desc',
  isLoading: false,
  showFavoritesOnly: false,

  setItems: (items) => set({ items }),
  addItem: (item) => set((state) => ({ items: [item, ...state.items] })),
  removeItem: (id) =>
    set((state) => ({
      items: state.items.filter((i) => i.id !== id),
      selectedItemIds: state.selectedItemIds.filter((sid) => sid !== id),
    })),
  updateItem: (id, updates) =>
    set((state) => ({
      items: state.items.map((i) => (i.id === id ? { ...i, ...updates } : i)),
    })),
  setSelectedItemIds: (ids) => set({ selectedItemIds: ids }),
  setSearchQuery: (query) => set({ searchQuery: query }),
  setSortBy: (sortBy) => set({ sortBy }),
  setSortOrder: (order) => set({ sortOrder: order }),
  setLoading: (loading) => set({ isLoading: loading }),
  setShowFavoritesOnly: (show) => set({ showFavoritesOnly: show }),
}));

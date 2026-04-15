import { create } from 'zustand';
import type { EditorTool } from '@/types';

interface EditorState {
  activeTool: EditorTool;
  strokeColor: string;
  fillColor: string;
  strokeWidth: number;
  fontSize: number;
  canUndo: boolean;
  canRedo: boolean;
  isDirty: boolean;
  zoom: number;
  panX: number;
  panY: number;
  editingItemId: string | null;

  setActiveTool: (tool: EditorTool) => void;
  setStrokeColor: (color: string) => void;
  setFillColor: (color: string) => void;
  setStrokeWidth: (width: number) => void;
  setFontSize: (size: number) => void;
  setCanUndo: (can: boolean) => void;
  setCanRedo: (can: boolean) => void;
  setDirty: (dirty: boolean) => void;
  setZoom: (zoom: number) => void;
  setPan: (x: number, y: number) => void;
  setEditingItem: (itemId: string | null) => void;
  resetEditor: () => void;
}

const initialState = {
  activeTool: 'select' as EditorTool,
  strokeColor: '#ff0000',
  fillColor: 'transparent',
  strokeWidth: 2,
  fontSize: 16,
  canUndo: false,
  canRedo: false,
  isDirty: false,
  zoom: 1,
  panX: 0,
  panY: 0,
  editingItemId: null as string | null,
};

export const useEditorStore = create<EditorState>((set) => ({
  ...initialState,

  setActiveTool: (tool) => set({ activeTool: tool }),
  setStrokeColor: (color) => set({ strokeColor: color }),
  setFillColor: (color) => set({ fillColor: color }),
  setStrokeWidth: (width) => set({ strokeWidth: width }),
  setFontSize: (size) => set({ fontSize: size }),
  setCanUndo: (can) => set({ canUndo: can }),
  setCanRedo: (can) => set({ canRedo: can }),
  setDirty: (dirty) => set({ isDirty: dirty }),
  setZoom: (zoom) => set({ zoom: Math.min(10, Math.max(0.1, zoom)) }),
  setPan: (x, y) => set({ panX: x, panY: y }),
  setEditingItem: (itemId) => set({ editingItemId: itemId }),
  resetEditor: () => set(initialState),
}));

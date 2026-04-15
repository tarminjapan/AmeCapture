import { create } from 'zustand';
import type { EditorAnnotation, EditorTool } from '@/types';

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
  annotations: EditorAnnotation[];
  annotationPast: EditorAnnotation[][];
  annotationFuture: EditorAnnotation[][];

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
  addAnnotation: (annotation: EditorAnnotation) => void;
  removeAnnotation: (id: string) => void;
  clearAnnotations: () => void;
  undoAnnotations: () => void;
  redoAnnotations: () => void;
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
  annotations: [] as EditorAnnotation[],
  annotationPast: [] as EditorAnnotation[][],
  annotationFuture: [] as EditorAnnotation[][],
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

  addAnnotation: (annotation) =>
    set((state) => {
      const newAnnotations = [...state.annotations, annotation];
      return {
        annotationPast: [...state.annotationPast, state.annotations],
        annotationFuture: [],
        annotations: newAnnotations,
        isDirty: true,
        canUndo: true,
        canRedo: false,
      };
    }),

  removeAnnotation: (id) =>
    set((state) => {
      const newAnnotations = state.annotations.filter((a) => a.id !== id);
      return {
        annotationPast: [...state.annotationPast, state.annotations],
        annotationFuture: [],
        annotations: newAnnotations,
        isDirty: newAnnotations.length > 0,
        canUndo: true,
        canRedo: false,
      };
    }),

  clearAnnotations: () =>
    set({
      annotations: [],
      annotationPast: [],
      annotationFuture: [],
      isDirty: false,
      canUndo: false,
      canRedo: false,
    }),

  undoAnnotations: () =>
    set((state) => {
      if (state.annotationPast.length === 0) return state;
      const previous = state.annotationPast[state.annotationPast.length - 1] ?? [];
      const newPast = state.annotationPast.slice(0, -1);
      return {
        annotations: previous,
        annotationPast: newPast,
        annotationFuture: [...state.annotationFuture, state.annotations],
        isDirty: previous.length > 0,
        canUndo: newPast.length > 0,
        canRedo: true,
      };
    }),

  redoAnnotations: () =>
    set((state) => {
      if (state.annotationFuture.length === 0) return state;
      const next = state.annotationFuture[state.annotationFuture.length - 1] ?? [];
      const newFuture = state.annotationFuture.slice(0, -1);
      return {
        annotations: next,
        annotationPast: [...state.annotationPast, state.annotations],
        annotationFuture: newFuture,
        isDirty: next.length > 0,
        canUndo: true,
        canRedo: newFuture.length > 0,
      };
    }),
}));

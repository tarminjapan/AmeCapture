import { create } from "zustand";
import type { EditorTool } from "@/types";

interface EditorState {
  activeTool: EditorTool;
  strokeColor: string;
  fillColor: string;
  strokeWidth: number;
  fontSize: number;
  canUndo: boolean;
  canRedo: boolean;
  isDirty: boolean;

  // Actions
  setActiveTool: (tool: EditorTool) => void;
  setStrokeColor: (color: string) => void;
  setFillColor: (color: string) => void;
  setStrokeWidth: (width: number) => void;
  setFontSize: (size: number) => void;
  setCanUndo: (can: boolean) => void;
  setCanRedo: (can: boolean) => void;
  setDirty: (dirty: boolean) => void;
}

export const useEditorStore = create<EditorState>((set) => ({
  activeTool: "select",
  strokeColor: "#ff0000",
  fillColor: "transparent",
  strokeWidth: 2,
  fontSize: 16,
  canUndo: false,
  canRedo: false,
  isDirty: false,

  setActiveTool: (tool) => set({ activeTool: tool }),
  setStrokeColor: (color) => set({ strokeColor: color }),
  setFillColor: (color) => set({ fillColor: color }),
  setStrokeWidth: (width) => set({ strokeWidth: width }),
  setFontSize: (size) => set({ fontSize: size }),
  setCanUndo: (can) => set({ canUndo: can }),
  setCanRedo: (can) => set({ canRedo: can }),
  setDirty: (dirty) => set({ isDirty: dirty }),
}));
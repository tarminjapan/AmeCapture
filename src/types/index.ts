// === Workspace Types ===

export interface WorkspaceItem {
  id: string;
  type: "image" | "video";
  originalPath: string;
  currentPath: string;
  thumbnailPath: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  isFavorite: boolean;
  metadataJson: string | null;
}

export interface Tag {
  id: string;
  name: string;
}

export interface WorkspaceItemTag {
  workspaceItemId: string;
  tagId: string;
}

// === Capture Types ===

export interface CaptureRegion {
  x: number;
  y: number;
  width: number;
  height: number;
}

export type CaptureType = "fullscreen" | "region" | "window";

// === Editor Types ===

export type EditorTool = "select" | "arrow" | "text" | "rectangle" | "mosaic" | "crop";

export interface EditorState {
  activeTool: EditorTool;
  strokeColor: string;
  fillColor: string;
  strokeWidth: number;
  fontSize: number;
  canUndo: boolean;
  canRedo: boolean;
}

// === Settings Types ===

export interface AppSettings {
  savePath: string;
  imageFormat: "png" | "jpg" | "webp";
  startMinimized: boolean;
  hotkeyCaptureRegion: string;
  hotkeyCaptureFullscreen: string;
  hotkeyCaptureWindow: string;
}

// === Tauri IPC Result ===

export interface CommandResult<T> {
  success: boolean;
  data: T | null;
  error: string | null;
}
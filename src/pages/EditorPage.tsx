import { useCallback, useEffect } from 'react';
import { useEditorStore } from '@/stores/editorStore';
import { useEditor } from '@/hooks/useEditor';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import { EditorToolbar } from '@/components/EditorToolbar';
import { EditorCanvas } from '@/components/EditorCanvas';
import { invoke } from '@tauri-apps/api/core';
import type { CommandResult } from '@/types';

interface EditorPageProps {
  onBack: () => void;
}

export default function EditorPage({ onBack }: EditorPageProps) {
  const editingItemId = useEditorStore((s) => s.editingItemId);
  const activeTool = useEditorStore((s) => s.activeTool);
  const zoom = useEditorStore((s) => s.zoom);
  const panX = useEditorStore((s) => s.panX);
  const panY = useEditorStore((s) => s.panY);
  const canUndo = useEditorStore((s) => s.canUndo);
  const canRedo = useEditorStore((s) => s.canRedo);
  const isDirty = useEditorStore((s) => s.isDirty);
  const setActiveTool = useEditorStore((s) => s.setActiveTool);
  const setZoom = useEditorStore((s) => s.setZoom);
  const setPan = useEditorStore((s) => s.setPan);
  const resetEditor = useEditorStore((s) => s.resetEditor);

  const { saveEdit, undo, redo } = useEditor();

  const items = useWorkspaceStore((s) => s.items);
  const editingItem = editingItemId ? items.find((item) => item.id === editingItemId) : null;

  useEffect(() => {
    return () => {
      resetEditor();
    };
  }, [resetEditor]);

  const handleZoomIn = useCallback(() => {
    setZoom(zoom + 0.25);
  }, [zoom, setZoom]);

  const handleZoomOut = useCallback(() => {
    setZoom(zoom - 0.25);
  }, [zoom, setZoom]);

  const handleZoomReset = useCallback(() => {
    setZoom(1);
    setPan(0, 0);
  }, [setZoom, setPan]);

  const handleSave = useCallback(async () => {
    if (!editingItemId) return;
    await saveEdit(editingItemId);
  }, [editingItemId, saveEdit]);

  const handleCopy = useCallback(async () => {
    if (!editingItemId) return;
    try {
      const result = await invoke<CommandResult<null>>('copy_image_to_clipboard', {
        id: editingItemId,
      });
      if (!result.success) {
        console.error('Failed to copy image to clipboard:', result.error);
      }
    } catch (error) {
      console.error('Failed to copy image to clipboard:', error);
    }
  }, [editingItemId]);

  const handleBack = useCallback(() => {
    resetEditor();
    onBack();
  }, [onBack, resetEditor]);

  if (!editingItem) {
    return (
      <div className="flex h-full w-full items-center justify-center">
        <div className="flex flex-col items-center gap-3 text-muted-foreground">
          <p className="text-lg font-medium">画像が選択されていません</p>
          <button
            className="px-4 py-2 text-sm rounded-md bg-primary text-primary-foreground hover:bg-primary/90"
            onClick={handleBack}
          >
            ワークスペースに戻る
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full w-full flex-col">
      <EditorToolbar
        activeTool={activeTool}
        zoom={zoom}
        canUndo={canUndo}
        canRedo={canRedo}
        isDirty={isDirty}
        onToolSelect={setActiveTool}
        onZoomIn={handleZoomIn}
        onZoomOut={handleZoomOut}
        onZoomReset={handleZoomReset}
        onUndo={undo}
        onRedo={redo}
        onSave={handleSave}
        onCopy={handleCopy}
        onBack={handleBack}
      />
      <EditorCanvas
        imagePath={editingItem.currentPath}
        zoom={zoom}
        panX={panX}
        panY={panY}
        onZoomChange={setZoom}
        onPanChange={setPan}
        className="flex-1"
      />
    </div>
  );
}

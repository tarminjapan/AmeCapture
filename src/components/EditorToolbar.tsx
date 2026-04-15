import {
  MousePointer2,
  MoveRight,
  Type,
  Square,
  Grid3x3,
  Crop,
  ZoomIn,
  ZoomOut,
  Save,
  Copy,
  ArrowLeft,
  Undo2,
  Redo2,
} from 'lucide-react';
import type { EditorTool } from '@/types';
import { cn } from '@/lib/utils';

interface EditorToolbarProps {
  activeTool: EditorTool;
  zoom: number;
  canUndo: boolean;
  canRedo: boolean;
  isDirty: boolean;
  onToolSelect: (tool: EditorTool) => void;
  onZoomIn: () => void;
  onZoomOut: () => void;
  onZoomReset: () => void;
  onUndo: () => void;
  onRedo: () => void;
  onSave: () => void;
  onCopy: () => void;
  onBack: () => void;
}

const tools: { tool: EditorTool; icon: React.ElementType; label: string }[] = [
  { tool: 'select', icon: MousePointer2, label: '選択' },
  { tool: 'arrow', icon: MoveRight, label: '矢印' },
  { tool: 'text', icon: Type, label: 'テキスト' },
  { tool: 'rectangle', icon: Square, label: '矩形' },
  { tool: 'mosaic', icon: Grid3x3, label: 'モザイク' },
  { tool: 'crop', icon: Crop, label: 'トリミング' },
];

export function EditorToolbar({
  activeTool,
  zoom,
  canUndo,
  canRedo,
  isDirty,
  onToolSelect,
  onZoomIn,
  onZoomOut,
  onZoomReset,
  onUndo,
  onRedo,
  onSave,
  onCopy,
  onBack,
}: EditorToolbarProps) {
  return (
    <div className="flex items-center justify-between border-b border-border px-4 py-2">
      <div className="flex items-center gap-1">
        <button
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md hover:bg-accent"
          onClick={onBack}
          title="ワークスペースに戻る"
        >
          <ArrowLeft className="w-4 h-4" />
          戻る
        </button>
        <div className="w-px h-6 bg-border mx-2" />
        {tools.map(({ tool, icon: Icon, label }) => (
          <button
            key={tool}
            className={cn(
              'p-1.5 rounded-md transition-colors',
              activeTool === tool ? 'bg-primary text-primary-foreground' : 'hover:bg-accent',
            )}
            onClick={() => onToolSelect(tool)}
            title={label}
          >
            <Icon className="w-4 h-4" />
          </button>
        ))}
      </div>

      <div className="flex items-center gap-1">
        <button
          className="p-1.5 rounded-md hover:bg-accent disabled:opacity-40"
          onClick={onUndo}
          disabled={!canUndo}
          title="元に戻す"
        >
          <Undo2 className="w-4 h-4" />
        </button>
        <button
          className="p-1.5 rounded-md hover:bg-accent disabled:opacity-40"
          onClick={onRedo}
          disabled={!canRedo}
          title="やり直す"
        >
          <Redo2 className="w-4 h-4" />
        </button>
        <div className="w-px h-6 bg-border mx-2" />
        <button className="p-1.5 rounded-md hover:bg-accent" onClick={onZoomOut} title="縮小">
          <ZoomOut className="w-4 h-4" />
        </button>
        <button
          className="px-2 py-1 text-xs rounded-md hover:bg-accent min-w-[60px] text-center"
          onClick={onZoomReset}
          title="ズームリセット"
        >
          {Math.round(zoom * 100)}%
        </button>
        <button className="p-1.5 rounded-md hover:bg-accent" onClick={onZoomIn} title="拡大">
          <ZoomIn className="w-4 h-4" />
        </button>
        <div className="w-px h-6 bg-border mx-2" />
        <button
          className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md hover:bg-accent"
          onClick={onCopy}
          title="クリップボードにコピー"
        >
          <Copy className="w-4 h-4" />
          コピー
        </button>
        <button
          className={cn(
            'flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md',
            isDirty
              ? 'bg-primary text-primary-foreground hover:bg-primary/90'
              : 'bg-primary/50 text-primary-foreground cursor-not-allowed',
          )}
          onClick={onSave}
          disabled={!isDirty}
          title="保存"
        >
          <Save className="w-4 h-4" />
          保存
        </button>
      </div>
    </div>
  );
}

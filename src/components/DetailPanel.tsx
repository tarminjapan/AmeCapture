import { useState, useRef, useEffect } from 'react';
import { convertFileSrc } from '@tauri-apps/api/core';
import type { WorkspaceItem } from '@/types';
import { X, Star, Copy, FolderOpen, Trash2, Edit3, Check } from 'lucide-react';
import { getTypeLabel } from '@/lib/mediaTypeConfig';
import { TagInput } from '@/components/TagInput';
import { useEditorStore } from '@/stores/editorStore';

interface DetailPanelProps {
  item: WorkspaceItem;
  onClose: () => void;
  onToggleFavorite: (id: string, isFavorite: boolean) => void;
  onDelete: (id: string) => void;
  onRename: (id: string, title: string) => void;
  onShowInFolder: (id: string) => void;
  onCopyToClipboard: (id: string) => void;
  onOpenEditor: () => void;
}

function DetailThumbnail({ item }: { item: WorkspaceItem }) {
  const [imgError, setImgError] = useState(false);

  // item が変更されたらエラー状態をリセット
  useEffect(() => {
    setImgError(false);
  }, [item.id]);

  if (imgError || !item.thumbnailPath) {
    return (
      <div className="aspect-video rounded-md bg-muted flex items-center justify-center">
        <span className="text-xs text-muted-foreground">プレビューなし</span>
      </div>
    );
  }

  const src = convertFileSrc(item.thumbnailPath);

  return (
    <div className="aspect-video rounded-md bg-muted overflow-hidden">
      <img
        src={src}
        alt={item.title}
        className="w-full h-full object-cover"
        onError={(e) => {
          console.warn(`Detail thumbnail load failed`);
          console.debug(`  item.id: ${item.id}`);
          console.debug(`  item.title: ${item.title}`);
          console.debug(`  thumbnailPath: ${item.thumbnailPath}`);
          console.debug(`  converted src: ${src}`);
          console.debug(`  event type: ${e.type}`);
          setImgError(true);
        }}
      />
    </div>
  );
}

export function DetailPanel({
  item,
  onClose,
  onToggleFavorite,
  onDelete,
  onRename,
  onShowInFolder,
  onCopyToClipboard,
  onOpenEditor,
}: DetailPanelProps) {
  const [isEditing, setIsEditing] = useState(false);
  const [editTitle, setEditTitle] = useState(item.title);
  const inputRef = useRef<HTMLInputElement>(null);
  const setEditingItem = useEditorStore((s) => s.setEditingItem);

  useEffect(() => {
    setEditTitle(item.title);
    setIsEditing(false);
  }, [item.id, item.title]);

  useEffect(() => {
    if (isEditing && inputRef.current) {
      inputRef.current.focus();
      inputRef.current.select();
    }
  }, [isEditing]);

  const handleTitleSubmit = () => {
    const trimmed = editTitle.trim();
    if (trimmed && trimmed !== item.title) {
      onRename(item.id, trimmed);
    } else {
      setEditTitle(item.title);
    }
    setIsEditing(false);
  };

  const handleTitleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleTitleSubmit();
    } else if (e.key === 'Escape') {
      setEditTitle(item.title);
      setIsEditing(false);
    }
  };

  return (
    <div className="w-72 border-l border-border bg-card p-4 space-y-4 overflow-auto">
      {/* Header */}
      <div className="flex items-center justify-between gap-2">
        {isEditing ? (
          <div className="flex items-center gap-1 flex-1 min-w-0">
            <input
              ref={inputRef}
              className="flex-1 min-w-0 text-sm font-semibold bg-background border border-border rounded px-2 py-1 outline-none focus:border-primary"
              value={editTitle}
              onChange={(e) => setEditTitle(e.target.value)}
              onBlur={handleTitleSubmit}
              onKeyDown={handleTitleKeyDown}
            />
            <button
              className="p-1 rounded hover:bg-accent shrink-0"
              onMouseDown={(e) => {
                e.preventDefault();
                handleTitleSubmit();
              }}
            >
              <Check className="w-3.5 h-3.5" />
            </button>
          </div>
        ) : (
          <h3
            className="font-semibold text-sm truncate cursor-pointer hover:text-primary"
            onDoubleClick={() => setIsEditing(true)}
            title="ダブルクリックでタイトルを編集"
          >
            {item.title}
          </h3>
        )}
        <button className="p-1 rounded hover:bg-accent shrink-0" onClick={onClose}>
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Preview */}
      <DetailThumbnail item={item} />

      {/* Info */}
      <div className="space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">種別</span>
          <span>{getTypeLabel(item.type)}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">撮影日時</span>
          <span>{new Date(item.createdAt).toLocaleString('ja-JP')}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">更新日時</span>
          <span>{new Date(item.updatedAt).toLocaleString('ja-JP')}</span>
        </div>
        <div>
          <span className="text-muted-foreground text-xs">保存先</span>
          <p className="text-xs break-all mt-0.5">{item.currentPath}</p>
        </div>
      </div>

      {/* Tags */}
      <div>
        <span className="text-muted-foreground text-xs">タグ</span>
        <div className="mt-1">
          <TagInput itemId={item.id} />
        </div>
      </div>

      {/* Actions */}
      <div className="space-y-2">
        <button
          className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent"
          onClick={() => {
            if (item.type === 'image') {
              setEditingItem(item.id);
              onOpenEditor();
            }
          }}
        >
          <Edit3 className="w-4 h-4" />
          編集
        </button>
        <button
          className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent disabled:opacity-50 disabled:cursor-not-allowed"
          onClick={() => onCopyToClipboard(item.id)}
          disabled={item.type !== 'image'}
          title={
            item.type !== 'image' ? '画像アイテムのみコピー可能です' : 'クリップボードにコピー'
          }
        >
          <Copy className="w-4 h-4" />
          クリップボードにコピー
        </button>
        <button
          className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent"
          onClick={() => onShowInFolder(item.id)}
        >
          <FolderOpen className="w-4 h-4" />
          保存先を開く
        </button>
        <button
          className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent"
          onClick={() => onToggleFavorite(item.id, !item.isFavorite)}
        >
          <Star className={`w-4 h-4 ${item.isFavorite ? 'fill-yellow-400 text-yellow-400' : ''}`} />
          {item.isFavorite ? 'お気に入り解除' : 'お気に入り'}
        </button>
        <button
          className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent text-destructive"
          onClick={() => onDelete(item.id)}
        >
          <Trash2 className="w-4 h-4" />
          削除
        </button>
      </div>
    </div>
  );
}

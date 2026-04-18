import { useState, useEffect } from 'react';
import { convertFileSrc } from '@tauri-apps/api/core';
import type { WorkspaceItem } from '@/types';
import { Star, StarOff, Trash2, ImageIcon, Film } from 'lucide-react';
import { MEDIA_TYPE_CONFIG } from '@/lib/mediaTypeConfig';
import { useTagStore } from '@/stores/tagStore';
import { useEditorStore } from '@/stores/editorStore';

interface ThumbnailGridProps {
  items: WorkspaceItem[];
  selectedIds: string[];
  onSelect: (ids: string[]) => void;
  onToggleFavorite: (id: string, isFavorite: boolean) => void;
  onDelete: (id: string) => void;
  onOpenEditor: () => void;
}

function ThumbnailImage({ item }: { item: WorkspaceItem }) {
  const [imgError, setImgError] = useState(false);

  useEffect(() => {
    setImgError(false);
  }, [item.id, item.thumbnailPath]);

  if (imgError || !item.thumbnailPath) {
    return (
      <div className="flex flex-col items-center gap-1 text-muted-foreground">
        {item.type === 'video' ? <Film className="w-8 h-8" /> : <ImageIcon className="w-8 h-8" />}
        <span className="text-xs">No preview</span>
      </div>
    );
  }

  const src = convertFileSrc(item.thumbnailPath);

  return (
    <img
      src={src}
      alt={item.title}
      className="w-full h-full object-cover"
      loading="lazy"
      onError={(e) => {
        console.warn(`Thumbnail load failed`);
        console.debug(`  item.id: ${item.id}`);
        console.debug(`  item.title: ${item.title}`);
        console.debug(`  thumbnailPath: ${item.thumbnailPath}`);
        console.debug(`  converted src: ${src}`);
        console.debug(`  event type: ${e.type}`);
        setImgError(true);
      }}
    />
  );
}

export function ThumbnailGrid({
  items,
  selectedIds,
  onSelect,
  onToggleFavorite,
  onDelete,
  onOpenEditor,
}: ThumbnailGridProps) {
  const itemTags = useTagStore((s) => s.itemTags);
  const setEditingItem = useEditorStore((s) => s.setEditingItem);

  const handleClick = (id: string, e: React.MouseEvent) => {
    if (e.ctrlKey || e.metaKey) {
      const newIds = selectedIds.includes(id)
        ? selectedIds.filter((sid) => sid !== id)
        : [...selectedIds, id];
      onSelect(newIds);
    } else {
      onSelect([id]);
    }
  };

  const handleDoubleClick = (item: WorkspaceItem) => {
    if (item.type === 'image') {
      setEditingItem(item.id);
      onOpenEditor();
    }
  };

  return (
    <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-3">
      {items.map((item) => (
        <div
          key={item.id}
          className={`group relative rounded-lg border overflow-hidden cursor-pointer transition-colors ${
            selectedIds.includes(item.id)
              ? 'border-primary ring-2 ring-primary/30'
              : 'border-border hover:border-primary/50'
          }`}
          onClick={(e) => handleClick(item.id, e)}
          onDoubleClick={() => handleDoubleClick(item)}
        >
          {/* Thumbnail */}
          <div className="aspect-video bg-muted flex items-center justify-center relative">
            <ThumbnailImage item={item} />

            {/* Type badge */}
            <span
              className={`absolute top-1 left-1 px-1.5 py-0.5 text-[10px] font-medium rounded ${MEDIA_TYPE_CONFIG[item.type].badgeClass}`}
            >
              {MEDIA_TYPE_CONFIG[item.type].label}
            </span>

            {/* Favorite indicator - always visible */}
            {item.isFavorite && (
              <span className="absolute bottom-1 left-1 p-0.5 rounded bg-background/80">
                <Star className="w-3 h-3 fill-yellow-400 text-yellow-400" />
              </span>
            )}
          </div>

          {/* Info */}
          <div className="p-2">
            <p className="text-sm font-medium truncate">{item.title}</p>
            <p className="text-xs text-muted-foreground">
              {new Date(item.createdAt).toLocaleString('ja-JP')}
            </p>
            {(itemTags[item.id] ?? []).length > 0 && (
              <div className="flex flex-wrap gap-0.5 mt-1">
                {(itemTags[item.id] ?? []).map((tag) => (
                  <span
                    key={tag.id}
                    className="px-1.5 py-0 text-[10px] rounded-full bg-primary/10 text-primary"
                  >
                    {tag.name}
                  </span>
                ))}
              </div>
            )}
          </div>

          {/* Actions overlay */}
          <div className="absolute top-1 right-1 opacity-0 group-hover:opacity-100 transition-opacity flex gap-1">
            <button
              className="p-1 rounded bg-background/80 hover:bg-background"
              onClick={(e) => {
                e.stopPropagation();
                onToggleFavorite(item.id, !item.isFavorite);
              }}
            >
              {item.isFavorite ? (
                <Star className="w-3 h-3 fill-yellow-400 text-yellow-400" />
              ) : (
                <StarOff className="w-3 h-3" />
              )}
            </button>
            <button
              className="p-1 rounded bg-background/80 hover:bg-background"
              onClick={(e) => {
                e.stopPropagation();
                onDelete(item.id);
              }}
            >
              <Trash2 className="w-3 h-3" />
            </button>
          </div>
        </div>
      ))}
    </div>
  );
}

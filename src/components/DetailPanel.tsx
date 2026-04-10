import type { WorkspaceItem } from "@/types";
import { X, Star, Copy, FolderOpen, Trash2, Edit3 } from "lucide-react";

interface DetailPanelProps {
  item: WorkspaceItem;
  onClose: () => void;
}

export function DetailPanel({ item, onClose }: DetailPanelProps) {
  return (
    <div className="w-72 border-l border-border bg-card p-4 space-y-4 overflow-auto">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h3 className="font-semibold text-sm truncate">{item.title}</h3>
        <button
          className="p-1 rounded hover:bg-accent"
          onClick={onClose}
        >
          <X className="w-4 h-4" />
        </button>
      </div>

      {/* Preview */}
      <div className="aspect-video rounded-md bg-muted overflow-hidden">
        {item.thumbnailPath ? (
          <img
            src={item.thumbnailPath}
            alt={item.title}
            className="w-full h-full object-cover"
          />
        ) : (
          <div className="flex items-center justify-center h-full text-xs text-muted-foreground">
            No preview
          </div>
        )}
      </div>

      {/* Info */}
      <div className="space-y-2 text-sm">
        <div className="flex justify-between">
          <span className="text-muted-foreground">種別</span>
          <span>{item.type === "image" ? "画像" : "動画"}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">撮影日時</span>
          <span>{new Date(item.createdAt).toLocaleString("ja-JP")}</span>
        </div>
        <div className="flex justify-between">
          <span className="text-muted-foreground">更新日時</span>
          <span>{new Date(item.updatedAt).toLocaleString("ja-JP")}</span>
        </div>
      </div>

      {/* Actions */}
      <div className="space-y-2">
        <button className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent">
          <Edit3 className="w-4 h-4" />
          編集
        </button>
        <button className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent">
          <Copy className="w-4 h-4" />
          クリップボードにコピー
        </button>
        <button className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent">
          <FolderOpen className="w-4 h-4" />
          保存先を開く
        </button>
        <button className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent">
          <Star className="w-4 h-4" />
          {item.isFavorite ? "お気に入り解除" : "お気に入り"}
        </button>
        <button className="flex items-center gap-2 w-full px-3 py-2 text-sm rounded-md hover:bg-accent text-destructive">
          <Trash2 className="w-4 h-4" />
          削除
        </button>
      </div>
    </div>
  );
}
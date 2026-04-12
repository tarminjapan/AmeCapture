import { useEffect, useMemo, useState } from 'react';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import { useWorkspace } from '@/hooks/useWorkspace';
import { SearchBar } from '@/components/SearchBar';
import { ThumbnailGrid } from '@/components/ThumbnailGrid';
import { DetailPanel } from '@/components/DetailPanel';
import { Toolbar } from '@/components/Toolbar';
import { ImageOff, Settings } from 'lucide-react';
import { getTypeLabel } from '@/lib/mediaTypeConfig';

export default function WorkspacePage() {
  const items = useWorkspaceStore((s) => s.items);
  const selectedItemIds = useWorkspaceStore((s) => s.selectedItemIds);
  const searchQuery = useWorkspaceStore((s) => s.searchQuery);
  const sortBy = useWorkspaceStore((s) => s.sortBy);
  const sortOrder = useWorkspaceStore((s) => s.sortOrder);
  const isLoading = useWorkspaceStore((s) => s.isLoading);
  const setSelectedItemIds = useWorkspaceStore((s) => s.setSelectedItemIds);

  const { loadItems, deleteItem, toggleFavorite, renameItem, showInFolder, copyImageToClipboard } =
    useWorkspace();
  const [showDetail, setShowDetail] = useState(false);

  useEffect(() => {
    loadItems();
  }, []);

  const filteredItems = useMemo(() => {
    let result = [...items];

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        (item) =>
          item.title.toLowerCase().includes(q) ||
          item.type.toLowerCase().includes(q) ||
          getTypeLabel(item.type).includes(q),
      );
    }

    result.sort((a, b) => {
      let cmp = 0;
      if (sortBy === 'title') {
        cmp = a.title.localeCompare(b.title);
      } else if (sortBy === 'updatedAt') {
        cmp = a.updatedAt.localeCompare(b.updatedAt);
      } else {
        cmp = a.createdAt.localeCompare(b.createdAt);
      }
      return sortOrder === 'asc' ? cmp : -cmp;
    });

    return result;
  }, [items, searchQuery, sortBy, sortOrder]);

  const selectedItem = useMemo(() => {
    if (selectedItemIds.length === 1) {
      return items.find((item) => item.id === selectedItemIds[0]) ?? null;
    }
    return null;
  }, [items, selectedItemIds]);

  const handleSelect = (ids: string[]) => {
    setSelectedItemIds(ids);
    setShowDetail(ids.length === 1);
  };

  const handleCaptureFullscreen = () => {
    // TODO: implement fullscreen capture
  };

  const handleCaptureRegion = (_region: {
    x: number;
    y: number;
    width: number;
    height: number;
  }) => {
    // TODO: implement region capture
  };

  const handleCaptureWindow = () => {
    // TODO: implement window capture
  };

  return (
    <div className="flex h-full w-full flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b border-border px-4 py-2">
        <Toolbar
          onCaptureFullscreen={handleCaptureFullscreen}
          onCaptureRegion={handleCaptureRegion}
          onCaptureWindow={handleCaptureWindow}
        />
        <div className="flex items-center gap-2">
          <SearchBar />
          <button className="p-1.5 rounded-md hover:bg-accent" title="設定">
            <Settings className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="flex flex-1 overflow-hidden">
        {/* Main grid area */}
        <div className="flex-1 overflow-auto p-4">
          {isLoading ? (
            <div className="flex h-full items-center justify-center">
              <div className="flex flex-col items-center gap-2 text-muted-foreground">
                <div className="h-6 w-6 animate-spin rounded-full border-2 border-primary border-t-transparent" />
                <p className="text-sm">読み込み中...</p>
              </div>
            </div>
          ) : filteredItems.length === 0 ? (
            <div className="flex h-full items-center justify-center">
              <div className="flex flex-col items-center gap-3 text-muted-foreground">
                <ImageOff className="w-12 h-12" />
                {items.length === 0 ? (
                  <>
                    <p className="text-lg font-medium">まだアイテムがありません</p>
                    <p className="text-sm">キャプチャボタンから画像または動画を撮影してください</p>
                  </>
                ) : (
                  <>
                    <p className="text-lg font-medium">検索結果が見つかりません</p>
                    <p className="text-sm">検索条件を変更してお試しください</p>
                  </>
                )}
              </div>
            </div>
          ) : (
            <ThumbnailGrid
              items={filteredItems}
              selectedIds={selectedItemIds}
              onSelect={handleSelect}
              onToggleFavorite={toggleFavorite}
              onDelete={deleteItem}
            />
          )}
        </div>

        {/* Detail panel */}
        {showDetail && selectedItem && (
          <DetailPanel
            item={selectedItem}
            onClose={() => setShowDetail(false)}
            onToggleFavorite={toggleFavorite}
            onDelete={deleteItem}
            onRename={renameItem}
            onShowInFolder={showInFolder}
            onCopyToClipboard={copyImageToClipboard}
          />
        )}
      </div>
    </div>
  );
}

import { useEffect, useMemo, useState } from 'react';
import { useWorkspaceStore } from '@/stores/workspaceStore';
import { useTagStore } from '@/stores/tagStore';
import { useWorkspace } from '@/hooks/useWorkspace';
import { useCapture } from '@/hooks/useCapture';
import { SearchBar } from '@/components/SearchBar';
import { ThumbnailGrid } from '@/components/ThumbnailGrid';
import { DetailPanel } from '@/components/DetailPanel';
import { Toolbar } from '@/components/Toolbar';
import { TagFilterBar } from '@/components/TagFilterBar';
import { RegionCaptureOverlay } from '@/components/RegionCaptureOverlay';
import { WindowCaptureOverlay } from '@/components/WindowCaptureOverlay';
import { CaptureProgressOverlay } from '@/components/CaptureProgressOverlay';
import { useGlobalShortcut } from '@/hooks/useGlobalShortcut';
import type { CaptureAction } from '@/hooks/useGlobalShortcut';
import { useTrayCapture } from '@/hooks/useTrayCapture';
import { ImageOff, Settings, Star } from 'lucide-react';
import { getTypeLabel } from '@/lib/mediaTypeConfig';
import type { CaptureRegion, RegionCaptureInfo, WindowInfo } from '@/types';

interface WorkspacePageProps {
  onNavigateToEditor: () => void;
}

export default function WorkspacePage({ onNavigateToEditor }: WorkspacePageProps) {
  const items = useWorkspaceStore((s) => s.items);
  const selectedItemIds = useWorkspaceStore((s) => s.selectedItemIds);
  const searchQuery = useWorkspaceStore((s) => s.searchQuery);
  const sortBy = useWorkspaceStore((s) => s.sortBy);
  const sortOrder = useWorkspaceStore((s) => s.sortOrder);
  const isLoading = useWorkspaceStore((s) => s.isLoading);
  const showFavoritesOnly = useWorkspaceStore((s) => s.showFavoritesOnly);
  const selectedTagIds = useWorkspaceStore((s) => s.selectedTagIds);
  const setSelectedItemIds = useWorkspaceStore((s) => s.setSelectedItemIds);
  const setShowFavoritesOnly = useWorkspaceStore((s) => s.setShowFavoritesOnly);
  const itemTags = useTagStore((s) => s.itemTags);

  const { loadItems, deleteItem, toggleFavorite, renameItem, showInFolder, copyImageToClipboard } =
    useWorkspace();
  const {
    captureFullscreen,
    captureWindow,
    prepareRegionCapture,
    finalizeRegionCapture,
    cancelRegionCapture,
    prepareWindowCapture,
  } = useCapture();
  const [showDetail, setShowDetail] = useState(false);
  const [regionCaptureInfo, setRegionCaptureInfo] = useState<RegionCaptureInfo | null>(null);
  const [windowCaptureList, setWindowCaptureList] = useState<WindowInfo[] | null>(null);
  const [isCapturing, setIsCapturing] = useState(false);

  const CAPTURE_PROGRESS_DELAY_MS = 50;

  const withCaptureProgress = async (
    action: () => Promise<void>,
    options?: { delay?: boolean },
  ) => {
    if (isCapturing) return;
    setIsCapturing(true);
    try {
      if (options?.delay) {
        await new Promise((resolve) => setTimeout(resolve, CAPTURE_PROGRESS_DELAY_MS));
      }
      await action();
    } finally {
      setIsCapturing(false);
    }
  };

  useEffect(() => {
    loadItems();
  }, [loadItems]);

  const handleCaptureFullscreen = () =>
    withCaptureProgress(
      async () => {
        await captureFullscreen();
      },
      { delay: true },
    );

  const handleCaptureRegion = () =>
    withCaptureProgress(async () => {
      const result = await prepareRegionCapture();
      if (result.success && result.data) {
        setRegionCaptureInfo(result.data);
      }
    });

  const handleRegionConfirm = (sourcePath: string, region: CaptureRegion) =>
    withCaptureProgress(
      async () => {
        setRegionCaptureInfo(null);
        await finalizeRegionCapture(sourcePath, region);
      },
      { delay: true },
    );

  const handleRegionCancel = async (sourcePath: string) => {
    setRegionCaptureInfo(null);
    await cancelRegionCapture(sourcePath);
  };

  const handleCaptureWindow = () =>
    withCaptureProgress(async () => {
      const result = await prepareWindowCapture();
      if (result.success && result.data) {
        setWindowCaptureList(result.data.windows);
      }
    });

  const handleWindowSelect = (hwnd: number) =>
    withCaptureProgress(
      async () => {
        setWindowCaptureList(null);
        await captureWindow(hwnd);
      },
      { delay: true },
    );

  const handleWindowCancel = () => {
    setWindowCaptureList(null);
  };

  useGlobalShortcut((action: CaptureAction) => {
    if (regionCaptureInfo || windowCaptureList || isCapturing) return;
    switch (action) {
      case 'fullscreen':
        handleCaptureFullscreen();
        break;
      case 'region':
        handleCaptureRegion();
        break;
      case 'window':
        handleCaptureWindow();
        break;
    }
  });

  useTrayCapture((action: CaptureAction) => {
    if (regionCaptureInfo || windowCaptureList || isCapturing) return;
    switch (action) {
      case 'fullscreen':
        handleCaptureFullscreen();
        break;
      case 'region':
        handleCaptureRegion();
        break;
      case 'window':
        handleCaptureWindow();
        break;
    }
  });

  const filteredItems = useMemo(() => {
    let result = [...items];

    if (showFavoritesOnly) {
      result = result.filter((item) => item.isFavorite);
    }

    if (selectedTagIds.length > 0) {
      result = result.filter((item) => {
        const itemTagIds = (itemTags[item.id] ?? []).map((t) => t.id);
        return selectedTagIds.some((tagId) => itemTagIds.includes(tagId));
      });
    }

    if (searchQuery.trim()) {
      const q = searchQuery.toLowerCase();
      result = result.filter(
        (item) =>
          (item.title ?? '').toLowerCase().includes(q) ||
          (item.type ?? '').toLowerCase().includes(q) ||
          getTypeLabel(item.type ?? 'image').includes(q),
      );
    }

    result.sort((a, b) => {
      let cmp: number;
      if (sortBy === 'title') {
        cmp = (a.title ?? '').localeCompare(b.title ?? '');
      } else if (sortBy === 'updatedAt') {
        cmp = (a.updatedAt ?? '').localeCompare(b.updatedAt ?? '');
      } else {
        cmp = (a.createdAt ?? '').localeCompare(b.createdAt ?? '');
      }
      return sortOrder === 'asc' ? cmp : -cmp;
    });

    return result;
  }, [items, searchQuery, sortBy, sortOrder, showFavoritesOnly, selectedTagIds, itemTags]);

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

  if (regionCaptureInfo) {
    return (
      <RegionCaptureOverlay
        captureInfo={regionCaptureInfo}
        onConfirm={handleRegionConfirm}
        onCancel={handleRegionCancel}
      />
    );
  }

  if (windowCaptureList) {
    return (
      <WindowCaptureOverlay
        windows={windowCaptureList}
        onSelect={handleWindowSelect}
        onCancel={handleWindowCancel}
      />
    );
  }

  return (
    <div className="flex h-full w-full flex-col">
      {isCapturing && <CaptureProgressOverlay />}
      {/* Header */}
      <div className="flex items-center justify-between border-b border-border px-4 py-2">
        <Toolbar
          onCaptureFullscreen={handleCaptureFullscreen}
          onCaptureRegion={handleCaptureRegion}
          onCaptureWindow={handleCaptureWindow}
        />
        <div className="flex items-center gap-2">
          <button
            className={`p-1.5 rounded-md hover:bg-accent ${showFavoritesOnly ? 'bg-accent text-yellow-400' : ''}`}
            title={showFavoritesOnly ? 'すべて表示' : 'お気に入りのみ表示'}
            aria-pressed={showFavoritesOnly}
            onClick={() => setShowFavoritesOnly(!showFavoritesOnly)}
          >
            <Star
              className={`w-4 h-4 ${showFavoritesOnly ? 'fill-yellow-400 text-yellow-400' : ''}`}
            />
          </button>
          <SearchBar />
          <button className="p-1.5 rounded-md hover:bg-accent" title="設定">
            <Settings className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Tag filter bar */}
      <TagFilterBar />

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
              onOpenEditor={onNavigateToEditor}
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
            onOpenEditor={onNavigateToEditor}
          />
        )}
      </div>
    </div>
  );
}

import { useState } from 'react';
import { AppWindow, X } from 'lucide-react';
import type { WindowInfo } from '@/types';

interface WindowCaptureOverlayProps {
  windows: WindowInfo[];
  onSelect: (hwnd: number) => void;
  onCancel: () => void;
}

export function WindowCaptureOverlay({ windows, onSelect, onCancel }: WindowCaptureOverlayProps) {
  const [selectedHwnd, setSelectedHwnd] = useState<number | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  const filteredWindows = windows.filter(
    (w) =>
      w.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      w.className.toLowerCase().includes(searchQuery.toLowerCase()),
  );

  const handleConfirm = () => {
    if (selectedHwnd !== null) {
      onSelect(selectedHwnd);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Escape') {
      onCancel();
    } else if (e.key === 'Enter' && selectedHwnd !== null) {
      handleConfirm();
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/70"
      onKeyDown={handleKeyDown}
    >
      <div className="relative flex w-full max-w-2xl flex-col rounded-lg border border-border bg-background shadow-2xl">
        <div className="flex items-center justify-between border-b border-border px-4 py-3">
          <div className="flex items-center gap-2">
            <AppWindow className="h-5 w-5 text-primary" />
            <h2 className="text-lg font-semibold">ウィンドウを選択</h2>
          </div>
          <button
            className="rounded-md p-1 hover:bg-accent"
            onClick={onCancel}
            title="キャンセル (Esc)"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <div className="border-b border-border px-4 py-2">
          <input
            type="text"
            className="w-full rounded-md border border-input bg-transparent px-3 py-1.5 text-sm outline-none focus:ring-2 focus:ring-ring"
            placeholder="ウィンドウを検索..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            autoFocus
          />
        </div>

        <div className="max-h-96 overflow-y-auto p-2">
          {filteredWindows.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 text-muted-foreground">
              <p className="text-sm">該当するウィンドウが見つかりません</p>
            </div>
          ) : (
            filteredWindows.map((w) => (
              <button
                key={w.hwnd}
                className={`flex w-full items-center gap-3 rounded-md px-3 py-2 text-left text-sm transition-colors ${
                  selectedHwnd === w.hwnd ? 'bg-primary text-primary-foreground' : 'hover:bg-accent'
                }`}
                onClick={() => setSelectedHwnd(w.hwnd)}
                onDoubleClick={() => onSelect(w.hwnd)}
              >
                <AppWindow className="h-4 w-4 shrink-0" />
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{w.title}</p>
                  <p
                    className={`truncate text-xs ${
                      selectedHwnd === w.hwnd
                        ? 'text-primary-foreground/70'
                        : 'text-muted-foreground'
                    }`}
                  >
                    {w.className} &middot; {w.bounds[2]}x{w.bounds[3]}
                  </p>
                </div>
              </button>
            ))
          )}
        </div>

        <div className="flex items-center justify-end gap-2 border-t border-border px-4 py-3">
          <button className="rounded-md px-4 py-1.5 text-sm hover:bg-accent" onClick={onCancel}>
            キャンセル
          </button>
          <button
            className="rounded-md bg-primary px-4 py-1.5 text-sm font-medium text-primary-foreground hover:bg-primary/90 disabled:opacity-50"
            onClick={handleConfirm}
            disabled={selectedHwnd === null}
          >
            キャプチャ
          </button>
        </div>
      </div>
    </div>
  );
}

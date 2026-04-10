import { Camera, Monitor, AppWindow } from "lucide-react";

interface ToolbarProps {
  onCaptureFullscreen: () => void;
  onCaptureRegion: (region: { x: number; y: number; width: number; height: number }) => void;
  onCaptureWindow: () => void;
}

export function Toolbar({
  onCaptureFullscreen,
  onCaptureRegion,
  onCaptureWindow,
}: ToolbarProps) {
  return (
    <div className="flex items-center gap-1">
      <button
        className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md bg-primary text-primary-foreground hover:bg-primary/90"
        onClick={() => onCaptureRegion({ x: 0, y: 0, width: 0, height: 0 })}
        title="領域キャプチャ"
      >
        <Camera className="w-4 h-4" />
        領域キャプチャ
      </button>
      <button
        className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md hover:bg-accent"
        onClick={onCaptureFullscreen}
        title="全画面キャプチャ"
      >
        <Monitor className="w-4 h-4" />
        全画面
      </button>
      <button
        className="flex items-center gap-1.5 px-3 py-1.5 text-sm rounded-md hover:bg-accent"
        onClick={onCaptureWindow}
        title="ウィンドウキャプチャ"
      >
        <AppWindow className="w-4 h-4" />
        ウィンドウ
      </button>
    </div>
  );
}
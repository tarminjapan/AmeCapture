import { useCallback, useRef, useState } from 'react';
import { convertFileSrc } from '@tauri-apps/api/core';
import { cn } from '@/lib/utils';

interface EditorCanvasProps {
  imagePath: string;
  zoom: number;
  panX: number;
  panY: number;
  onZoomChange: (zoom: number) => void;
  onPanChange: (x: number, y: number) => void;
  className?: string;
}

export function EditorCanvas({
  imagePath,
  zoom,
  panX,
  panY,
  onZoomChange,
  onPanChange,
  className,
}: EditorCanvasProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isPanning, setIsPanning] = useState(false);
  const [panStart, setPanStart] = useState({ x: 0, y: 0 });

  const handleWheel = useCallback(
    (e: React.WheelEvent) => {
      e.preventDefault();
      const delta = e.deltaY > 0 ? -0.1 : 0.1;
      onZoomChange(zoom + delta);
    },
    [zoom, onZoomChange],
  );

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (e.button === 0 || e.button === 1) {
        setIsPanning(true);
        setPanStart({ x: e.clientX - panX, y: e.clientY - panY });
      }
    },
    [panX, panY],
  );

  const handleMouseMove = useCallback(
    (e: React.MouseEvent) => {
      if (!isPanning) return;
      onPanChange(e.clientX - panStart.x, e.clientY - panStart.y);
    },
    [isPanning, panStart, onPanChange],
  );

  const handleMouseUp = useCallback(() => {
    setIsPanning(false);
  }, []);

  const imageUrl = convertFileSrc(imagePath);

  return (
    <div
      ref={containerRef}
      className={cn('relative overflow-hidden bg-muted/50', className)}
      onWheel={handleWheel}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
      style={{ cursor: isPanning ? 'grabbing' : 'grab' }}
    >
      <div
        style={{
          transform: `translate(${panX}px, ${panY}px) scale(${zoom})`,
          transformOrigin: '0 0',
          position: 'absolute',
          top: 0,
          left: 0,
        }}
      >
        <img
          src={imageUrl}
          alt="edit target"
          draggable={false}
          className="block max-w-none"
          style={{ imageRendering: zoom > 2 ? 'pixelated' : 'auto' }}
        />
      </div>
    </div>
  );
}

import { useCallback, useEffect, useRef, useState } from 'react';
import type { CaptureRegion, RegionCaptureInfo } from '@/types';

interface RegionCaptureOverlayProps {
  captureInfo: RegionCaptureInfo;
  onConfirm: (sourcePath: string, region: CaptureRegion) => void;
  onCancel: (sourcePath: string) => void;
}

interface SelectionRect {
  left: number;
  top: number;
  width: number;
  height: number;
}

interface ImageBounds {
  x: number;
  y: number;
  width: number;
  height: number;
}

export function RegionCaptureOverlay({
  captureInfo,
  onConfirm,
  onCancel,
}: RegionCaptureOverlayProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isSelecting, setIsSelecting] = useState(false);
  const [startPoint, setStartPoint] = useState<{ x: number; y: number } | null>(null);
  const [currentPoint, setCurrentPoint] = useState<{ x: number; y: number } | null>(null);
  const [mousePos, setMousePos] = useState<{ x: number; y: number } | null>(null);
  const [imageBounds, setImageBounds] = useState<ImageBounds | null>(null);

  const computeImageBounds = useCallback(() => {
    if (!containerRef.current) return;
    const container = containerRef.current;
    const containerWidth = container.clientWidth;
    const containerHeight = container.clientHeight;
    const imgAspect = captureInfo.screenWidth / captureInfo.screenHeight;
    const containerAspect = containerWidth / containerHeight;

    let renderedWidth: number;
    let renderedHeight: number;
    let offsetX: number;
    let offsetY: number;

    if (imgAspect > containerAspect) {
      renderedWidth = containerWidth;
      renderedHeight = containerWidth / imgAspect;
      offsetX = 0;
      offsetY = (containerHeight - renderedHeight) / 2;
    } else {
      renderedHeight = containerHeight;
      renderedWidth = containerHeight * imgAspect;
      offsetX = (containerWidth - renderedWidth) / 2;
      offsetY = 0;
    }

    setImageBounds({
      x: offsetX,
      y: offsetY,
      width: renderedWidth,
      height: renderedHeight,
    });
  }, [captureInfo.screenWidth, captureInfo.screenHeight]);

  useEffect(() => {
    computeImageBounds();
    const handleResize = () => computeImageBounds();
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, [computeImageBounds]);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onCancel(captureInfo.tempPath);
      }
      if (e.key === 'Enter' && selection) {
        const region = mapSelectionToScreen(selection);
        if (region.width > 0 && region.height > 0) {
          onConfirm(captureInfo.tempPath, region);
        }
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  });

  const mapClientToScreen = useCallback(
    (clientX: number, clientY: number): { x: number; y: number } => {
      if (!imageBounds) return { x: 0, y: 0 };
      const relX = clientX - imageBounds.x;
      const relY = clientY - imageBounds.y;
      const scaleX = captureInfo.screenWidth / imageBounds.width;
      const scaleY = captureInfo.screenHeight / imageBounds.height;
      return {
        x: Math.max(0, Math.min(Math.round(relX * scaleX), captureInfo.screenWidth)),
        y: Math.max(0, Math.min(Math.round(relY * scaleY), captureInfo.screenHeight)),
      };
    },
    [imageBounds, captureInfo.screenWidth, captureInfo.screenHeight],
  );

  const mapSelectionToScreen = useCallback(
    (sel: SelectionRect): CaptureRegion => {
      const topLeft = mapClientToScreen(sel.left, sel.top);
      const bottomRight = mapClientToScreen(sel.left + sel.width, sel.top + sel.height);
      return {
        x: topLeft.x,
        y: topLeft.y,
        width: bottomRight.x - topLeft.x,
        height: bottomRight.y - topLeft.y,
      };
    },
    [mapClientToScreen],
  );

  const getSelection = useCallback((): SelectionRect | null => {
    if (!startPoint || !currentPoint) return null;
    const left = Math.min(startPoint.x, currentPoint.x);
    const top = Math.min(startPoint.y, currentPoint.y);
    const width = Math.abs(currentPoint.x - startPoint.x);
    const height = Math.abs(currentPoint.y - startPoint.y);
    if (width < 3 || height < 3) return null;
    return { left, top, width, height };
  }, [startPoint, currentPoint]);

  const selection = getSelection();

  const handleMouseDown = (e: React.MouseEvent) => {
    if (e.button !== 0) return;
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect) return;
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    setStartPoint({ x, y });
    setCurrentPoint({ x, y });
    setIsSelecting(true);
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    const rect = containerRef.current?.getBoundingClientRect();
    if (!rect) return;
    const x = e.clientX - rect.left;
    const y = e.clientY - rect.top;
    setMousePos({ x, y });
    if (isSelecting) {
      setCurrentPoint({ x, y });
    }
  };

  const handleMouseUp = () => {
    if (!isSelecting) return;
    setIsSelecting(false);
    if (selection) {
      const region = mapSelectionToScreen(selection);
      if (region.width > 0 && region.height > 0) {
        onConfirm(captureInfo.tempPath, region);
      }
    }
  };

  const screenDim = selection
    ? (() => {
        const r = mapSelectionToScreen(selection);
        return `${r.width} × ${r.height}`;
      })()
    : null;

  return (
    <div
      ref={containerRef}
      className="fixed inset-0 z-50 cursor-crosshair bg-black select-none"
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={() => setMousePos(null)}
    >
      <img
        src={captureInfo.tempPath}
        className="absolute w-full h-full object-contain pointer-events-none"
        draggable={false}
        alt=""
      />

      <div className="absolute inset-0 bg-black/40 pointer-events-none" />

      {selection && (
        <div
          className="absolute border-2 border-white/90 pointer-events-none"
          style={{
            left: selection.left,
            top: selection.top,
            width: selection.width,
            height: selection.height,
            boxShadow: '0 0 0 9999px rgba(0, 0, 0, 0.4)',
            zIndex: 10,
          }}
        >
          <div className="absolute inset-0 overflow-hidden">
            <img
              src={captureInfo.tempPath}
              className="absolute w-full h-full object-contain pointer-events-none"
              draggable={false}
              alt=""
              style={{
                left: imageBounds ? -selection.left + imageBounds.x : 0,
                top: imageBounds ? -selection.top + imageBounds.y : 0,
                width: imageBounds?.width,
                height: imageBounds?.height,
              }}
            />
          </div>
        </div>
      )}

      {!selection && mousePos && (
        <>
          <div
            className="absolute w-px h-full bg-white/40 pointer-events-none"
            style={{ left: mousePos.x }}
          />
          <div
            className="absolute h-px w-full bg-white/40 pointer-events-none"
            style={{ top: mousePos.y }}
          />
        </>
      )}

      {screenDim && selection && (
        <div
          className="absolute text-white text-xs bg-black/70 px-2 py-1 rounded pointer-events-none whitespace-nowrap"
          style={{
            left: selection.left,
            top: selection.top + selection.height + 4,
            zIndex: 20,
          }}
        >
          {screenDim}
        </div>
      )}

      <div className="absolute bottom-6 left-1/2 -translate-x-1/2 text-white/70 text-sm pointer-events-none z-30">
        ドラッグで範囲選択 · Enter で確定 · Esc でキャンセル
      </div>
    </div>
  );
}

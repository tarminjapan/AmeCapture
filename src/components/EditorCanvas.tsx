import { useCallback, useEffect, useRef, useState } from 'react';
import { convertFileSrc } from '@tauri-apps/api/core';
import { cn } from '@/lib/utils';
import type { EditorAnnotation, EditorTool } from '@/types';

interface EditorCanvasProps {
  imagePath: string;
  zoom: number;
  panX: number;
  panY: number;
  activeTool: EditorTool;
  strokeColor: string;
  strokeWidth: number;
  annotations: EditorAnnotation[];
  onZoomChange: (zoom: number) => void;
  onPanChange: (x: number, y: number) => void;
  onAddAnnotation: (annotation: EditorAnnotation) => void;
  className?: string;
}

function generateId(): string {
  return Math.random().toString(36).substring(2) + Date.now().toString(36);
}

function getArrowheadPoints(
  startX: number,
  startY: number,
  endX: number,
  endY: number,
  sw: number,
): string {
  const dx = endX - startX;
  const dy = endY - startY;
  const length = Math.sqrt(dx * dx + dy * dy);
  if (length === 0) return '';

  const angle = Math.atan2(dy, dx);
  const headLength = sw * 4;
  const headWidth = sw * 2;

  const baseX = endX - headLength * Math.cos(angle);
  const baseY = endY - headLength * Math.sin(angle);

  const leftX = baseX + headWidth * Math.cos(angle - Math.PI / 2);
  const leftY = baseY + headWidth * Math.sin(angle - Math.PI / 2);

  const rightX = baseX + headWidth * Math.cos(angle + Math.PI / 2);
  const rightY = baseY + headWidth * Math.sin(angle + Math.PI / 2);

  return `${endX},${endY} ${leftX},${leftY} ${rightX},${rightY}`;
}

export function EditorCanvas({
  imagePath,
  zoom,
  panX,
  panY,
  activeTool,
  strokeColor,
  strokeWidth,
  annotations,
  onZoomChange,
  onPanChange,
  onAddAnnotation,
  className,
}: EditorCanvasProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const [isPanning, setIsPanning] = useState(false);
  const [panStart, setPanStart] = useState({ x: 0, y: 0 });
  const [imgSize, setImgSize] = useState({ width: 0, height: 0 });
  const [drawing, setDrawing] = useState<{
    startX: number;
    startY: number;
    endX: number;
    endY: number;
  } | null>(null);

  const zoomRef = useRef(zoom);
  useEffect(() => {
    zoomRef.current = zoom;
  }, [zoom]);

  const getImageCoords = useCallback(
    (e: React.MouseEvent): { x: number; y: number } | null => {
      if (!containerRef.current) return null;
      const rect = containerRef.current.getBoundingClientRect();
      const x = (e.clientX - rect.left - panX) / zoom;
      const y = (e.clientY - rect.top - panY) / zoom;
      return { x, y };
    },
    [panX, panY, zoom],
  );

  const handleWheel = useCallback(
    (e: WheelEvent) => {
      e.preventDefault();
      const delta = e.deltaY > 0 ? -0.1 : 0.1;
      onZoomChange(zoomRef.current + delta);
    },
    [onZoomChange],
  );

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    el.addEventListener('wheel', handleWheel, { passive: false });
    return () => {
      el.removeEventListener('wheel', handleWheel);
    };
  }, [handleWheel]);

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (e.button === 1) {
        setIsPanning(true);
        setPanStart({ x: e.clientX - panX, y: e.clientY - panY });
        return;
      }
      if (e.button === 0 && activeTool === 'arrow') {
        const coords = getImageCoords(e);
        if (coords) {
          setDrawing({
            startX: coords.x,
            startY: coords.y,
            endX: coords.x,
            endY: coords.y,
          });
        }
      }
    },
    [panX, panY, activeTool, getImageCoords],
  );

  const handleMouseMove = useCallback(
    (e: React.MouseEvent) => {
      if (isPanning) {
        onPanChange(e.clientX - panStart.x, e.clientY - panStart.y);
        return;
      }
      if (drawing) {
        const coords = getImageCoords(e);
        if (coords) {
          setDrawing((prev) => (prev ? { ...prev, endX: coords.x, endY: coords.y } : null));
        }
      }
    },
    [isPanning, panStart, onPanChange, drawing, getImageCoords],
  );

  const handleMouseUp = useCallback(() => {
    if (drawing) {
      const dx = drawing.endX - drawing.startX;
      const dy = drawing.endY - drawing.startY;
      if (Math.sqrt(dx * dx + dy * dy) > 5) {
        onAddAnnotation({
          id: generateId(),
          type: 'arrow',
          startX: drawing.startX,
          startY: drawing.startY,
          endX: drawing.endX,
          endY: drawing.endY,
          strokeColor,
          strokeWidth,
        });
      }
      setDrawing(null);
    }
    setIsPanning(false);
  }, [drawing, strokeColor, strokeWidth, onAddAnnotation]);

  const cursor = isPanning ? 'grabbing' : activeTool === 'arrow' ? 'crosshair' : 'default';

  const imageUrl = convertFileSrc(imagePath);

  return (
    <div
      ref={containerRef}
      className={cn('relative overflow-hidden bg-muted/50', className)}
      onMouseDown={handleMouseDown}
      onMouseMove={handleMouseMove}
      onMouseUp={handleMouseUp}
      onMouseLeave={handleMouseUp}
      style={{ cursor }}
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
          ref={imgRef}
          src={imageUrl}
          alt="edit target"
          draggable={false}
          className="block max-w-none"
          style={{ imageRendering: zoom > 2 ? 'pixelated' : 'auto' }}
          onLoad={() => {
            if (imgRef.current) {
              setImgSize({
                width: imgRef.current.naturalWidth,
                height: imgRef.current.naturalHeight,
              });
            }
          }}
        />
        {imgSize.width > 0 && imgSize.height > 0 && (
          <svg
            width={imgSize.width}
            height={imgSize.height}
            style={{
              position: 'absolute',
              top: 0,
              left: 0,
              pointerEvents: 'none',
            }}
          >
            {annotations.map((ann) => (
              <g key={ann.id}>
                <line
                  x1={ann.startX}
                  y1={ann.startY}
                  x2={ann.endX}
                  y2={ann.endY}
                  stroke={ann.strokeColor}
                  strokeWidth={ann.strokeWidth}
                  strokeLinecap="round"
                />
                <polygon
                  points={getArrowheadPoints(
                    ann.startX,
                    ann.startY,
                    ann.endX,
                    ann.endY,
                    ann.strokeWidth,
                  )}
                  fill={ann.strokeColor}
                />
              </g>
            ))}
            {drawing && (
              <g>
                <line
                  x1={drawing.startX}
                  y1={drawing.startY}
                  x2={drawing.endX}
                  y2={drawing.endY}
                  stroke={strokeColor}
                  strokeWidth={strokeWidth}
                  strokeLinecap="round"
                />
                <polygon
                  points={getArrowheadPoints(
                    drawing.startX,
                    drawing.startY,
                    drawing.endX,
                    drawing.endY,
                    strokeWidth,
                  )}
                  fill={strokeColor}
                />
              </g>
            )}
          </svg>
        )}
      </div>
    </div>
  );
}

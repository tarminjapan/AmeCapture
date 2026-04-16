import { useCallback, useEffect, useRef, useState } from 'react';
import { convertFileSrc } from '@tauri-apps/api/core';
import { cn } from '@/lib/utils';
import type { CropAnnotation, EditorAnnotation, EditorTool } from '@/types';

interface EditorCanvasProps {
  imagePath: string;
  zoom: number;
  panX: number;
  panY: number;
  activeTool: EditorTool;
  strokeColor: string;
  strokeWidth: number;
  fontSize: number;
  annotations: EditorAnnotation[];
  onZoomChange: (zoom: number) => void;
  onPanChange: (x: number, y: number) => void;
  onAddAnnotation: (annotation: EditorAnnotation) => void;
  onCropAnnotation: (annotation: CropAnnotation) => void;
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
  fontSize,
  annotations,
  onZoomChange,
  onPanChange,
  onAddAnnotation,
  onCropAnnotation,
  className,
}: EditorCanvasProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const imgRef = useRef<HTMLImageElement>(null);
  const textInputRef = useRef<HTMLInputElement>(null);
  const [isPanning, setIsPanning] = useState(false);
  const [panStart, setPanStart] = useState({ x: 0, y: 0 });
  const [imgSize, setImgSize] = useState({ width: 0, height: 0 });
  const [drawing, setDrawing] = useState<{
    startX: number;
    startY: number;
    endX: number;
    endY: number;
  } | null>(null);
  const [textInput, setTextInput] = useState<{
    x: number;
    y: number;
    value: string;
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

  useEffect(() => {
    if (textInput && textInputRef.current) {
      textInputRef.current.focus();
    }
  }, [textInput]);

  const commitText = useCallback(() => {
    if (textInput && textInput.value.trim()) {
      onAddAnnotation({
        id: generateId(),
        type: 'text',
        x: textInput.x,
        y: textInput.y,
        text: textInput.value.trim(),
        fontSize,
        strokeColor,
      });
    }
    setTextInput(null);
  }, [textInput, fontSize, strokeColor, onAddAnnotation]);

  const handleMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (e.button === 1) {
        setIsPanning(true);
        setPanStart({ x: e.clientX - panX, y: e.clientY - panY });
        return;
      }
      if (
        e.button === 0 &&
        (activeTool === 'arrow' ||
          activeTool === 'rectangle' ||
          activeTool === 'mosaic' ||
          activeTool === 'crop')
      ) {
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
      if (e.button === 0 && activeTool === 'text') {
        if (textInput) {
          commitText();
          return;
        }
        const coords = getImageCoords(e);
        if (coords) {
          setTextInput({ x: coords.x, y: coords.y, value: '' });
        }
      }
    },
    [panX, panY, activeTool, getImageCoords, textInput, commitText],
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
        if (activeTool === 'arrow') {
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
        } else if (activeTool === 'rectangle') {
          const x = Math.min(drawing.startX, drawing.endX);
          const y = Math.min(drawing.startY, drawing.endY);
          onAddAnnotation({
            id: generateId(),
            type: 'rectangle',
            x,
            y,
            width: Math.abs(dx),
            height: Math.abs(dy),
            strokeColor,
            strokeWidth,
          });
        } else if (activeTool === 'mosaic') {
          const x = Math.min(drawing.startX, drawing.endX);
          const y = Math.min(drawing.startY, drawing.endY);
          onAddAnnotation({
            id: generateId(),
            type: 'mosaic',
            x,
            y,
            width: Math.abs(dx),
            height: Math.abs(dy),
            strength: 20, // Default strength
          });
        } else if (activeTool === 'crop') {
          const x = Math.min(drawing.startX, drawing.endX);
          const y = Math.min(drawing.startY, drawing.endY);
          onCropAnnotation({
            id: generateId(),
            type: 'crop',
            x,
            y,
            width: Math.abs(dx),
            height: Math.abs(dy),
          });
        }
      }
      setDrawing(null);
    }
    setIsPanning(false);
  }, [drawing, activeTool, strokeColor, strokeWidth, onAddAnnotation, onCropAnnotation]);

  const handleTextKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      if (e.key === 'Enter') {
        commitText();
      } else if (e.key === 'Escape') {
        setTextInput(null);
      }
    },
    [commitText],
  );

  const cursor = isPanning
    ? 'grabbing'
    : activeTool === 'arrow' ||
        activeTool === 'rectangle' ||
        activeTool === 'mosaic' ||
        activeTool === 'crop'
      ? 'crosshair'
      : activeTool === 'text'
        ? 'text'
        : 'default';

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
            {annotations.map((ann) => {
              if (ann.type === 'arrow') {
                return (
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
                );
              }
              if (ann.type === 'text') {
                return (
                  <text
                    key={ann.id}
                    x={ann.x}
                    y={ann.y}
                    fontSize={ann.fontSize}
                    fill={ann.strokeColor}
                    dominantBaseline="auto"
                    style={{ userSelect: 'none' }}
                  >
                    {ann.text}
                  </text>
                );
              }
              if (ann.type === 'rectangle') {
                return (
                  <rect
                    key={ann.id}
                    x={ann.x}
                    y={ann.y}
                    width={ann.width}
                    height={ann.height}
                    stroke={ann.strokeColor}
                    strokeWidth={ann.strokeWidth}
                    strokeLinejoin="round"
                    fill="none"
                  />
                );
              }
              if (ann.type === 'mosaic') {
                return (
                  <g key={ann.id}>
                    <rect
                      x={ann.x}
                      y={ann.y}
                      width={ann.width}
                      height={ann.height}
                      fill="rgba(0,0,0,0.2)"
                      stroke="#000"
                      strokeWidth="1"
                      strokeDasharray="4 2"
                    />
                    <text
                      x={ann.x + 4}
                      y={ann.y + 14}
                      fontSize="10"
                      fill="#fff"
                      style={{ pointerEvents: 'none', userSelect: 'none' }}
                    >
                      Mosaic
                    </text>
                  </g>
                );
              }
              if (ann.type === 'crop') {
                return (
                  <g key={ann.id}>
                    <rect x={0} y={0} width={imgSize.width} height={ann.y} fill="rgba(0,0,0,0.5)" />
                    <rect
                      x={0}
                      y={ann.y}
                      width={ann.x}
                      height={ann.height}
                      fill="rgba(0,0,0,0.5)"
                    />
                    <rect
                      x={ann.x + ann.width}
                      y={ann.y}
                      width={imgSize.width - ann.x - ann.width}
                      height={ann.height}
                      fill="rgba(0,0,0,0.5)"
                    />
                    <rect
                      x={0}
                      y={ann.y + ann.height}
                      width={imgSize.width}
                      height={imgSize.height - ann.y - ann.height}
                      fill="rgba(0,0,0,0.5)"
                    />
                    <rect
                      x={ann.x}
                      y={ann.y}
                      width={ann.width}
                      height={ann.height}
                      fill="none"
                      stroke="#fff"
                      strokeWidth="1.5"
                      strokeDasharray="6 3"
                    />
                    <text
                      x={ann.x + 4}
                      y={ann.y + 14}
                      fontSize="10"
                      fill="#fff"
                      style={{ pointerEvents: 'none', userSelect: 'none' }}
                    >
                      Crop
                    </text>
                  </g>
                );
              }
              return null;
            })}
            {drawing && activeTool === 'arrow' && (
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
            {drawing && activeTool === 'rectangle' && (
              <rect
                x={Math.min(drawing.startX, drawing.endX)}
                y={Math.min(drawing.startY, drawing.endY)}
                width={Math.abs(drawing.endX - drawing.startX)}
                height={Math.abs(drawing.endY - drawing.startY)}
                stroke={strokeColor}
                strokeWidth={strokeWidth}
                strokeLinejoin="round"
                fill="none"
              />
            )}
            {drawing && activeTool === 'mosaic' && (
              <rect
                x={Math.min(drawing.startX, drawing.endX)}
                y={Math.min(drawing.startY, drawing.endY)}
                width={Math.abs(drawing.endX - drawing.startX)}
                height={Math.abs(drawing.endY - drawing.startY)}
                fill="rgba(0,0,0,0.1)"
                stroke="#000"
                strokeWidth="1"
                strokeDasharray="4 2"
              />
            )}
            {drawing &&
              activeTool === 'crop' &&
              (() => {
                const cx = Math.min(drawing.startX, drawing.endX);
                const cy = Math.min(drawing.startY, drawing.endY);
                const cw = Math.abs(drawing.endX - drawing.startX);
                const ch = Math.abs(drawing.endY - drawing.startY);
                return (
                  <g>
                    <rect x={0} y={0} width={imgSize.width} height={cy} fill="rgba(0,0,0,0.5)" />
                    <rect x={0} y={cy} width={cx} height={ch} fill="rgba(0,0,0,0.5)" />
                    <rect
                      x={cx + cw}
                      y={cy}
                      width={imgSize.width - cx - cw}
                      height={ch}
                      fill="rgba(0,0,0,0.5)"
                    />
                    <rect
                      x={0}
                      y={cy + ch}
                      width={imgSize.width}
                      height={imgSize.height - cy - ch}
                      fill="rgba(0,0,0,0.5)"
                    />
                    <rect
                      x={cx}
                      y={cy}
                      width={cw}
                      height={ch}
                      fill="none"
                      stroke="#fff"
                      strokeWidth="1.5"
                      strokeDasharray="6 3"
                    />
                  </g>
                );
              })()}
          </svg>
        )}
        {textInput && imgSize.width > 0 && (
          <input
            ref={textInputRef}
            type="text"
            value={textInput.value}
            onChange={(e) =>
              setTextInput((prev) => (prev ? { ...prev, value: e.target.value } : null))
            }
            onKeyDown={handleTextKeyDown}
            onBlur={commitText}
            className="border-none bg-transparent outline-none"
            style={{
              position: 'absolute',
              left: textInput.x,
              top: textInput.y - fontSize,
              fontSize: `${fontSize}px`,
              color: strokeColor,
              minWidth: '50px',
              caretColor: strokeColor,
            }}
          />
        )}
      </div>
    </div>
  );
}

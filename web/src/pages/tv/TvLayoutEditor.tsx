import { useCallback, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react';
import {
  type CreateTvLayoutRequest,
  type TvLayoutDto,
  type TvLayoutZoneDto,
  type TvWidgetType,
  tvWidgetTypeLabels,
} from '../../api/client';

type Props = {
  layout?: TvLayoutDto | null;
  onSave: (data: CreateTvLayoutRequest) => Promise<void>;
};

type DragMode = 'move' | 'resize-se';

const WIDGET_OPTIONS: TvWidgetType[] = [1, 2, 3, 4, 5, 6, 7, 8, 9];

function newZone(widget: TvWidgetType = 1): TvLayoutZoneDto {
  const id = `zone-${Date.now().toString(36)}`;
  return { id, widget, x: 5, y: 5, w: 30, h: 25 };
}

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value));
}

export function TvLayoutEditor({ layout, onSave }: Props) {
  const [name, setName] = useState(layout?.name ?? '');
  const [description, setDescription] = useState(layout?.description ?? '');
  const [zones, setZones] = useState<TvLayoutZoneDto[]>(layout?.zones ?? [newZone()]);
  const [selectedId, setSelectedId] = useState<string | null>(zones[0]?.id ?? null);
  const [saving, setSaving] = useState(false);
  const canvasRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<{
    mode: DragMode;
    zoneId: string;
    startX: number;
    startY: number;
    origin: TvLayoutZoneDto;
  } | null>(null);

  const selected = zones.find((z) => z.id === selectedId) ?? null;

  const updateZone = useCallback((id: string, patch: Partial<TvLayoutZoneDto>) => {
    setZones((current) => current.map((z) => (z.id === id ? { ...z, ...patch } : z)));
  }, []);

  const onPointerDown = (event: ReactPointerEvent, zone: TvLayoutZoneDto, mode: DragMode) => {
    event.preventDefault();
    event.stopPropagation();
    setSelectedId(zone.id);
    dragRef.current = {
      mode,
      zoneId: zone.id,
      startX: event.clientX,
      startY: event.clientY,
      origin: { ...zone },
    };
    (event.target as HTMLElement).setPointerCapture(event.pointerId);
  };

  const onPointerMove = (event: ReactPointerEvent) => {
    const drag = dragRef.current;
    const canvas = canvasRef.current;
    if (!drag || !canvas) return;

    const rect = canvas.getBoundingClientRect();
    const dx = ((event.clientX - drag.startX) / rect.width) * 100;
    const dy = ((event.clientY - drag.startY) / rect.height) * 100;
    const origin = drag.origin;

    if (drag.mode === 'move') {
      updateZone(drag.zoneId, {
        x: clamp(Math.round(origin.x + dx), 0, 100 - origin.w),
        y: clamp(Math.round(origin.y + dy), 0, 100 - origin.h),
      });
      return;
    }

    updateZone(drag.zoneId, {
      w: clamp(Math.round(origin.w + dx), 8, 100 - origin.x),
      h: clamp(Math.round(origin.h + dy), 8, 100 - origin.y),
    });
  };

  const onPointerUp = () => {
    dragRef.current = null;
  };

  async function handleSubmit() {
    setSaving(true);
    try {
      await onSave({ name: name.trim(), description: description.trim() || undefined, zones });
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="tv-layout-editor">
      <div className="tv-layout-editor-toolbar">
        <label>
          Nome
          <input value={name} onChange={(e) => setName(e.target.value)} required />
        </label>
        <label>
          Descrição
          <input value={description} onChange={(e) => setDescription(e.target.value)} />
        </label>
        <button type="button" className="btn btn-secondary" onClick={() => setZones((z) => [...z, newZone()])}>
          Adicionar zona
        </button>
        <button type="button" className="btn" disabled={!name.trim() || saving} onClick={handleSubmit}>
          {saving ? 'Salvando…' : layout ? 'Salvar layout' : 'Criar layout'}
        </button>
      </div>

      <div className="tv-layout-editor-body">
        <div
          ref={canvasRef}
          className="tv-layout-canvas"
          onPointerMove={onPointerMove}
          onPointerUp={onPointerUp}
          onPointerLeave={onPointerUp}
        >
          {zones.map((zone) => (
            <div
              key={zone.id}
              className={`tv-layout-zone ${selectedId === zone.id ? 'selected' : ''}`}
              style={{ left: `${zone.x}%`, top: `${zone.y}%`, width: `${zone.w}%`, height: `${zone.h}%` }}
              onPointerDown={(e) => onPointerDown(e, zone, 'move')}
            >
              <span className="tv-layout-zone-label">{tvWidgetTypeLabels[zone.widget]}</span>
              <button
                type="button"
                className="tv-layout-zone-handle"
                aria-label="Redimensionar"
                onPointerDown={(e) => onPointerDown(e, zone, 'resize-se')}
              />
            </div>
          ))}
        </div>

        <aside className="tv-layout-sidebar card-panel">
          <h4>Zona selecionada</h4>
          {!selected ? <p>Selecione uma zona no canvas.</p> : (
            <>
              <label>
                Widget
                <select
                  value={selected.widget}
                  onChange={(e) => updateZone(selected.id, { widget: Number(e.target.value) as TvWidgetType })}
                >
                  {WIDGET_OPTIONS.map((w) => (
                    <option key={w} value={w}>{tvWidgetTypeLabels[w]}</option>
                  ))}
                </select>
              </label>
              <div className="tv-layout-zone-metrics">
                <span>X: {selected.x}%</span>
                <span>Y: {selected.y}%</span>
                <span>W: {selected.w}%</span>
                <span>H: {selected.h}%</span>
              </div>
              <button
                type="button"
                className="btn btn-danger"
                onClick={() => {
                  setZones((current) => current.filter((z) => z.id !== selected.id));
                  setSelectedId(null);
                }}
              >
                Remover zona
              </button>
            </>
          )}
        </aside>
      </div>
    </div>
  );
}

import { useEffect, useRef } from 'react';

type DigitalSignaturePadProps = {
  onChange: (dataUrl: string | null) => void;
  height?: number;
  /** Altere ao abrir modal para reinicializar o canvas com largura correta. */
  layoutKey?: string | number;
  label?: string;
  hint?: string;
  className?: string;
};

export function DigitalSignaturePad({
  onChange,
  height = 140,
  layoutKey,
  label = 'Assinatura digital do profissional',
  hint = 'Desenhe com o dedo ou caneta stylus no tablet do leito.',
  className,
}: DigitalSignaturePadProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const drawing = useRef(false);
  const hasStroke = useRef(false);
  const onChangeRef = useRef(onChange);
  onChangeRef.current = onChange;

  useEffect(() => {
    hasStroke.current = false;
    onChangeRef.current(null);

    const canvas = canvasRef.current;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    const resize = () => {
      const rect = canvas.getBoundingClientRect();
      if (rect.width < 10) return;

      const ratio = window.devicePixelRatio || 1;
      canvas.width = Math.floor(rect.width * ratio);
      canvas.height = Math.floor(height * ratio);
      ctx.setTransform(1, 0, 0, 1, 0, 0);
      ctx.scale(ratio, ratio);
      ctx.lineCap = 'round';
      ctx.lineJoin = 'round';
      ctx.lineWidth = 2;
      ctx.strokeStyle = '#0f172a';
    };

    resize();

    const observer = new ResizeObserver(() => resize());
    observer.observe(canvas);

    const raf = requestAnimationFrame(() => resize());

    return () => {
      cancelAnimationFrame(raf);
      observer.disconnect();
    };
  }, [height, layoutKey]);

  function getPoint(e: React.PointerEvent<HTMLCanvasElement>) {
    const canvas = canvasRef.current!;
    const rect = canvas.getBoundingClientRect();
    return { x: e.clientX - rect.left, y: e.clientY - rect.top };
  }

  function emitChange() {
    const canvas = canvasRef.current;
    onChangeRef.current(canvas && hasStroke.current ? canvas.toDataURL('image/png') : null);
  }

  function startDraw(e: React.PointerEvent<HTMLCanvasElement>) {
    e.preventDefault();
    const canvas = canvasRef.current;
    const ctx = canvas?.getContext('2d');
    if (!canvas || !ctx) return;
    drawing.current = true;
    canvas.setPointerCapture(e.pointerId);
    const p = getPoint(e);
    ctx.beginPath();
    ctx.moveTo(p.x, p.y);
  }

  function draw(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!drawing.current) return;
    e.preventDefault();
    const ctx = canvasRef.current?.getContext('2d');
    if (!ctx) return;
    const p = getPoint(e);
    ctx.lineTo(p.x, p.y);
    ctx.stroke();
    hasStroke.current = true;
  }

  function endDraw(e: React.PointerEvent<HTMLCanvasElement>) {
    if (!drawing.current) return;
    drawing.current = false;
    const canvas = canvasRef.current;
    if (!canvas) return;
    if (canvas.hasPointerCapture(e.pointerId)) {
      canvas.releasePointerCapture(e.pointerId);
    }
    emitChange();
  }

  function clear() {
    const canvas = canvasRef.current;
    const ctx = canvas?.getContext('2d');
    if (!canvas || !ctx) return;
    const rect = canvas.getBoundingClientRect();
    ctx.clearRect(0, 0, rect.width, height);
    hasStroke.current = false;
    onChangeRef.current(null);
  }

  return (
    <div className={`signature-pad${className ? ` ${className}` : ''}`}>
      <div className="signature-pad-header">
        <span>{label || 'Assinatura'}</span>
        <button type="button" className="btn btn-secondary btn-sm" onClick={clear}>Limpar</button>
      </div>
      <canvas
        ref={canvasRef}
        className="signature-canvas"
        style={{ height, display: 'block' }}
        onPointerDown={startDraw}
        onPointerMove={draw}
        onPointerUp={endDraw}
        onPointerLeave={endDraw}
        onPointerCancel={endDraw}
        aria-label="Área para assinatura manuscrita"
      />
      {hint ? <p className="form-hint">{hint}</p> : null}
    </div>
  );
}

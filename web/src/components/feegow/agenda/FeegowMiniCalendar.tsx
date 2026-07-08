import { useEffect, useMemo, useState } from 'react';

const WEEKDAYS = ['DO', 'SE', 'TE', 'QA', 'QI', 'SE', 'SA'];
const MONTHS = ['JAN', 'FEV', 'MAR', 'ABR', 'MAI', 'JUN', 'JUL', 'AGO', 'SET', 'OUT', 'NOV', 'DEZ'];

type FeegowMiniCalendarProps = {
  selectedDate: string;
  onSelectDate: (isoDate: string) => void;
};

function pad2(n: number) {
  return String(n).padStart(2, '0');
}

function toIso(y: number, m: number, d: number) {
  return `${y}-${pad2(m + 1)}-${pad2(d)}`;
}

export function FeegowMiniCalendar({ selectedDate, onSelectDate }: FeegowMiniCalendarProps) {
  const initial = useMemo(() => {
    const [y, m] = selectedDate.split('-').map(Number);
    return { year: y, month: m - 1 };
  }, [selectedDate]);

  const [viewYear, setViewYear] = useState(initial.year);
  const [viewMonth, setViewMonth] = useState(initial.month);

  useEffect(() => {
    setViewYear(initial.year);
    setViewMonth(initial.month);
  }, [initial.year, initial.month]);

  const cells = useMemo(() => {
    const first = new Date(viewYear, viewMonth, 1);
    const startPad = first.getDay();
    const daysInMonth = new Date(viewYear, viewMonth + 1, 0).getDate();
    const result: { day: number | null; iso?: string }[] = [];
    for (let i = 0; i < startPad; i += 1) result.push({ day: null });
    for (let d = 1; d <= daysInMonth; d += 1) {
      result.push({ day: d, iso: toIso(viewYear, viewMonth, d) });
    }
    return result;
  }, [viewMonth, viewYear]);

  function shiftMonth(delta: number) {
    let m = viewMonth + delta;
    let y = viewYear;
    if (m < 0) { m = 11; y -= 1; }
    if (m > 11) { m = 0; y += 1; }
    setViewMonth(m);
    setViewYear(y);
  }

  return (
    <div className="feegow-agenda-calendar">
      <div className="feegow-agenda-calendar-head">
        <button type="button" className="feegow-cal-nav" onClick={() => shiftMonth(-1)} aria-label="Mês anterior">‹</button>
        <span>{MONTHS[viewMonth]} - {viewYear}</span>
        <button type="button" className="feegow-cal-nav" onClick={() => shiftMonth(1)} aria-label="Próximo mês">›</button>
      </div>
      <div className="feegow-agenda-calendar-weekdays">
        {WEEKDAYS.map((d) => (
          <span key={d}>{d}</span>
        ))}
      </div>
      <div className="feegow-agenda-calendar-grid">
        {cells.map((cell, idx) => (
          <button
            key={cell.iso ?? `empty-${idx}`}
            type="button"
            className={`feegow-cal-day${cell.iso === selectedDate ? ' selected' : ''}${cell.day === null ? ' empty' : ''}`}
            disabled={!cell.iso}
            onClick={() => cell.iso && onSelectDate(cell.iso)}
          >
            {cell.day ?? ''}
          </button>
        ))}
      </div>
    </div>
  );
}

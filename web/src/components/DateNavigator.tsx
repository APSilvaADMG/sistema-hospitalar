import {
  formatBrLongDate,
  isTodayIso,
  shiftIsoDate,
  todayIsoDate,
  toIsoDateInput,
} from '../utils/dateUtils';

type DateNavigatorProps = {
  date: string;
  onChange: (date: string) => void;
};

export function DateNavigator({ date, onChange }: DateNavigatorProps) {
  const today = !isTodayIso(date);

  return (
    <div className="date-navigator">
      <div className="date-navigator-controls">
        <button type="button" className="btn-icon" onClick={() => onChange(shiftIsoDate(date, -1))} title="Dia anterior">
          ‹
        </button>
        {today && (
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => onChange(todayIsoDate())}>
            Hoje
          </button>
        )}
        <button type="button" className="btn-icon" onClick={() => onChange(shiftIsoDate(date, 1))} title="Próximo dia">
          ›
        </button>
        <input
          type="date"
          className="date-navigator-input"
          value={toIsoDateInput(date)}
          onChange={(e) => onChange(e.target.value)}
        />
      </div>
      <div className="date-navigator-label">{formatBrLongDate(date)}</div>
    </div>
  );
}

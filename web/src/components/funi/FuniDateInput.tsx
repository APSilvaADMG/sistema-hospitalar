import { FuniCharInput } from './FuniCharInput';

type Props = {
  value: string;
  onChange: (isoDate: string) => void;
  disabled?: boolean;
};

/** Data no formato das guias FUNI: DD/MM/AAAA (celulas). Aceita ISO yyyy-mm-dd internamente. */
export function FuniDateInput({ value, onChange, disabled }: Props) {
  const iso = value && /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : '';
  const [y, m, d] = iso ? iso.split('-') : ['', '', ''];
  const dd = d ?? '';
  const mm = m ?? '';
  const yyyy = y ?? '';

  function patch(part: 'dd' | 'mm' | 'yyyy', raw: string) {
    const digits = raw.replace(/\D/g, '');
    const next = {
      dd: part === 'dd' ? digits.slice(0, 2) : dd,
      mm: part === 'mm' ? digits.slice(0, 2) : mm,
      yyyy: part === 'yyyy' ? digits.slice(0, 4) : yyyy,
    };
    if (!next.dd && !next.mm && !next.yyyy) {
      onChange('');
    } else if (next.yyyy.length === 4 && next.mm.length === 2 && next.dd.length === 2) {
      onChange(`${next.yyyy}-${next.mm.padStart(2, '0')}-${next.dd.padStart(2, '0')}`);
    } else {
      onChange(`__-${next.mm}-${next.dd}-${next.yyyy}`);
    }
  }

  const displayDd = dd && dd !== '00' ? dd.replace(/^0/, '') || dd : (value.startsWith('__-') ? value.split('-')[2] : dd);
  const displayMm = mm && mm !== '00' ? mm.replace(/^0/, '') || mm : (value.startsWith('__-') ? value.split('-')[1] : mm);
  const displayYyyy = yyyy && yyyy !== '0000' ? yyyy : (value.startsWith('__-') ? value.split('-')[3] ?? '' : yyyy);

  return (
    <div className="funi-date-row" aria-label="Data DD/MM/AAAA">
      <FuniCharInput value={displayDd} maxLength={2} disabled={disabled} onChange={(v) => patch('dd', v)} />
      <span className="funi-date-sep">/</span>
      <FuniCharInput value={displayMm} maxLength={2} disabled={disabled} onChange={(v) => patch('mm', v)} />
      <span className="funi-date-sep">/</span>
      <FuniCharInput value={displayYyyy} maxLength={4} disabled={disabled} onChange={(v) => patch('yyyy', v)} />
    </div>
  );
}

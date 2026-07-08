import { FuniCharInput } from './FuniCharInput';

type Props = {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
};

/** Telefone (DD) NNNNN-NNNN no estilo FUNI. */
export function FuniPhoneInput({ value, onChange, disabled }: Props) {
  const digits = value.replace(/\D/g, '').slice(0, 11);
  const ddd = digits.slice(0, 2);
  const mid = digits.slice(2, 7);
  const end = digits.slice(7, 11);

  function patch(next: string) {
    onChange(next.replace(/\D/g, '').slice(0, 11));
  }

  return (
    <div className="funi-phone-row">
      <span className="funi-phone-paren">(</span>
      <FuniCharInput value={ddd} maxLength={2} disabled={disabled} onChange={(v) => patch(v + mid + end)} />
      <span className="funi-phone-paren">)</span>
      <FuniCharInput value={mid} maxLength={5} disabled={disabled} onChange={(v) => patch(ddd + v + end)} />
      <span className="funi-date-sep">-</span>
      <FuniCharInput value={end} maxLength={4} disabled={disabled} onChange={(v) => patch(ddd + mid + v)} />
    </div>
  );
}

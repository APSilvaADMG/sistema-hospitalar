import { FuniCharInput } from './FuniCharInput';

type Props = {
  value: string;
  integerDigits: number;
  decimalDigits: number;
  onChange: (value: string) => void;
  disabled?: boolean;
};

/** Valor decimal no estilo guia FUNI (parte inteira + vírgula + decimais). */
export function FuniDecimalInput({ value, integerDigits, decimalDigits, onChange, disabled }: Props) {
  const normalized = value.replace('.', ',');
  const [intPart = '', decPart = ''] = normalized.split(',');
  const intPadded = intPart.replace(/\D/g, '').slice(0, integerDigits);
  const decPadded = decPart.replace(/\D/g, '').slice(0, decimalDigits);

  function emit(intVal: string, decVal: string) {
    if (!intVal && !decVal) {
      onChange('');
      return;
    }
    onChange(decVal ? `${intVal},${decVal.padEnd(decimalDigits, '0')}` : intVal);
  }

  return (
    <div className="funi-decimal-row">
      <FuniCharInput
        value={intPadded}
        maxLength={integerDigits}
        disabled={disabled}
        onChange={(v) => emit(v.replace(/\D/g, ''), decPadded)}
      />
      <span className="funi-date-sep">,</span>
      <FuniCharInput
        value={decPadded}
        maxLength={decimalDigits}
        disabled={disabled}
        onChange={(v) => emit(intPadded, v.replace(/\D/g, ''))}
      />
    </div>
  );
}

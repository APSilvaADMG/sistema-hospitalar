type Props = {
  id?: string;
  value: string;
  maxLength: number;
  disabled?: boolean;
  onChange: (value: string) => void;
  className?: string;
};

/** Campo segmentado no estilo das guias FUNI/TISS em papel. */
export function FuniCharInput({ id, value, maxLength, disabled, onChange, className }: Props) {
  const safeValue = value.startsWith('ENC1:') ? '' : value;
  const chars = safeValue.padEnd(maxLength, ' ').slice(0, maxLength).split('');

  function updateAt(index: number, char: string) {
    const next = [...chars];
    next[index] = char.slice(-1);
    onChange(next.join('').trimEnd());
  }

  return (
    <div id={id} className={`funi-char-row ${className ?? ''}`} aria-label={`Campo ${maxLength} posições`}>
      {Array.from({ length: maxLength }).map((_, i) => (
        <input
          key={i}
          className="funi-char-cell"
          type="text"
          maxLength={1}
          disabled={disabled}
          value={chars[i]?.trim() ? chars[i] : ''}
          onChange={(e) => updateAt(i, e.target.value.toUpperCase())}
          onKeyDown={(e) => {
            if (e.key === 'Backspace' && !chars[i]?.trim() && i > 0) {
              const prev = (e.currentTarget.parentElement?.children[i - 1] as HTMLInputElement | undefined);
              prev?.focus();
            }
          }}
        />
      ))}
    </div>
  );
}

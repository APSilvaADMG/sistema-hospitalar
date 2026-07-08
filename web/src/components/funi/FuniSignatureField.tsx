import { DigitalSignaturePad } from '../DigitalSignaturePad';

type Props = {
  fieldNumber: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  layoutKey?: string;
};

export function FuniSignatureField({
  fieldNumber,
  label,
  value,
  onChange,
  layoutKey,
}: Props) {
  return (
    <div className={`funi-signature-box funi-signature-box--pad${value ? ' funi-signature-box--signed' : ''}`}>
      <div className="funi-signature-label">
        <span>
          <strong>{fieldNumber}</strong> — {label}
        </span>
        {value ? <span className="funi-signature-badge">Assinatura salva</span> : null}
      </div>
      <DigitalSignaturePad
        height={88}
        layoutKey={layoutKey ?? `${fieldNumber}-${label}`}
        label="Desenhe a assinatura"
        hint="Use mouse, dedo ou caneta. Clique em Limpar para refazer."
        className="funi-signature-pad"
        onChange={(dataUrl) => onChange(dataUrl ?? '')}
      />
    </div>
  );
}

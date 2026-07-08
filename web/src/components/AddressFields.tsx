import { useRef, useState } from 'react';
import { fetchAddressByCep, formatCep, normalizeCepDigits } from '../utils/cepLookup';

type AddressValues = {
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
};

type AddressFieldsProps<T extends AddressValues> = {
  values: T;
  onChange: (patch: Partial<T>) => void;
  prefix?: string;
};

export function AddressFields<T extends AddressValues>({ values, onChange, prefix = '' }: AddressFieldsProps<T>) {
  const id = (field: string) => `${prefix}${field}`;
  const [cepLoading, setCepLoading] = useState(false);
  const [cepHint, setCepHint] = useState('');
  const lastLookupRef = useRef('');

  async function lookupCep(rawCep: string) {
    const digits = normalizeCepDigits(rawCep);
    if (digits.length !== 8 || digits === lastLookupRef.current) return;

    setCepLoading(true);
    setCepHint('');
    try {
      const address = await fetchAddressByCep(digits);
      lastLookupRef.current = digits;

      const patch: Partial<T> = {
        addressZipCode: formatCep(digits),
        addressStreet: address.addressStreet,
        addressNeighborhood: address.addressNeighborhood,
        addressCity: address.addressCity,
        addressState: address.addressState,
      } as Partial<T>;

      if (address.addressComplement && !values.addressComplement?.trim()) {
        (patch as AddressValues).addressComplement = address.addressComplement;
      }

      onChange(patch);
      setCepHint('Endereço preenchido automaticamente.');

      const numberInput = document.getElementById(id('addressNumber')) as HTMLInputElement | null;
      numberInput?.focus();
    } catch (err) {
      lastLookupRef.current = '';
      setCepHint(err instanceof Error ? err.message : 'Erro ao buscar CEP.');
    } finally {
      setCepLoading(false);
    }
  }

  function handleCepChange(raw: string) {
    const formatted = formatCep(raw);
    const digits = normalizeCepDigits(raw);

    if (digits.length < 8) {
      lastLookupRef.current = '';
      setCepHint('');
    } else if (digits !== lastLookupRef.current) {
      setCepHint('');
    }

    onChange({ addressZipCode: formatted } as Partial<T>);

    if (digits.length === 8) {
      lookupCep(digits).catch(console.error);
    }
  }

  return (
    <>
      <div className="form-field">
        <label htmlFor={id('addressZipCode')}>CEP</label>
        <input
          id={id('addressZipCode')}
          inputMode="numeric"
          placeholder="00000-000"
          maxLength={9}
          value={values.addressZipCode ?? ''}
          onChange={(e) => handleCepChange(e.target.value)}
          onBlur={(e) => lookupCep(e.target.value)}
          disabled={cepLoading}
        />
        {cepLoading && <span className="form-hint">Buscando endereço...</span>}
        {!cepLoading && cepHint && (
          <span className={`form-hint${cepHint.includes('não') || cepHint.includes('Erro') || cepHint.includes('Não') ? ' form-hint-error' : ''}`}>
            {cepHint}
          </span>
        )}
      </div>
      <div className="form-field">
        <label htmlFor={id('addressStreet')}>Logradouro</label>
        <input
          id={id('addressStreet')}
          value={values.addressStreet ?? ''}
          onChange={(e) => onChange({ addressStreet: e.target.value } as Partial<T>)}
        />
      </div>
      <div className="form-field">
        <label htmlFor={id('addressNumber')}>Número</label>
        <input
          id={id('addressNumber')}
          value={values.addressNumber ?? ''}
          onChange={(e) => onChange({ addressNumber: e.target.value } as Partial<T>)}
        />
      </div>
      <div className="form-field">
        <label htmlFor={id('addressComplement')}>Complemento</label>
        <input
          id={id('addressComplement')}
          value={values.addressComplement ?? ''}
          onChange={(e) => onChange({ addressComplement: e.target.value } as Partial<T>)}
        />
      </div>
      <div className="form-field">
        <label htmlFor={id('addressNeighborhood')}>Bairro</label>
        <input
          id={id('addressNeighborhood')}
          value={values.addressNeighborhood ?? ''}
          onChange={(e) => onChange({ addressNeighborhood: e.target.value } as Partial<T>)}
        />
      </div>
      <div className="form-field">
        <label htmlFor={id('addressCity')}>Cidade</label>
        <input
          id={id('addressCity')}
          value={values.addressCity ?? ''}
          onChange={(e) => onChange({ addressCity: e.target.value } as Partial<T>)}
        />
      </div>
      <div className="form-field">
        <label htmlFor={id('addressState')}>UF</label>
        <input
          id={id('addressState')}
          maxLength={2}
          value={values.addressState ?? ''}
          onChange={(e) => onChange({ addressState: e.target.value.toUpperCase() } as Partial<T>)}
        />
      </div>
    </>
  );
}

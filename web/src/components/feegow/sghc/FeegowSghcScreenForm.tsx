import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api, type PatientDto, type ProductDto, type ProfessionalDto, type WardDto } from '../../../api/client';
import {
  buildSghcFormDefaults,
  getSghcFormDefinition,
  resolveSghcModuleDeepLink,
  submitSghcForm,
  type SghcFormField,
  type SghcFormMode,
  type SghcFormDefinition,
  type SghcDynamicOptionSource,
} from './sghcScreenForms';
import type { SghcDataModule } from './sghcScreenData';

type Props = {
  module: SghcDataModule;
  mode: SghcFormMode;
  route: string;
  moduleLink?: string | null;
  onSaved?: () => void;
};

type SelectOptions = {
  patients: PatientDto[];
  wards: WardDto[];
  products: ProductDto[];
  professionals: ProfessionalDto[];
};

export function FeegowSghcScreenForm({ module, mode, route, moduleLink, onSaved }: Props) {
  const navigate = useNavigate();
  const definition = useMemo(
    () => getSghcFormDefinition(module, mode, route),
    [module, mode, route],
  );
  const [values, setValues] = useState(() => buildSghcFormDefaults(definition));
  const [recordId, setRecordId] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [selectOptions, setSelectOptions] = useState<SelectOptions>({
    patients: [],
    wards: [],
    products: [],
    professionals: [],
  });

  useEffect(() => {
    setValues(buildSghcFormDefaults(definition));
    setError('');
    setSuccess('');
  }, [definition]);

  const neededOptions = useMemo(() => {
    const keys = new Set<SghcDynamicOptionSource>();
    for (const field of definition.fields) {
      if (field.optionSource) keys.add(field.optionSource);
    }
    return keys;
  }, [definition.fields]);

  useEffect(() => {
    if (mode !== 'add' || neededOptions.size === 0) return;
    let cancelled = false;
    (async () => {
      try {
        const [patients, wards, products, professionals] = await Promise.all([
          neededOptions.has('patients') ? api.getPatients('', 1, 100).then((r) => r.items) : Promise.resolve([]),
          neededOptions.has('wards') ? api.getWards() : Promise.resolve([]),
          neededOptions.has('products') ? api.getProducts('', false, 1) : Promise.resolve([]),
          neededOptions.has('professionals') ? api.getProfessionals() : Promise.resolve([]),
        ]);
        if (!cancelled) {
          setSelectOptions({ patients, wards, products, professionals });
        }
      } catch (err) {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Erro ao carregar opções do formulário.');
        }
      }
    })();
    return () => { cancelled = true; };
  }, [mode, neededOptions]);

  const deepLink = resolveSghcModuleDeepLink(
    module,
    mode,
    route,
    recordId.trim() || undefined,
  );

  function setField(key: string, value: string) {
    setValues((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);
    try {
      const result = await submitSghcForm(module, route, values);
      setSuccess(result.message);
      onSaved?.();
      if (result.navigateTo) {
        window.setTimeout(() => navigate(result.navigateTo!), 900);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.');
    } finally {
      setLoading(false);
    }
  }

  if (mode === 'edit') {
    return (
      <EditRedirectPanel
        definition={definition}
        route={route}
        recordId={recordId}
        onRecordIdChange={setRecordId}
        deepLink={deepLink}
        moduleLink={moduleLink}
      />
    );
  }

  if (definition.redirectOnly) {
    return (
      <RedirectOnlyPanel
        definition={definition}
        deepLink={deepLink}
        moduleLink={moduleLink}
      />
    );
  }

  return (
    <div className="feegow-patient-card feegow-sghc-form-card">
      <header className="feegow-sghc-form-header">
        <h2>{definition.title}</h2>
        {definition.hint ? <p className="form-hint">{definition.hint}</p> : null}
      </header>

      {error ? <div className="alert alert-error">{error}</div> : null}
      {success ? <div className="alert alert-success">{success}</div> : null}

      <form className="feegow-sghc-form" onSubmit={handleSubmit}>
        {definition.fields.map((field) => (
          <FieldControl
            key={field.key}
            field={field}
            value={values[field.key] ?? ''}
            onChange={(value) => setField(field.key, value)}
            selectOptions={selectOptions}
          />
        ))}
        <div className="feegow-sghc-form-actions">
          <button type="submit" className="btn btn-sm" disabled={loading}>
            {loading ? 'Salvando…' : 'Salvar'}
          </button>
          <Link to={deepLink} className="btn btn-secondary btn-sm">
            Cadastro completo
          </Link>
          {moduleLink && moduleLink !== deepLink ? (
            <Link to={moduleLink} className="btn btn-secondary btn-sm">
              Ir ao módulo
            </Link>
          ) : null}
        </div>
      </form>
    </div>
  );
}

function EditRedirectPanel({
  definition,
  route,
  recordId,
  onRecordIdChange,
  deepLink,
  moduleLink,
}: {
  definition: SghcFormDefinition;
  route: string;
  recordId: string;
  onRecordIdChange: (value: string) => void;
  deepLink: string;
  moduleLink?: string | null;
}) {
  const listLink = moduleLink ?? resolveSghcModuleDeepLink(definition.module, 'add', route);
  return (
    <div className="feegow-patient-card feegow-sghc-form-card">
      <header className="feegow-sghc-form-header">
        <h2>{definition.title}</h2>
        {definition.hint ? <p className="form-hint">{definition.hint}</p> : null}
      </header>
      <label className="feegow-sghc-field">
        <span>{definition.fields[0]?.label ?? 'ID do registro'}</span>
        <input
          type="text"
          value={recordId}
          onChange={(e) => onRecordIdChange(e.target.value)}
          placeholder="Cole o ID da linha selecionada na listagem"
        />
      </label>
      <div className="feegow-sghc-form-actions">
        <Link
          to={deepLink}
          className={`btn btn-sm${!recordId.trim() ? ' disabled' : ''}`}
          aria-disabled={!recordId.trim()}
          onClick={(e) => { if (!recordId.trim()) e.preventDefault(); }}
        >
          Abrir para editar
        </Link>
        <Link to={listLink} className="btn btn-secondary btn-sm">
          Ver listagem no módulo
        </Link>
        {moduleLink ? (
          <Link to={moduleLink} className="btn btn-secondary btn-sm">Módulo completo</Link>
        ) : null}
      </div>
    </div>
  );
}

function RedirectOnlyPanel({
  definition,
  deepLink,
  moduleLink,
}: {
  definition: SghcFormDefinition;
  deepLink: string;
  moduleLink?: string | null;
}) {
  return (
    <div className="feegow-patient-card feegow-sghc-form-card">
      <header className="feegow-sghc-form-header">
        <h2>{definition.title}</h2>
        {definition.hint ? <p className="form-hint">{definition.hint}</p> : null}
      </header>
      <div className="feegow-sghc-form-actions">
        <Link to={deepLink} className="btn btn-sm">Continuar no módulo Feegow</Link>
        {moduleLink && moduleLink !== deepLink ? (
          <Link to={moduleLink} className="btn btn-secondary btn-sm">Módulo completo</Link>
        ) : null}
      </div>
    </div>
  );
}

function FieldControl({
  field,
  value,
  onChange,
  selectOptions,
}: {
  field: SghcFormField;
  value: string;
  onChange: (value: string) => void;
  selectOptions: SelectOptions;
}) {
  if (field.type === 'select') {
    const options = resolveSelectOptions(field, selectOptions);
    return (
      <label className="feegow-sghc-field">
        <span>{field.label}{field.required ? ' *' : ''}</span>
        <select
          required={field.required}
          value={value}
          onChange={(e) => onChange(e.target.value)}
        >
          <option value="">Selecione…</option>
          {options.map((opt) => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>
      </label>
    );
  }

  if (field.type === 'textarea') {
    return (
      <label className="feegow-sghc-field feegow-sghc-field-full">
        <span>{field.label}{field.required ? ' *' : ''}</span>
        <textarea
          required={field.required}
          value={value}
          placeholder={field.placeholder}
          rows={3}
          onChange={(e) => onChange(e.target.value)}
        />
      </label>
    );
  }

  return (
    <label className="feegow-sghc-field">
      <span>{field.label}{field.required ? ' *' : ''}</span>
      <input
        type={field.type}
        required={field.required}
        value={value}
        placeholder={field.placeholder}
        onChange={(e) => onChange(e.target.value)}
      />
    </label>
  );
}

function resolveSelectOptions(
  field: SghcFormField,
  data: SelectOptions,
): { value: string; label: string }[] {
  if (field.options?.length) return field.options;
  switch (field.optionSource) {
    case 'patients':
      return data.patients.map((p) => ({ value: p.id, label: `${p.fullName} — ${p.cpf}` }));
    case 'wards':
      return data.wards.map((w) => ({ value: w.id, label: w.code ? `${w.name} (${w.code})` : w.name }));
    case 'products':
      return data.products.map((p) => ({ value: p.id, label: `${p.name}${p.sku ? ` — ${p.sku}` : ''}` }));
    case 'professionals':
      return data.professionals.map((p) => ({
        value: p.id,
        label: p.crm ? `${p.fullName} — CRM ${p.crm}` : p.fullName,
      }));
    default:
      return [];
  }
}

import { useMemo, useState, type FormEvent } from 'react';
import {
  entryTypeLabels,
  type PatientDetailDto,
  type SpecialtyClinicalCatalogDto,
} from '../api/client';
import {
  ANAMNESIS_SNIPPETS,
  ENTRY_TYPE_HINTS,
  TEXT_TEMPLATES,
  buildAnamnesisContent,
  buildEvolutionContent,
  hydrateAnamnesisFromStored,
  hydrateEvolutionFromStored,
  seedAnamnesisFromPatient,
  type AnamnesisData,
} from '../data/clinicalEntryTemplates';
import { useAdministrationRoutes } from '../hooks/useAdministrationRoutes';
import {
  buildPrescriptionBlock,
  emptyPrescriptionLine,
  type PrescriptionLineItem,
} from '../utils/prescriptionFormat';
import { Cid10Picker } from './Cid10Picker';
import { DigitalSignaturePad } from './DigitalSignaturePad';
import { PrescriptionItemsEditor } from './PrescriptionItemsEditor';
import { SpecialtyCatalogPanel } from './SpecialtyCatalogPanel';

export type ClinicalEntryPayload = {
  entryType: number;
  cid10Code: string;
  content: string;
};

type Props = {
  catalog: SpecialtyClinicalCatalogDto | null;
  patient: PatientDetailDto | null;
  signOnSave: boolean;
  signatureImage: string | null;
  onSignOnSaveChange: (value: boolean) => void;
  onSignatureImageChange: (value: string | null) => void;
  onCancel: () => void;
  onSubmit: (payload: ClinicalEntryPayload) => void;
  initialEntryType?: number;
  allowedEntryTypes?: number[];
  editContent?: string;
  editCid10Code?: string;
  readOnly?: boolean;
  showSignature?: boolean;
  submitLabel?: string;
  submitting?: boolean;
  signatureLayoutKey?: string | number;
};

function TemplateChips({ templates, onPick }: { templates: string[]; onPick: (text: string) => void }) {
  if (templates.length === 0) return null;
  return (
    <div className="pep-template-chips">
      <span className="pep-template-label">Textos pré-definidos:</span>
      {templates.map((t) => (
        <button key={t} type="button" className="pep-template-chip" onClick={() => onPick(t)}>
          {t.length > 48 ? `${t.slice(0, 48)}…` : t}
        </button>
      ))}
    </div>
  );
}

function appendSnippet(current: string, snippet: string): string {
  const trimmed = current.trim();
  return trimmed ? `${trimmed}\n${snippet}` : snippet;
}

function AnamnesisSection({
  title,
  field,
  value,
  onChange,
  rows = 2,
  snippets,
  disabled = false,
}: {
  title: string;
  field: keyof AnamnesisData;
  value: string;
  onChange: (field: keyof AnamnesisData, value: string) => void;
  rows?: number;
  snippets?: string[];
  disabled?: boolean;
}) {
  return (
    <div className="pep-anamnesis-section">
      <label>{title}</label>
      {snippets && snippets.length > 0 && !disabled && (
        <div className="pep-template-chips compact">
          {snippets.map((s) => (
            <button
              key={s}
              type="button"
              className="pep-template-chip"
              onClick={() => onChange(field, appendSnippet(value, s))}
            >
              + {s.length > 40 ? `${s.slice(0, 40)}…` : s}
            </button>
          ))}
        </div>
      )}
      <textarea
        rows={rows}
        value={value}
        onChange={(e) => onChange(field, e.target.value)}
        placeholder={`${title}...`}
        disabled={disabled}
      />
    </div>
  );
}

export function ClinicalEntryForm({
  catalog,
  patient,
  signOnSave,
  onSignOnSaveChange,
  onSignatureImageChange,
  onCancel,
  onSubmit,
  initialEntryType = 2,
  allowedEntryTypes,
  editContent,
  editCid10Code,
  readOnly = false,
  showSignature = true,
  submitLabel = 'Salvar registro',
  submitting = false,
  signatureLayoutKey,
}: Props) {
  const resolvedInitialType = initialEntryType;
  const [entryType, setEntryType] = useState(resolvedInitialType);
  const [cid10Code, setCid10Code] = useState(editCid10Code ?? '');
  const [anamnesis, setAnamnesis] = useState<AnamnesisData>(() => {
    if (editContent && resolvedInitialType === 1) {
      return hydrateAnamnesisFromStored(editContent, patient);
    }
    return seedAnamnesisFromPatient(patient);
  });
  const [freeText, setFreeText] = useState(() => {
    if (editContent && resolvedInitialType !== 1) {
      return hydrateEvolutionFromStored(editContent).freeText;
    }
    return '';
  });
  const [soap, setSoap] = useState(() => {
    if (editContent && resolvedInitialType === 2) {
      return hydrateEvolutionFromStored(editContent).soap;
    }
    return { subjective: '', objective: '', assessment: '', plan: '' };
  });
  const [selectedLabs, setSelectedLabs] = useState<string[]>([]);
  const [prescriptionItems, setPrescriptionItems] = useState<PrescriptionLineItem[]>([]);
  const [selectedImaging, setSelectedImaging] = useState('');
  const { routes: administrationRoutes, loading: routesLoading } = useAdministrationRoutes();

  const entryTypeOptions = useMemo(() => {
    const entries = Object.entries(entryTypeLabels).map(([v, l]) => ({ value: Number(v), label: l }));
    if (!allowedEntryTypes?.length) return entries;
    return entries.filter((e) => allowedEntryTypes.includes(e.value));
  }, [allowedEntryTypes]);

  const disabled = readOnly || submitting;

  const showCatalog = entryType === 3 || entryType === 4;
  const isAnamnesis = entryType === 1;
  const isEvolution = entryType === 2;

  const suggestText = useMemo(() => {
    if (isAnamnesis) {
      return [anamnesis.chiefComplaint, anamnesis.illnessHistory, anamnesis.physicalExam].filter(Boolean).join('. ');
    }
    return [soap.subjective, soap.objective, freeText].filter(Boolean).join('. ');
  }, [isAnamnesis, anamnesis, soap, freeText]);

  function updateAnamnesis(field: keyof AnamnesisData, value: string) {
    setAnamnesis((prev) => ({ ...prev, [field]: value }));
  }

  function buildCatalogAppendix(): string {
    if (!catalog) return '';
    const lines: string[] = [];
    if (entryType === 4 && selectedLabs.length > 0) {
      const names = catalog.labExams.filter((e) => selectedLabs.includes(e.id)).map((e) => e.name);
      lines.push('Exames solicitados:\n- ' + names.join('\n- '));
    }
    if (entryType === 4 && selectedImaging) {
      const img = catalog.imagingProcedures.find((p) => p.id === selectedImaging);
      if (img) lines.push(`Imagem solicitada: ${img.name}`);
    }
    if (entryType === 3 && prescriptionItems.length > 0) {
      lines.push(buildPrescriptionBlock(prescriptionItems, administrationRoutes));
    }
    return lines.join('\n\n');
  }

  function buildContent(): string {
    let main = '';
    if (isAnamnesis) {
      main = buildAnamnesisContent(anamnesis);
    } else if (isEvolution) {
      main = buildEvolutionContent(freeText, soap);
    } else {
      main = freeText.trim();
    }
    const appendix = buildCatalogAppendix();
    return [main, appendix].filter(Boolean).join('\n\n');
  }

  const prescriptionInvalid = entryType === 3
    && prescriptionItems.some((item) => !item.administrationRouteCode);

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (prescriptionInvalid) return;
    onSubmit({ entryType, cid10Code, content: buildContent() });
  }

  function handleTypeChange(next: number) {
    setEntryType(next);
    setSelectedLabs([]);
    setPrescriptionItems([]);
    setSelectedImaging('');
  }

  function handleMedToggle(medId: string) {
    if (!catalog) return;
    const med = catalog.medications.find((m) => m.id === medId);
    if (!med) return;

    setPrescriptionItems((prev) => {
      const exists = prev.some((item) => item.medicationId === medId);
      if (exists) return prev.filter((item) => item.medicationId !== medId);
      return [...prev, emptyPrescriptionLine(med, administrationRoutes)];
    });
  }

  return (
    <form className="form-grid pep-clinical-entry-form" onSubmit={handleSubmit}>
      <div className="form-field">
        <label>Tipo de registro</label>
        <select
          value={entryType}
          onChange={(e) => handleTypeChange(Number(e.target.value))}
          disabled={disabled}
        >
          {entryTypeOptions.map(({ value, label }) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
        <p className="form-hint">{ENTRY_TYPE_HINTS[entryType]}</p>
      </div>

      <div className="form-field full">
        <label>CID-10</label>
        <Cid10Picker
          value={cid10Code}
          onChange={(code) => setCid10Code(code)}
          onSuggestFromText={() => suggestText || undefined}
        />
      </div>

      {isAnamnesis && (
        <div className="form-field full pep-anamnesis-grid">
          <h4 className="pep-section-title">Anamnese completa</h4>
          <AnamnesisSection
            title="Queixa principal (QP)"
            field="chiefComplaint"
            value={anamnesis.chiefComplaint}
            onChange={updateAnamnesis}
            rows={2}
            disabled={disabled}
          />
          <AnamnesisSection
            title="História da doença atual (HDA)"
            field="illnessHistory"
            value={anamnesis.illnessHistory}
            onChange={updateAnamnesis}
            rows={4}
            snippets={ANAMNESIS_SNIPPETS.illnessHistory}
            disabled={disabled}
          />
          <div className="pep-anamnesis-two-col">
            <AnamnesisSection
              title="Antecedentes pessoais"
              field="personalHistory"
              value={anamnesis.personalHistory}
              onChange={updateAnamnesis}
              snippets={ANAMNESIS_SNIPPETS.personalHistory}
              disabled={disabled}
            />
            <AnamnesisSection
              title="Antecedentes familiares"
              field="familyHistory"
              value={anamnesis.familyHistory}
              onChange={updateAnamnesis}
              snippets={ANAMNESIS_SNIPPETS.familyHistory}
              disabled={disabled}
            />
          </div>
          <div className="pep-anamnesis-two-col">
            <AnamnesisSection
              title="Antecedentes cirúrgicos"
              field="surgicalHistory"
              value={anamnesis.surgicalHistory}
              onChange={updateAnamnesis}
              snippets={ANAMNESIS_SNIPPETS.surgicalHistory}
              disabled={disabled}
            />
            <AnamnesisSection
              title="Medicações em uso"
              field="medicationsInUse"
              value={anamnesis.medicationsInUse}
              onChange={updateAnamnesis}
              snippets={ANAMNESIS_SNIPPETS.medicationsInUse}
              disabled={disabled}
            />
          </div>
          <AnamnesisSection
            title="Alergias"
            field="allergies"
            value={anamnesis.allergies}
            onChange={updateAnamnesis}
            snippets={ANAMNESIS_SNIPPETS.allergies}
            disabled={disabled}
          />
          <div className="pep-habits-row">
            <div className="form-field">
              <label>Tabagismo</label>
              <select value={anamnesis.smoking} onChange={(e) => updateAnamnesis('smoking', e.target.value)} disabled={disabled}>
                <option value="">—</option>
                <option value="nao">Não</option>
                <option value="sim">Sim</option>
                <option value="ex">Ex-tabagista</option>
              </select>
            </div>
            <div className="form-field">
              <label>Etilismo</label>
              <select value={anamnesis.alcohol} onChange={(e) => updateAnamnesis('alcohol', e.target.value)} disabled={disabled}>
                <option value="">—</option>
                <option value="nao">Não</option>
                <option value="social">Social</option>
                <option value="sim">Sim</option>
              </select>
            </div>
            <div className="form-field">
              <label>Drogas ilícitas</label>
              <select value={anamnesis.illicitDrugs} onChange={(e) => updateAnamnesis('illicitDrugs', e.target.value)} disabled={disabled}>
                <option value="">—</option>
                <option value="nao">Nega</option>
                <option value="sim">Relata uso</option>
              </select>
            </div>
          </div>
          <div className="pep-anamnesis-two-col">
            <AnamnesisSection title="Atividade física" field="physicalActivity" value={anamnesis.physicalActivity} onChange={updateAnamnesis} rows={2} disabled={disabled} />
            <AnamnesisSection title="Alimentação" field="diet" value={anamnesis.diet} onChange={updateAnamnesis} rows={2} disabled={disabled} />
          </div>
          <AnamnesisSection
            title="Revisão de sistemas"
            field="systemsReview"
            value={anamnesis.systemsReview}
            onChange={updateAnamnesis}
            snippets={ANAMNESIS_SNIPPETS.systemsReview}
            disabled={disabled}
          />
          <AnamnesisSection
            title="Sinais vitais"
            field="vitalSigns"
            value={anamnesis.vitalSigns}
            onChange={updateAnamnesis}
            snippets={ANAMNESIS_SNIPPETS.vitalSigns}
            disabled={disabled}
          />
          <AnamnesisSection
            title="Exame físico"
            field="physicalExam"
            value={anamnesis.physicalExam}
            onChange={updateAnamnesis}
            rows={4}
            snippets={ANAMNESIS_SNIPPETS.physicalExam}
            disabled={disabled}
          />
          <div className="pep-anamnesis-two-col">
            <AnamnesisSection
              title="Hipótese diagnóstica"
              field="hypothesis"
              value={anamnesis.hypothesis}
              onChange={updateAnamnesis}
              snippets={ANAMNESIS_SNIPPETS.hypothesis}
              disabled={disabled}
            />
            <AnamnesisSection
              title="Conduta"
              field="conduct"
              value={anamnesis.conduct}
              onChange={updateAnamnesis}
              snippets={ANAMNESIS_SNIPPETS.conduct}
              disabled={disabled}
            />
          </div>
          <AnamnesisSection
            title="Observações / texto livre"
            field="freeNotes"
            value={anamnesis.freeNotes}
            onChange={updateAnamnesis}
            rows={3}
            disabled={disabled}
          />
        </div>
      )}

      {isEvolution && (
        <div className="form-field full pep-soap-grid">
          <h4 className="pep-section-title">Evolução (formato SOAP)</h4>
          <div className="form-field">
            <label>Subjetivo (S)</label>
            <textarea rows={2} value={soap.subjective} onChange={(e) => setSoap({ ...soap, subjective: e.target.value })} placeholder="Queixas e relato do paciente..." disabled={disabled} />
          </div>
          <div className="form-field">
            <label>Objetivo (O)</label>
            <textarea rows={2} value={soap.objective} onChange={(e) => setSoap({ ...soap, objective: e.target.value })} placeholder="Exame físico, sinais vitais..." disabled={disabled} />
          </div>
          <div className="form-field">
            <label>Avaliação (A)</label>
            <textarea rows={2} value={soap.assessment} onChange={(e) => setSoap({ ...soap, assessment: e.target.value })} placeholder="Impressão diagnóstica..." disabled={disabled} />
          </div>
          <div className="form-field">
            <label>Plano (P)</label>
            <textarea rows={2} value={soap.plan} onChange={(e) => setSoap({ ...soap, plan: e.target.value })} placeholder="Conduta e orientações..." disabled={disabled} />
          </div>
        </div>
      )}

      {!isAnamnesis && (
        <div className="form-field full">
          <label>{isEvolution ? 'Complemento / texto livre' : 'Texto livre'}</label>
          {!disabled && (
            <TemplateChips
              templates={TEXT_TEMPLATES[entryType] ?? []}
              onPick={(t) => setFreeText((prev) => appendSnippet(prev, t))}
            />
          )}
          <textarea
            rows={isEvolution ? 3 : 5}
            value={freeText}
            onChange={(e) => setFreeText(e.target.value)}
            placeholder="Descreva o registro ou use os textos pré-definidos acima..."
            disabled={disabled}
          />
        </div>
      )}

      {showSignature && !readOnly && (
        <>
          <div className="form-field full">
            <label className="pep-checkbox-label">
              <input
                type="checkbox"
                checked={signOnSave}
                onChange={(e) => {
                  onSignOnSaveChange(e.target.checked);
                  if (!e.target.checked) onSignatureImageChange(null);
                }}
                disabled={submitting}
              />
              Assinar digitalmente ao salvar (recomendado em leitos)
            </label>
          </div>
          {signOnSave && (
            <div className="form-field full">
              <DigitalSignaturePad onChange={onSignatureImageChange} layoutKey={signatureLayoutKey} />
            </div>
          )}
        </>
      )}

      {readOnly && (
        <p className="form-hint">Este registro foi assinado digitalmente e não pode ser alterado.</p>
      )}

      {showCatalog && catalog && (
        <div className="form-field full">
          <SpecialtyCatalogPanel
            specialtyName={catalog.specialtyName}
            labExams={catalog.labExams}
            imagingProcedures={catalog.imagingProcedures}
            medications={catalog.medications}
            selectedLabIds={selectedLabs}
            onLabToggle={(id) => setSelectedLabs((p) => (p.includes(id) ? p.filter((x) => x !== id) : [...p, id]))}
            selectedImagingId={selectedImaging}
            onImagingSelect={(id) => setSelectedImaging(id)}
            selectedMedIds={prescriptionItems.map((item) => item.medicationId)}
            onMedToggle={(id) => handleMedToggle(id)}
            showLabs={entryType === 4}
            showImaging={entryType === 4}
            showMeds={entryType === 3}
          />
        </div>
      )}

      {entryType === 3 && prescriptionItems.length > 0 && (
        <div className="form-field full">
          <PrescriptionItemsEditor
            items={prescriptionItems}
            routes={administrationRoutes}
            routesLoading={routesLoading}
            onChange={setPrescriptionItems}
          />
        </div>
      )}

      {entryType === 3 && prescriptionItems.some((item) => !item.administrationRouteCode) && (
        <p className="form-hint form-hint-error">Informe a via de administração de todos os medicamentos.</p>
      )}

      <div className="form-field full modal-actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={submitting}>Cancelar</button>
        {!readOnly && (
          <button type="submit" className="btn" disabled={prescriptionInvalid || submitting}>
            {submitting ? 'Salvando…' : submitLabel}
          </button>
        )}
      </div>
    </form>
  );
}

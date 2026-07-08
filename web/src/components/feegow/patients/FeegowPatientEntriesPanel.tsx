import { useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  entryTypeLabels,
  entryTypeToNumber,
  formatEntryTypeLabel,
  type MedicalRecordEntryDto,
  type PatientDetailDto,
} from '../../../api/client';
import { useAuth } from '../../../auth/AuthContext';
import { usePatientDigitalRecord } from '../../../hooks/usePatientDigitalRecord';
import { Cid10Picker } from '../../Cid10Picker';
import { Modal } from '../../Modal';
import { formatBrDateTime } from '../../../utils/dateUtils';

export type FeegowEntriesPanelConfig = {
  createButtonLabel: string;
  emptyMessage: string;
  contentPlaceholder: string;
  filter: (entry: MedicalRecordEntryDto) => boolean;
  allowedEntryTypes?: number[];
  defaultEntryType?: number;
  fixedEntryType?: number;
  showCid10?: boolean;
  contentTemplates?: string[];
  showTypeColumn?: boolean;
  pepLink?: string;
  extraFooterLinks?: { label: string; to: string }[];
};

type Props = {
  patientId: string;
  patient: PatientDetailDto;
  config: FeegowEntriesPanelConfig;
};

type EditorState = {
  mode: 'create' | 'edit';
  entry?: MedicalRecordEntryDto;
};

function appendSnippet(current: string, snippet: string): string {
  const trimmed = current.trim();
  return trimmed ? `${trimmed}\n${snippet}` : snippet;
}

export function FeegowPatientEntriesPanel({ patientId, patient, config }: Props) {
  const { hasPermission } = useAuth();
  const canWrite = hasPermission('pep.write');
  const { entries: allEntries, loading, reload } = usePatientDigitalRecord(patientId);

  const entries = useMemo(
    () => allEntries.filter(config.filter),
    [allEntries, config],
  );

  const [editor, setEditor] = useState<EditorState | null>(null);
  const [entryType, setEntryType] = useState(config.defaultEntryType ?? config.fixedEntryType ?? 2);
  const [cid10Code, setCid10Code] = useState('');
  const [content, setContent] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const createTypes = config.allowedEntryTypes
    ?? (config.fixedEntryType ? [config.fixedEntryType] : [1, 2, 3, 4, 5]);

  function openCreate() {
    setEditor({ mode: 'create' });
    setEntryType(config.fixedEntryType ?? config.defaultEntryType ?? createTypes[0] ?? 2);
    setCid10Code('');
    setContent('');
    setError('');
  }

  function openEdit(entry: MedicalRecordEntryDto) {
    setEditor({ mode: 'edit', entry });
    setEntryType(entryTypeToNumber(entry.entryType));
    setCid10Code(entry.cid10Code ?? '');
    setContent(entry.content ?? '');
    setError('');
  }

  function closeEditor() {
    setEditor(null);
    setError('');
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!canWrite || !editor) return;

    const trimmed = content.trim();
    if (!trimmed) {
      setError('Informe o conteúdo do registro.');
      return;
    }

    if (editor.mode === 'edit' && editor.entry?.isSigned) {
      setError('Registro assinado não pode ser alterado.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      const payload = {
        entryType: config.fixedEntryType ?? entryType,
        content: trimmed,
        cid10Code: config.showCid10 === false ? undefined : (cid10Code.trim() || undefined),
      };

      if (editor.mode === 'create') {
        await api.addMedicalRecordEntry(patientId, payload);
      } else if (editor.entry) {
        await api.updateMedicalRecordEntry(patientId, editor.entry.id, payload);
      }

      closeEditor();
      await reload();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar o registro.');
    } finally {
      setSaving(false);
    }
  }

  const editorSigned = editor?.mode === 'edit' && editor.entry?.isSigned;
  const showTypeColumn = config.showTypeColumn !== false;

  return (
    <>
      <div className="feegow-patient-section-toolbar">
        {canWrite ? (
          <button type="button" className="feegow-patient-save-btn" onClick={openCreate}>
            {config.createButtonLabel}
          </button>
        ) : null}
      </div>

      {loading ? (
        <p className="feegow-patient-section-empty">Carregando…</p>
      ) : entries.length === 0 ? (
        <p className="feegow-patient-section-empty">{config.emptyMessage}</p>
      ) : (
        <table className="feegow-patient-section-table">
          <thead>
            <tr>
              <th>Data</th>
              {showTypeColumn ? <th>Tipo</th> : null}
              {config.showCid10 !== false ? <th>CID</th> : null}
              <th>Profissional</th>
              <th>Resumo</th>
              {canWrite ? <th className="feegow-patient-section-actions-col">Ações</th> : null}
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.id}>
                <td>{formatBrDateTime(entry.createdAt)}</td>
                {showTypeColumn ? <td>{formatEntryTypeLabel(entry.entryType)}</td> : null}
                {config.showCid10 !== false ? <td>{entry.cid10Code ?? '—'}</td> : null}
                <td>{entry.professionalName ?? '—'}</td>
                <td>{entry.content?.slice(0, 120) || '—'}</td>
                {canWrite ? (
                  <td className="feegow-patient-section-actions-col">
                    <button
                      type="button"
                      className="feegow-patient-entry-edit-btn"
                      onClick={() => openEdit(entry)}
                      disabled={entry.isSigned}
                      title={entry.isSigned ? 'Registro assinado' : 'Editar'}
                    >
                      Editar
                    </button>
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      )}

      {config.pepLink ? (
        <p className="feegow-patient-section-pep-link">
          <Link to={config.pepLink}>Abrir prontuário completo (PEP)</Link>
        </p>
      ) : null}
      {config.extraFooterLinks?.map((link) => (
        <p key={link.to} className="feegow-patient-section-pep-link">
          <Link to={link.to}>{link.label}</Link>
        </p>
      ))}

      <Modal
        open={editor != null}
        onClose={() => { if (!saving) closeEditor(); }}
        title={editor?.mode === 'create' ? 'Novo registro' : 'Editar registro'}
        subtitle={patient.fullName}
        width="lg"
      >
        <form className="feegow-form-grid" onSubmit={handleSubmit}>
          {!config.fixedEntryType && createTypes.length > 1 ? (
            <label className="feegow-field feegow-field-grow2">
              <span>Tipo</span>
              <select
                value={entryType}
                onChange={(e) => setEntryType(Number(e.target.value))}
                disabled={editorSigned || saving}
              >
                {createTypes.map((type) => (
                  <option key={type} value={type}>{entryTypeLabels[type]}</option>
                ))}
              </select>
            </label>
          ) : null}

          {config.showCid10 !== false ? (
            <label className="feegow-field feegow-field-span-full">
              <span>CID-10</span>
              <Cid10Picker
                value={cid10Code}
                onChange={setCid10Code}
              />
            </label>
          ) : null}

          {config.contentTemplates && config.contentTemplates.length > 0 ? (
            <div className="feegow-field feegow-field-span-full">
              <span>Modelos</span>
              <div className="pep-template-chips compact">
                {config.contentTemplates.map((template) => (
                  <button
                    key={template}
                    type="button"
                    className="pep-template-chip"
                    onClick={() => setContent((prev) => appendSnippet(prev, template))}
                    disabled={editorSigned || saving}
                  >
                    + {template.length > 42 ? `${template.slice(0, 42)}…` : template}
                  </button>
                ))}
              </div>
            </div>
          ) : null}

          <label className="feegow-field feegow-field-span-full">
            <span>Conteúdo<span className="feegow-req">*</span></span>
            <textarea
              rows={10}
              value={content}
              onChange={(e) => setContent(e.target.value)}
              disabled={editorSigned || saving}
              placeholder={config.contentPlaceholder}
            />
          </label>

          {editorSigned ? (
            <p className="feegow-patient-section-empty feegow-field-span-full">
              Este registro foi assinado digitalmente e não pode ser alterado.
            </p>
          ) : null}

          {error ? <div className="alert alert-error feegow-field-span-full">{error}</div> : null}

          <div className="feegow-form-actions">
            <button type="button" className="feegow-form-btn-cancel" onClick={closeEditor} disabled={saving}>
              Cancelar
            </button>
            {!editorSigned ? (
              <button type="submit" className="feegow-patient-save-btn" disabled={saving}>
                {saving ? 'Salvando…' : 'Salvar'}
              </button>
            ) : null}
          </div>
        </form>
      </Modal>
    </>
  );
}

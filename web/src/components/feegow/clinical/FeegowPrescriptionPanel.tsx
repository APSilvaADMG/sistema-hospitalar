import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  entryTypeToNumber,
  type MedicalRecordEntryDto,
  type MedicationCatalogDto,
  type PatientDetailDto,
} from '../../../api/client';
import { useAuth } from '../../../auth/AuthContext';
import { useAdministrationRoutes } from '../../../hooks/useAdministrationRoutes';
import { Modal } from '../../Modal';
import { PrescriptionItemsEditor } from '../../PrescriptionItemsEditor';
import { Cid10Picker } from '../../Cid10Picker';
import {
  buildPrescriptionBlock,
  emptyPrescriptionLine,
  type PrescriptionLineItem,
} from '../../../utils/prescriptionFormat';
import { formatBrDateTime } from '../../../utils/dateUtils';
import { printPrescriptionReport } from '../../../utils/printTemplates';

const PRESCRIPTION_ENTRY_TYPE = 3;

type Props = {
  patientId: string;
  patient: PatientDetailDto;
};

type EditorState = {
  mode: 'create' | 'edit';
  entry?: MedicalRecordEntryDto;
};

function isPrescriptionEntry(entryType: number | string): boolean {
  return entryTypeToNumber(entryType) === PRESCRIPTION_ENTRY_TYPE;
}

export function FeegowPrescriptionPanel({ patientId, patient }: Props) {
  const { hasPermission } = useAuth();
  const canWrite = hasPermission('pep.write');
  const canPrint = hasPermission('pep.read');
  const { routes, loading: routesLoading, error: routesError } = useAdministrationRoutes();

  const [entries, setEntries] = useState<MedicalRecordEntryDto[]>([]);
  const [medications, setMedications] = useState<MedicationCatalogDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [cid10Code, setCid10Code] = useState('');
  const [freeText, setFreeText] = useState('');
  const [prescriptionItems, setPrescriptionItems] = useState<PrescriptionLineItem[]>([]);
  const [medSearch, setMedSearch] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [recordNumber, setRecordNumber] = useState<string | undefined>();

  const loadEntries = useCallback(async () => {
    setLoading(true);
    try {
      const digital = await api.getDigitalRecord(patientId);
      const list = (digital.record.entries ?? []).filter((entry) => isPrescriptionEntry(entry.entryType));
      setEntries(list);
      setRecordNumber(digital.record.recordNumber);
    } catch {
      setEntries([]);
    } finally {
      setLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    loadEntries().catch(console.error);
  }, [loadEntries]);

  useEffect(() => {
    api.getMedications()
      .then(setMedications)
      .catch(() => setMedications([]));
  }, []);

  const filteredMedications = useMemo(() => {
    const q = medSearch.trim().toLowerCase();
    if (!q) return medications.slice(0, 40);
    return medications
      .filter((med) => med.name.toLowerCase().includes(q) || med.activeIngredient?.toLowerCase().includes(q))
      .slice(0, 40);
  }, [medications, medSearch]);

  function openCreate() {
    setEditor({ mode: 'create' });
    setCid10Code('');
    setFreeText('');
    setPrescriptionItems([]);
    setMedSearch('');
    setError('');
  }

  function openEdit(entry: MedicalRecordEntryDto) {
    setEditor({ mode: 'edit', entry });
    setCid10Code(entry.cid10Code ?? '');
    setFreeText(entry.content ?? '');
    setPrescriptionItems([]);
    setMedSearch('');
    setError('');
  }

  function closeEditor() {
    setEditor(null);
    setError('');
  }

  function toggleMedication(med: MedicationCatalogDto) {
    setPrescriptionItems((prev) => {
      const exists = prev.some((item) => item.medicationId === med.id);
      if (exists) return prev.filter((item) => item.medicationId !== med.id);
      return [...prev, emptyPrescriptionLine(med, routes)];
    });
  }

  function buildContent(): string {
    const block = buildPrescriptionBlock(prescriptionItems, routes);
    const extra = freeText.trim();
    return [extra, block].filter(Boolean).join('\n\n');
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!canWrite || !editor) return;

    if (prescriptionItems.length === 0) {
      setError('Selecione ao menos um medicamento.');
      return;
    }
    if (prescriptionItems.some((item) => !item.administrationRouteCode)) {
      setError('Informe a via de administração de todos os medicamentos.');
      return;
    }

    const content = buildContent();
    if (!content.trim()) {
      setError('A prescrição está vazia.');
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
        entryType: PRESCRIPTION_ENTRY_TYPE,
        content,
        cid10Code: cid10Code.trim() || undefined,
      };

      if (editor.mode === 'create') {
        await api.addMedicalRecordEntry(patientId, payload);
      } else if (editor.entry) {
        await api.updateMedicalRecordEntry(patientId, editor.entry.id, payload);
      }

      closeEditor();
      await loadEntries();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar a prescrição.');
    } finally {
      setSaving(false);
    }
  }

  const editorSigned = editor?.mode === 'edit' && editor.entry?.isSigned;

  return (
    <>
      <div className="feegow-patient-section-toolbar">
        {canWrite ? (
          <button type="button" className="feegow-patient-save-btn" onClick={openCreate}>
            + NOVA PRESCRIÇÃO
          </button>
        ) : null}
        <Link to="/pep/vias-administracao" className="feegow-patient-section-pep-link" style={{ marginLeft: 12 }}>
          Catálogo de vias
        </Link>
      </div>

      {routesError ? (
        <p className="feegow-patient-section-empty">{routesError}</p>
      ) : null}

      {loading ? (
        <p className="feegow-patient-section-empty">Carregando…</p>
      ) : entries.length === 0 ? (
        <div className="appt-empty">
          <div className="appt-empty-icon">💊</div>
          <h3>Nenhuma prescrição registrada</h3>
          <p>{canWrite ? 'Use o botão acima para prescrever medicamentos vinculados ao PEP.' : 'Sem prescrições para este paciente.'}</p>
        </div>
      ) : (
        <table className="feegow-patient-section-table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Profissional</th>
              <th>CID</th>
              <th>Resumo</th>
              {(canWrite || canPrint) ? <th className="feegow-patient-section-actions-col">Ações</th> : null}
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.id}>
                <td>{formatBrDateTime(entry.createdAt)}</td>
                <td>{entry.professionalName ?? '—'}</td>
                <td>{entry.cid10Code ?? '—'}</td>
                <td>{entry.content?.slice(0, 120) || '—'}</td>
                {(canWrite || canPrint) ? (
                  <td className="feegow-patient-section-actions-col">
                    {canPrint ? (
                      <button
                        type="button"
                        className="feegow-patient-entry-edit-btn"
                        onClick={() => printPrescriptionReport(patient, entry, recordNumber)}
                        title="Imprimir prescrição"
                      >
                        Imprimir
                      </button>
                    ) : null}
                    {canWrite ? (
                      <button
                        type="button"
                        className="feegow-patient-entry-edit-btn"
                        onClick={() => openEdit(entry)}
                        disabled={entry.isSigned}
                        title={entry.isSigned ? 'Registro assinado' : 'Editar prescrição'}
                      >
                        Editar
                      </button>
                    ) : null}
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <p className="feegow-patient-section-pep-link">
        <Link to={`/pacientes/${patientId}/prontuario/prescricao`}>Abrir prescrição no PEP</Link>
      </p>

      <Modal
        open={editor != null}
        onClose={() => { if (!saving) closeEditor(); }}
        title={editor?.mode === 'create' ? 'Nova prescrição' : 'Editar prescrição'}
        subtitle={patient.fullName}
        width="lg"
      >
        <form className="feegow-form-grid" onSubmit={handleSubmit}>
          <label className="feegow-field feegow-field-span-full">
            <span>CID-10</span>
            <Cid10Picker
              value={cid10Code}
              onChange={setCid10Code}
            />
          </label>

          <label className="feegow-field feegow-field-span-full">
            <span>Buscar medicamento</span>
            <input
              value={medSearch}
              onChange={(e) => setMedSearch(e.target.value)}
              placeholder="Nome ou princípio ativo…"
              disabled={editorSigned || saving}
            />
          </label>

          <div className="feegow-field feegow-field-span-full">
            <span>Medicamentos</span>
            <div className="exam-grid">
              {filteredMedications.map((med) => (
                <label
                  key={med.id}
                  className={`exam-chip${prescriptionItems.some((item) => item.medicationId === med.id) ? ' selected' : ''}`}
                >
                  <input
                    type="checkbox"
                    checked={prescriptionItems.some((item) => item.medicationId === med.id)}
                    onChange={() => toggleMedication(med)}
                    disabled={editorSigned || saving}
                  />
                  <span>
                    <strong>{med.name}</strong>
                    {med.defaultDosage ? <small>{med.defaultDosage}</small> : null}
                  </span>
                </label>
              ))}
              {filteredMedications.length === 0 ? (
                <p className="feegow-patient-section-empty">Nenhum medicamento encontrado.</p>
              ) : null}
            </div>
          </div>

          <div className="feegow-field feegow-field-span-full">
            <PrescriptionItemsEditor
              items={prescriptionItems}
              routes={routes}
              routesLoading={routesLoading}
              onChange={setPrescriptionItems}
              disabled={editorSigned || saving}
              compact
            />
          </div>

          <label className="feegow-field feegow-field-span-full">
            <span>Orientações complementares</span>
            <textarea
              rows={4}
              value={freeText}
              onChange={(e) => setFreeText(e.target.value)}
              disabled={editorSigned || saving}
              placeholder="Retorno, cuidados, observações…"
            />
          </label>

          {editorSigned ? (
            <p className="feegow-patient-section-empty feegow-field-span-full">
              Esta prescrição foi assinada digitalmente e não pode ser alterada.
            </p>
          ) : null}

          {error ? <div className="alert alert-error feegow-field-span-full">{error}</div> : null}

          <div className="feegow-form-actions">
            <button type="button" className="feegow-form-btn-cancel" onClick={closeEditor} disabled={saving}>
              Cancelar
            </button>
            {!editorSigned ? (
              <button type="submit" className="feegow-patient-save-btn" disabled={saving}>
                {saving ? 'Salvando…' : 'Salvar prescrição'}
              </button>
            ) : null}
          </div>
        </form>
      </Modal>
    </>
  );
}

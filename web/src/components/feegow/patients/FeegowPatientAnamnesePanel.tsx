import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  entryTypeToNumber,
  formatEntryTypeLabel,
  type MedicalRecordEntryDto,
  type PatientDetailDto,
} from '../../../api/client';
import { useAuth } from '../../../auth/AuthContext';
import { ClinicalEntryForm, type ClinicalEntryPayload } from '../../ClinicalEntryForm';
import { Modal } from '../../Modal';
import { formatBrDateTime } from '../../../utils/dateUtils';
import { saveMedicalEntry } from '../../../offline/pepActions';

const ANAMNESIS_ENTRY_TYPES = [1, 2] as const;

type Props = {
  patientId: string;
  patient: PatientDetailDto;
};

type EditorState = {
  mode: 'create' | 'edit';
  entry?: MedicalRecordEntryDto;
};

function isAnamnesisEntry(entryType: number | string): boolean {
  const type = entryTypeToNumber(entryType);
  return type === 1 || type === 2;
}

export function FeegowPatientAnamnesePanel({ patientId, patient }: Props) {
  const { hasPermission, user } = useAuth();
  const canWrite = hasPermission('pep.write');

  const [entries, setEntries] = useState<MedicalRecordEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [editor, setEditor] = useState<EditorState | null>(null);
  const [entryFormKey, setEntryFormKey] = useState(0);
  const [signOnSave, setSignOnSave] = useState(false);
  const [signatureImage, setSignatureImage] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  const loadEntries = useCallback(async () => {
    setLoading(true);
    try {
      const digital = await api.getDigitalRecord(patientId);
      const list = (digital.record.entries ?? []).filter((entry) => isAnamnesisEntry(entry.entryType));
      setEntries(list);
    } catch {
      setEntries([]);
    } finally {
      setLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    loadEntries().catch(console.error);
  }, [loadEntries]);

  function openCreate() {
    setEditor({ mode: 'create' });
    setSignOnSave(false);
    setSignatureImage(null);
    setError('');
    setEntryFormKey((k) => k + 1);
  }

  function openEdit(entry: MedicalRecordEntryDto) {
    setEditor({ mode: 'edit', entry });
    setSignOnSave(false);
    setSignatureImage(null);
    setError('');
    setEntryFormKey((k) => k + 1);
  }

  function closeEditor() {
    if (saving) return;
    setEditor(null);
    setError('');
    setSignatureImage(null);
  }

  async function handleEntryPayload(payload: ClinicalEntryPayload) {
    if (!canWrite || !editor) return;

    const trimmed = payload.content.trim();
    if (!trimmed) {
      setError('Informe o conteúdo do registro.');
      return;
    }

    if (editor.mode === 'edit' && editor.entry?.isSigned) {
      setError('Registro assinado não pode ser alterado.');
      return;
    }

    if (signOnSave && !signatureImage) {
      setError('Desenhe sua assinatura digital para concluir o registro.');
      return;
    }

    const professionalId = user?.professionalId;
    if (signOnSave && !professionalId) {
      setError('Seu usuário precisa estar vinculado a um profissional para assinar.');
      return;
    }

    setSaving(true);
    setError('');
    try {
      if (editor.mode === 'create') {
        await saveMedicalEntry(patientId, {
          entryType: payload.entryType,
          content: trimmed,
          cid10Code: payload.cid10Code || undefined,
          professionalId: signOnSave ? professionalId : undefined,
          signatureImage: signOnSave ? signatureImage ?? undefined : undefined,
        });
      } else if (editor.entry) {
        await api.updateMedicalRecordEntry(patientId, editor.entry.id, {
          entryType: payload.entryType,
          content: trimmed,
          cid10Code: payload.cid10Code || undefined,
        });
      }

      closeEditor();
      await loadEntries();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível salvar o registro.');
    } finally {
      setSaving(false);
    }
  }

  const editorTitle = editor?.mode === 'create' ? 'Novo registro clínico' : 'Editar registro clínico';
  const editorSigned = editor?.mode === 'edit' && Boolean(editor.entry?.isSigned);
  const initialEntryType = editor?.entry
    ? entryTypeToNumber(editor.entry.entryType)
    : 1;

  return (
    <>
      <div className="feegow-patient-section-toolbar">
        {canWrite ? (
          <button type="button" className="feegow-patient-save-btn" onClick={openCreate}>
            + NOVO REGISTRO
          </button>
        ) : null}
      </div>

      {loading ? (
        <p className="feegow-patient-section-empty">Carregando…</p>
      ) : entries.length === 0 ? (
        <p className="feegow-patient-section-empty">Nenhum registro nesta seção.</p>
      ) : (
        <table className="feegow-patient-section-table">
          <thead>
            <tr>
              <th>Data</th>
              <th>Tipo</th>
              <th>Profissional</th>
              <th>Resumo</th>
              {canWrite ? <th className="feegow-patient-section-actions-col">Ações</th> : null}
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.id}>
                <td>{formatBrDateTime(entry.createdAt)}</td>
                <td>{formatEntryTypeLabel(entry.entryType)}</td>
                <td>{entry.professionalName ?? '—'}</td>
                <td>{entry.content?.slice(0, 120) || '—'}</td>
                {canWrite ? (
                  <td className="feegow-patient-section-actions-col">
                    <button
                      type="button"
                      className="feegow-patient-entry-edit-btn"
                      onClick={() => openEdit(entry)}
                      title={entry.isSigned ? 'Visualizar registro assinado' : 'Editar registro'}
                    >
                      {entry.isSigned ? 'Ver' : 'Editar'}
                    </button>
                  </td>
                ) : null}
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <p className="feegow-patient-section-pep-link">
        <Link to={`/pacientes/${patientId}/prontuario/resumo`}>Abrir prontuário completo (PEP)</Link>
      </p>

      <Modal
        open={editor != null}
        onClose={closeEditor}
        title={editorTitle}
        subtitle={`${patient.fullName} — anamnese estruturada ou evolução SOAP`}
        width="lg"
      >
        {error ? <div className="alert alert-error" style={{ marginBottom: 12 }}>{error}</div> : null}

        {editor ? (
          <ClinicalEntryForm
            key={entryFormKey}
            catalog={null}
            patient={patient}
            allowedEntryTypes={[...ANAMNESIS_ENTRY_TYPES]}
            initialEntryType={initialEntryType}
            editContent={editor.mode === 'edit' ? editor.entry?.content ?? '' : undefined}
            editCid10Code={editor.mode === 'edit' ? editor.entry?.cid10Code ?? '' : undefined}
            readOnly={editorSigned}
            signOnSave={signOnSave}
            signatureImage={signatureImage}
            onSignOnSaveChange={setSignOnSave}
            onSignatureImageChange={setSignatureImage}
            onCancel={closeEditor}
            onSubmit={handleEntryPayload}
            submitting={saving}
            submitLabel={editor.mode === 'create' ? 'Salvar registro' : 'Atualizar registro'}
            signatureLayoutKey={editor != null ? entryFormKey : 'closed'}
          />
        ) : null}
      </Modal>
    </>
  );
}

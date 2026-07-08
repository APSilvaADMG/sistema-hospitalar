import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { appointmentStatusLabel, appointmentStatusLabels, isAppointmentStatus, type AppointmentDto } from '../../../api/client';
import { formatBrTime } from '../../../utils/dateUtils';
import { Modal } from '../../Modal';

type Props = {
  open: boolean;
  date: string;
  appointments: AppointmentDto[];
  professionalName?: string;
  onClose: () => void;
  onApply: (appointmentIds: string[], status: number) => Promise<void>;
};

export function FeegowAgendaBulkModal({
  open,
  date,
  appointments,
  professionalName,
  onClose,
  onApply,
}: Props) {
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [status, setStatus] = useState(2);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const editable = useMemo(
    () => appointments.filter((a) => !isAppointmentStatus(a.status, 4, 5)),
    [appointments],
  );

  useEffect(() => {
    if (open) {
      setSelected(new Set(editable.map((a) => a.id)));
      setStatus(2);
      setError('');
    }
  }, [open, editable]);

  function toggleAll(checked: boolean) {
    setSelected(checked ? new Set(editable.map((a) => a.id)) : new Set());
  }

  function toggleOne(id: string) {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (selected.size === 0) {
      setError('Selecione ao menos um agendamento.');
      return;
    }
    setLoading(true);
    setError('');
    try {
      await onApply([...selected], status);
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao aplicar alterações.');
    } finally {
      setLoading(false);
    }
  }

  const formattedDate = new Date(`${date}T12:00:00`).toLocaleDateString('pt-BR');

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Alterações em massa"
      subtitle={
        professionalName
          ? `${professionalName} · ${formattedDate}`
          : formattedDate
      }
      width="lg"
    >
      <form onSubmit={handleSubmit}>
        <div className="feegow-bulk-toolbar">
          <label className="feegow-checkbox-row">
            <input
              type="checkbox"
              checked={selected.size === editable.length && editable.length > 0}
              onChange={(e) => toggleAll(e.target.checked)}
            />
            Selecionar todos ({editable.length})
          </label>
          <label className="feegow-field" style={{ minWidth: 200 }}>
            <span>Novo status</span>
            <select
              id="bulkStatus"
              value={status}
              onChange={(e) => setStatus(Number(e.target.value))}
            >
              {Object.entries(appointmentStatusLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </label>
        </div>

        <div className="feegow-bulk-list">
          {editable.length === 0 ? (
            <p className="feegow-agenda-side-empty">Nenhum agendamento editável neste dia.</p>
          ) : (
            editable.map((appt) => (
              <label key={appt.id} className="feegow-bulk-item">
                <input
                  type="checkbox"
                  checked={selected.has(appt.id)}
                  onChange={() => toggleOne(appt.id)}
                />
                <span>
                  <strong>{formatBrTime(appt.scheduledAt)}</strong>
                  {' — '}
                  {appt.patientName}
                  <small>{appointmentStatusLabel(appt.status)}</small>
                </span>
              </label>
            ))
          )}
        </div>

        {error ? <div className="alert alert-error" style={{ marginTop: 12 }}>{error}</div> : null}

        <div className="feegow-form-actions" style={{ marginTop: 16 }}>
          <button type="button" className="feegow-form-btn-cancel" onClick={onClose} disabled={loading}>
            Cancelar
          </button>
          <button type="submit" className="feegow-patient-save-btn" disabled={loading || editable.length === 0}>
            {loading ? 'Aplicando…' : `Aplicar em ${selected.size} agendamento(s)`}
          </button>
        </div>
      </form>
    </Modal>
  );
}

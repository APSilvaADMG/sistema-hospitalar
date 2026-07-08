import { useState, type FormEvent } from 'react';
import { Modal } from '../../Modal';

type Props = {
  open: boolean;
  date: string;
  professionalName?: string;
  onClose: () => void;
  onConfirm: (payload: {
    startTime: string;
    endTime: string;
    reason: string;
    cancelExisting: boolean;
    blockFullDay: boolean;
  }) => Promise<void>;
};

export function FeegowAgendaBlockModal({
  open,
  date,
  professionalName,
  onClose,
  onConfirm,
}: Props) {
  const [startTime, setStartTime] = useState('09:00');
  const [endTime, setEndTime] = useState('10:00');
  const [reason, setReason] = useState('');
  const [cancelExisting, setCancelExisting] = useState(false);
  const [blockFullDay, setBlockFullDay] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    if (!reason.trim()) {
      setError('Informe o motivo do bloqueio.');
      return;
    }
    if (!blockFullDay && startTime >= endTime) {
      setError('O horário final deve ser após o inicial.');
      return;
    }
    setLoading(true);
    try {
      await onConfirm({
        startTime,
        endTime,
        reason: reason.trim(),
        cancelExisting,
        blockFullDay,
      });
      onClose();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível bloquear a agenda.');
    } finally {
      setLoading(false);
    }
  }

  const formattedDate = new Date(`${date}T12:00:00`).toLocaleDateString('pt-BR');

  return (
    <Modal
      open={open}
      onClose={onClose}
      title="Bloqueio de agenda"
      subtitle={
        professionalName
          ? `${professionalName} · ${formattedDate}`
          : formattedDate
      }
      width="lg"
    >
      <form className="feegow-form-grid" onSubmit={handleSubmit}>
        <div className="feegow-field feegow-field-span-full">
          <label className="feegow-check-pill">
            <input
              type="checkbox"
              checked={blockFullDay}
              onChange={(e) => setBlockFullDay(e.target.checked)}
            />
            <span>Bloquear o dia inteiro (cancela consultas ativas e notifica pacientes)</span>
          </label>
        </div>

        {!blockFullDay ? (
          <>
            <label className="feegow-field feegow-field-grow2">
              <span>Horário inicial</span>
              <input
                id="blockStart"
                type="time"
                required
                value={startTime}
                onChange={(e) => setStartTime(e.target.value)}
              />
            </label>
            <label className="feegow-field feegow-field-grow2">
              <span>Horário final</span>
              <input
                id="blockEnd"
                type="time"
                required
                value={endTime}
                onChange={(e) => setEndTime(e.target.value)}
              />
            </label>
            <div className="feegow-field feegow-field-span-full">
              <label className="feegow-check-pill">
                <input
                  type="checkbox"
                  checked={cancelExisting}
                  onChange={(e) => setCancelExisting(e.target.checked)}
                />
                <span>Cancelar consultas já agendadas neste período</span>
              </label>
            </div>
          </>
        ) : null}

        <label className="feegow-field feegow-field-span-full">
          <span>Motivo<span className="feegow-req">*</span></span>
          <input
            id="blockReason"
            required
            placeholder="Ex.: reunião, congresso, folga..."
            value={reason}
            onChange={(e) => setReason(e.target.value)}
          />
        </label>

        {error ? <div className="alert alert-error">{error}</div> : null}

        <div className="feegow-form-actions">
          <button type="button" className="feegow-form-btn-cancel" onClick={onClose} disabled={loading}>
            Cancelar
          </button>
          <button type="submit" className="feegow-patient-save-btn" disabled={loading}>
            {loading ? 'Aplicando…' : 'Confirmar bloqueio'}
          </button>
        </div>
      </form>
    </Modal>
  );
}

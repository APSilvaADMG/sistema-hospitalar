import { useEffect, useMemo, useState } from 'react';
import { isAppointmentStatus, type AppointmentDto, type ProfessionalDto } from '../../../api/client';
import { useAuth } from '../../../auth/AuthContext';
import { FeegowMiniCalendar } from './FeegowMiniCalendar';
import { formatBrTime } from '../../../utils/dateUtils';

export const FEEGOW_AGENDA_SIDEBAR_HOST_ID = 'feegow-agenda-sidebar-host';

export type FeegowAgendaSidebarVariant = 'daily' | 'weekly' | 'multiple' | 'equipment';

type FeegowAgendaSidebarProps = {
  variant?: FeegowAgendaSidebarVariant;
  date: string;
  onDateChange: (date: string) => void;
  appointments: AppointmentDto[];
  professionals: ProfessionalDto[];
  selectedProfessionalId: string;
  onProfessionalChange: (id: string) => void;
  onRefresh: () => void;
  onEncaixe?: () => void;
  onBlock?: () => void;
  onBulkEdit?: () => void;
  onGrade?: () => void;
  onPrintReport?: () => void;
  canManage?: boolean;
  onlyEmptySlots?: boolean;
  onOnlyEmptySlotsChange?: (value: boolean) => void;
  selectedEquipmentId?: string;
  onEquipmentChange?: (id: string) => void;
};

const EQUIPMENT_OPTIONS = [
  { id: '', label: 'Selecione' },
  { id: 'eq-01', label: 'Ultrassom' },
  { id: 'eq-02', label: 'Raio-X' },
  { id: 'eq-03', label: 'Eletrocardiógrafo' },
];

function notesStorageKey(professionalId: string, date: string) {
  return `iasgh-agenda-notes:${professionalId || 'all'}:${date}`;
}

export function FeegowAgendaSidebar({
  variant = 'daily',
  date,
  onDateChange,
  appointments,
  professionals,
  selectedProfessionalId,
  onProfessionalChange,
  onRefresh,
  onEncaixe,
  onBlock,
  onBulkEdit,
  onGrade,
  onPrintReport,
  canManage = false,
  onlyEmptySlots = false,
  onOnlyEmptySlotsChange,
  selectedEquipmentId = '',
  onEquipmentChange,
}: FeegowAgendaSidebarProps) {
  const { user } = useAuth();
  const [sideTab, setSideTab] = useState<'notas' | 'espera'>('notas');
  const [notes, setNotes] = useState('');

  const displayProfessional = useMemo(() => {
    if (selectedProfessionalId) {
      return professionals.find((p) => p.id === selectedProfessionalId);
    }
    if (user?.professionalId) {
      return professionals.find((p) => p.id === user.professionalId);
    }
    return professionals[0];
  }, [professionals, selectedProfessionalId, user?.professionalId]);

  const activeProfessionalId = selectedProfessionalId || displayProfessional?.id || '';

  const waitingAppointments = useMemo(
    () => appointments
      .filter((a) => isAppointmentStatus(a.status, 1))
      .sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt)),
    [appointments],
  );

  useEffect(() => {
    try {
      const saved = localStorage.getItem(notesStorageKey(activeProfessionalId, date));
      setNotes(saved ?? '');
    } catch {
      setNotes('');
    }
  }, [activeProfessionalId, date]);

  useEffect(() => {
    try {
      localStorage.setItem(notesStorageKey(activeProfessionalId, date), notes);
    } catch {
      /* ignore */
    }
  }, [activeProfessionalId, date, notes]);

  const showProfessional = variant === 'daily' || variant === 'weekly';
  const showNotesTabs = variant !== 'equipment';
  const showBulk = variant === 'daily';
  const showPrint = variant === 'daily';
  const showRefresh = variant !== 'equipment';

  return (
    <div className="feegow-agenda-sidebar">
      {showProfessional ? (
        <div className="feegow-agenda-professional">
          <p className="feegow-agenda-side-label">PROFISSIONAL</p>
          <div className="feegow-agenda-prof-card">
            <span className="feegow-agenda-prof-avatar" aria-hidden>👤</span>
            <select
              className="feegow-agenda-prof-select"
              value={activeProfessionalId}
              onChange={(e) => onProfessionalChange(e.target.value)}
            >
              {professionals.length === 0 ? (
                <option value="">Nenhum profissional</option>
              ) : (
                professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))
              )}
            </select>
            <button type="button" className="feegow-agenda-prof-info" title="Informações" aria-label="Informações">i</button>
          </div>
          {displayProfessional ? (
            <p className="feegow-agenda-prof-name">{displayProfessional.fullName}</p>
          ) : null}
        </div>
      ) : null}

      {variant === 'equipment' ? (
        <div className="feegow-agenda-professional">
          <p className="feegow-agenda-side-label">EQUIPAMENTO</p>
          <div className="feegow-agenda-prof-card">
            <span className="feegow-agenda-prof-avatar" aria-hidden>🖥</span>
            <select
              className="feegow-agenda-prof-select"
              value={selectedEquipmentId}
              onChange={(e) => onEquipmentChange?.(e.target.value)}
            >
              {EQUIPMENT_OPTIONS.map((eq) => (
                <option key={eq.id || 'empty'} value={eq.id}>{eq.label}</option>
              ))}
            </select>
          </div>
        </div>
      ) : null}

      <div className="feegow-agenda-actions-panel">
      <div className={`feegow-agenda-actions-grid${variant === 'weekly' || variant === 'equipment' ? ' feegow-agenda-actions-compact' : ''}`}>
        <button type="button" className="feegow-agenda-action feegow-agenda-action-span" title="Grade" onClick={onGrade}>
          <span className="feegow-agenda-action-icon" aria-hidden>▦</span>
          Grade
        </button>
        {showPrint ? (
          <button
            type="button"
            className="feegow-agenda-action"
            title="Imprimir relatório"
            onClick={() => (onPrintReport ? onPrintReport() : window.print())}
          >
            <span className="feegow-agenda-action-icon" aria-hidden>🖨</span>
            Imprimir
          </button>
        ) : null}
        <button
          type="button"
          className="feegow-agenda-action"
          title="Encaixe"
          onClick={onEncaixe}
          disabled={!canManage || !onEncaixe}
        >
          <span className="feegow-agenda-action-icon" aria-hidden>↗</span>
          Encaixe
        </button>
        <button
          type="button"
          className="feegow-agenda-action"
          title="Bloqueio"
          onClick={onBlock}
          disabled={!canManage || !onBlock}
        >
          <span className="feegow-agenda-action-icon" aria-hidden>🔒</span>
          Bloqueio
        </button>
      </div>
      </div>

      {showBulk ? (
        <button
          type="button"
          className="feegow-agenda-action-wide"
          onClick={onBulkEdit}
          disabled={!canManage || !onBulkEdit}
        >
          Alterações em massa
        </button>
      ) : null}

      {variant === 'multiple' ? (
        <label className="feegow-checkbox-row">
          <input
            type="checkbox"
            checked={onlyEmptySlots}
            onChange={(e) => onOnlyEmptySlotsChange?.(e.target.checked)}
          />
          <span>Somente horários vazios</span>
        </label>
      ) : null}

      <FeegowMiniCalendar selectedDate={date} onSelectDate={onDateChange} />

      {showRefresh ? (
        <button type="button" className="feegow-agenda-refresh" onClick={onRefresh}>
          <span className="feegow-agenda-action-icon" aria-hidden>↻</span>
          Atualizar Calendário
        </button>
      ) : null}

      {showNotesTabs ? (
        <>
          <div className="feegow-agenda-side-tabs">
            <button
              type="button"
              className={sideTab === 'notas' ? 'active' : ''}
              onClick={() => setSideTab('notas')}
            >
              <span aria-hidden>📝</span> Notas
            </button>
            <button
              type="button"
              className={sideTab === 'espera' ? 'active' : ''}
              onClick={() => setSideTab('espera')}
            >
              <span aria-hidden>⏱</span> Espera
            </button>
          </div>

          <div className="feegow-agenda-side-panel">
            {sideTab === 'notas' ? (
              <label className="feegow-field feegow-field-notes">
                <span>Notas do dia</span>
                <textarea
                  placeholder="Incluir notas para este dia..."
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  rows={5}
                />
              </label>
            ) : waitingAppointments.length === 0 ? (
              <p className="feegow-agenda-side-empty">Nenhum paciente em espera neste dia.</p>
            ) : (
              <ul className="feegow-agenda-wait-list">
                {waitingAppointments.map((appt) => (
                  <li key={appt.id} className="feegow-agenda-wait-item">
                    <strong>{appt.patientName}</strong>
                    <span>{formatBrTime(appt.scheduledAt)}</span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </>
      ) : null}
    </div>
  );
}

import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import type { ConsultingRoomScheduleDto } from '../../../api/client';
import { getInstitutionShortName } from '../../../config/iasghBranding';
import { Modal } from '../../Modal';

const DAY_ORDER = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const;

const DAY_LABELS: Record<string, string> = {
  Monday: 'Segunda-feira',
  Tuesday: 'Terça-feira',
  Wednesday: 'Quarta-feira',
  Thursday: 'Quinta-feira',
  Friday: 'Sexta-feira',
  Saturday: 'Sábado',
  Sunday: 'Domingo',
};

type Props = {
  open: boolean;
  professionalName?: string;
  professionalId?: string;
  schedules: ConsultingRoomScheduleDto[];
  onClose: () => void;
};

type GradeRow = {
  day: string;
  room: string;
  startTime: string;
  endTime: string;
};

function defaultGradeRows(): GradeRow[] {
  const room = `CONSULTÓRIO 01 (${getInstitutionShortName()})`;
  return ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'].map((day) => ({
    day: DAY_LABELS[day],
    room,
    startTime: '08:00',
    endTime: '12:00',
  })).concat(
    ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday'].map((day) => ({
      day: DAY_LABELS[day],
      room,
      startTime: '14:00',
      endTime: '18:00',
    })),
  );
}

export function FeegowAgendaGradeModal({
  open,
  professionalName,
  professionalId,
  schedules,
  onClose,
}: Props) {
  const rows = useMemo(() => {
    if (!professionalId) return defaultGradeRows();

    const profSchedules = schedules
      .filter((s) => s.professionalId === professionalId)
      .sort((a, b) => DAY_ORDER.indexOf(a.dayOfWeek as typeof DAY_ORDER[number])
        - DAY_ORDER.indexOf(b.dayOfWeek as typeof DAY_ORDER[number]));

    if (profSchedules.length === 0) return defaultGradeRows();

    return profSchedules.map((s) => ({
      day: DAY_LABELS[s.dayOfWeek] ?? s.dayOfWeek,
      room: `${s.roomName} (${getInstitutionShortName()})`,
      startTime: s.startTime?.slice(0, 5) ?? '—',
      endTime: s.endTime?.slice(0, 5) ?? '—',
    }));
  }, [professionalId, schedules]);

  return (
    <Modal
      open={open}
      title="Grade de atendimento"
      subtitle={professionalName ?? 'Selecione um profissional na barra lateral'}
      onClose={onClose}
      width="lg"
      overlayClassName="feegow-grade-modal-overlay"
    >
      <div className="feegow-grade-modal">
        <table className="feegow-agenda-data-table feegow-grade-table">
          <thead>
            <tr>
              <th>Dia</th>
              <th>Local</th>
              <th>Início</th>
              <th>Término</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((row, index) => (
              <tr key={`${row.day}-${row.startTime}-${index}`}>
                <td>{row.day}</td>
                <td>{row.room}</td>
                <td>{row.startTime}</td>
                <td>{row.endTime}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <p className="feegow-grade-hint">
          Horários exibidos conforme a escala cadastrada para o profissional.
        </p>
        <div className="modal-actions feegow-grade-actions">
          <Link to="/ambulatorio/consultorios" className="btn btn-secondary" onClick={onClose}>
            Gerenciar consultórios
          </Link>
          <button type="button" className="btn" onClick={onClose}>Fechar</button>
        </div>
      </div>
    </Modal>
  );
}

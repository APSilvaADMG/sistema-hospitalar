import type { AppointmentDto, ProfessionalDto } from '../../../api/client';
import type { AgendaBlockSlot } from '../../../utils/agendaBlocks';
import { FeegowAgendaLayout } from './FeegowAgendaLayout';
import { FeegowAgendaSchedule } from './FeegowAgendaSchedule';

type FeegowDailyAgendaProps = {
  date: string;
  onDateChange: (date: string) => void;
  appointments: AppointmentDto[];
  professionals: ProfessionalDto[];
  selectedProfessionalId: string;
  onProfessionalChange: (id: string) => void;
  onRefresh: () => void;
  roomLabel?: string;
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onClinicalData?: (appt: AppointmentDto) => void;
  onCreateAt?: (slotIso: string) => void;
  onEncaixe?: () => void;
  onBlock?: () => void;
  onBulkEdit?: () => void;
  onGrade?: () => void;
  onPrintReport?: () => void;
  blockedSlots?: AgendaBlockSlot[];
  error?: string;
  success?: string;
};

export function FeegowDailyAgenda(props: FeegowDailyAgendaProps) {
  const {
    date,
    onDateChange,
    appointments,
    professionals,
    selectedProfessionalId,
    onProfessionalChange,
    onRefresh,
    roomLabel,
    canManage,
    onStatusChange,
    onClinicalData,
    onCreateAt,
    onEncaixe,
    onBlock,
    onBulkEdit,
    onGrade,
    onPrintReport,
    blockedSlots,
    error,
    success,
  } = props;

  return (
    <FeegowAgendaLayout
      date={date}
      onDateChange={onDateChange}
      appointments={appointments}
      professionals={professionals}
      selectedProfessionalId={selectedProfessionalId}
      onProfessionalChange={onProfessionalChange}
      onRefresh={onRefresh}
      canManage={canManage}
      onEncaixe={onEncaixe}
      onBlock={onBlock}
      onBulkEdit={onBulkEdit}
      onGrade={onGrade}
      onPrintReport={onPrintReport}
      error={error}
      success={success}
    >
      <FeegowAgendaSchedule
        appointments={appointments}
        date={date}
        roomLabel={roomLabel}
        blockedSlots={blockedSlots}
        canManage={canManage}
        onStatusChange={onStatusChange}
        onClinicalData={onClinicalData}
        onCreateAt={onCreateAt}
      />
    </FeegowAgendaLayout>
  );
}

import { type ReactNode } from 'react';
import type { AppointmentDto, ProfessionalDto } from '../../../api/client';
import { FeegowAgendaScreenLayout } from './FeegowAgendaScreenLayout';
import { FeegowAgendaSidebar, type FeegowAgendaSidebarVariant } from './FeegowAgendaSidebar';

type FeegowAgendaLayoutProps = {
  variant?: FeegowAgendaSidebarVariant;
  date: string;
  onDateChange: (date: string) => void;
  appointments: AppointmentDto[];
  professionals: ProfessionalDto[];
  selectedProfessionalId: string;
  onProfessionalChange: (id: string) => void;
  onRefresh: () => void;
  canManage?: boolean;
  onEncaixe?: () => void;
  onBlock?: () => void;
  onBulkEdit?: () => void;
  onGrade?: () => void;
  onPrintReport?: () => void;
  onlyEmptySlots?: boolean;
  onOnlyEmptySlotsChange?: (value: boolean) => void;
  selectedEquipmentId?: string;
  onEquipmentChange?: (id: string) => void;
  error?: string;
  success?: string;
  children: ReactNode;
};

export function FeegowAgendaLayout({
  variant = 'daily',
  date,
  onDateChange,
  appointments,
  professionals,
  selectedProfessionalId,
  onProfessionalChange,
  onRefresh,
  canManage,
  onEncaixe,
  onBlock,
  onBulkEdit,
  onGrade,
  onPrintReport,
  onlyEmptySlots,
  onOnlyEmptySlotsChange,
  selectedEquipmentId,
  onEquipmentChange,
  error,
  success,
  children,
}: FeegowAgendaLayoutProps) {
  return (
    <FeegowAgendaScreenLayout error={error} success={success} sidebar={(
      <FeegowAgendaSidebar
        variant={variant}
        date={date}
        onDateChange={onDateChange}
        appointments={appointments}
        professionals={professionals}
        selectedProfessionalId={selectedProfessionalId}
        onProfessionalChange={onProfessionalChange}
        onRefresh={onRefresh}
        onEncaixe={onEncaixe}
        onBlock={onBlock}
        onBulkEdit={onBulkEdit}
        onGrade={onGrade}
        onPrintReport={onPrintReport}
        canManage={canManage}
        onlyEmptySlots={onlyEmptySlots}
        onOnlyEmptySlotsChange={onOnlyEmptySlotsChange}
        selectedEquipmentId={selectedEquipmentId}
        onEquipmentChange={onEquipmentChange}
      />
    )}
    >
      {children}
    </FeegowAgendaScreenLayout>
  );
}

import { useMemo } from 'react';
import type { AppointmentDto } from '../../../api/client';
import { getInstitutionShortName } from '../../../config/iasghBranding';
import { isSlotBlocked, type AgendaBlockSlot } from '../../../utils/agendaBlocks';
import { AGENDA_DAY_RANGES_DAILY } from '../../../utils/agendaGridUtils';
import { FeegowAgendaAppointmentCard } from './FeegowAgendaAppointmentCard';
type BlockConfig = { startHour: number; endHour: number; label: string };

type FeegowAgendaScheduleProps = {
  appointments: AppointmentDto[];
  date: string;
  roomLabel?: string;
  blockedSlots?: AgendaBlockSlot[];
  slotMinutes?: number;
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onClinicalData?: (appt: AppointmentDto) => void;
  onCreateAt?: (slotIso: string) => void;
};

function pad2(n: number) {
  return String(n).padStart(2, '0');
}

function buildSlots(startHour: number, endHour: number, slotMinutes: number) {
  const slots: string[] = [];
  for (let t = startHour * 60; t < endHour * 60; t += slotMinutes) {
    slots.push(`${pad2(Math.floor(t / 60))}:${pad2(t % 60)}`);
  }
  return slots;
}

function slotKey(appt: AppointmentDto, slotMinutes: number) {
  const d = new Date(appt.scheduledAt);
  const total = d.getHours() * 60 + d.getMinutes();
  const snapped = Math.floor(total / slotMinutes) * slotMinutes;
  return `${pad2(Math.floor(snapped / 60))}:${pad2(snapped % 60)}`;
}

function formatAgendaTitle(dateStr: string) {
  const d = new Date(`${dateStr}T12:00:00`);
  const text = d.toLocaleDateString('pt-BR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });
  return text.charAt(0).toUpperCase() + text.slice(1);
}

function ScheduleBlock({
  block,
  appointments,
  date,
  slotMinutes,
  roomLabel,
  blockedSlots,
  canManage,
  onStatusChange,
  onClinicalData,
  onCreateAt,
}: {
  block: BlockConfig;
  appointments: AppointmentDto[];
  date: string;
  slotMinutes: number;
  roomLabel: string;
  blockedSlots?: AgendaBlockSlot[];
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onClinicalData?: (appt: AppointmentDto) => void;
  onCreateAt?: (slotIso: string) => void;
}) {
  const slots = useMemo(
    () => buildSlots(block.startHour, block.endHour, slotMinutes),
    [block.endHour, block.startHour, slotMinutes],
  );

  const bySlot = useMemo(() => {
    const map = new Map<string, AppointmentDto[]>();
    for (const appt of appointments) {
      const h = new Date(appt.scheduledAt).getHours();
      if (h < block.startHour || h >= block.endHour) continue;
      const key = slotKey(appt, slotMinutes);
      const list = map.get(key) ?? [];
      list.push(appt);
      map.set(key, list);
    }
    return map;
  }, [appointments, block.endHour, block.startHour, slotMinutes]);

  return (
    <div className="feegow-agenda-block">
      <div className="feegow-agenda-room-title">{roomLabel}</div>
      <div className="feegow-agenda-rows">
        {slots.map((time) => {
          const items = bySlot.get(time) ?? [];
          const slotIso = `${date}T${time}`;
          const blockInfo = isSlotBlocked(blockedSlots ?? [], time);
          return (
            <div key={`${block.label}-${time}`} className="feegow-agenda-row">
              <div className="feegow-agenda-time-pill">{time}</div>
              <div className="feegow-agenda-slot">
                {blockInfo ? (
                  <div className="feegow-agenda-slot-blocked" title={blockInfo.reason}>
                    <span>Bloqueado</span>
                    <small>{blockInfo.reason}</small>
                  </div>
                ) : items.length === 0 ? (
                  canManage && onCreateAt ? (
                    <button type="button" className="feegow-agenda-slot-empty" onClick={() => onCreateAt(slotIso)}>
                      {' '}
                    </button>
                  ) : (
                    <div className="feegow-agenda-slot-empty" />
                  )
                ) : (
                  items.map((appt) => (
                    <FeegowAgendaAppointmentCard
                      key={appt.id}
                      appointment={appt}
                      canManage={canManage}
                      onStatusChange={onStatusChange}
                      onClinicalData={onClinicalData}
                    />
                  ))
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export function FeegowAgendaSchedule({
  appointments,
  date,
  roomLabel,
  blockedSlots,
  slotMinutes = 30,
  canManage,
  onStatusChange,
  onClinicalData,
  onCreateAt,
}: FeegowAgendaScheduleProps) {
  const institution = getInstitutionShortName();
  const room = roomLabel ? `${roomLabel} (${institution})` : `CONSULTÓRIO 01 (${institution})`;

  const blocks: BlockConfig[] = AGENDA_DAY_RANGES_DAILY.map(([startHour, endHour], index) => ({
    startHour,
    endHour,
    label: index === 0 ? 'manha' : 'tarde',
  }));

  return (
    <div className="feegow-agenda-schedule">
      <header className="feegow-agenda-schedule-head">
        <h1 className="feegow-agenda-schedule-title">Agenda diária</h1>
        <p className="feegow-agenda-schedule-date">
          <span className="feegow-agenda-cal-icon" aria-hidden>📅</span>
          <span className="feegow-crumb-sep">/</span>
          {formatAgendaTitle(date)}
        </p>
      </header>

      <div className="feegow-agenda-schedule-body">
        {blocks.map((block) => (
          <ScheduleBlock
            key={block.label}
            block={block}
            appointments={appointments}
            date={date}
            slotMinutes={slotMinutes}
            roomLabel={room}
            blockedSlots={blockedSlots}
            canManage={canManage}
            onStatusChange={onStatusChange}
            onClinicalData={onClinicalData}
            onCreateAt={onCreateAt}
          />
        ))}
      </div>
    </div>
  );
}

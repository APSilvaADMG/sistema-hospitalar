import { useMemo } from 'react';
import type { AppointmentDto } from '../../../api/client';
import type { AgendaBlockSlot } from '../../../utils/agendaBlocks';
import { isSlotBlocked } from '../../../utils/agendaBlocks';
import { getInstitutionShortName } from '../../../config/iasghBranding';
import {
  AGENDA_DAY_RANGES,
  buildAgendaTimeSlots,
  formatWeekRangeLabelLong,
  formatWeeklyColumnHeader,
  groupAppointmentsByDateAndSlot,
  shiftWeek,
  weekDatesFrom,
} from '../../../utils/agendaGridUtils';
import { FeegowAgendaAppointmentCard } from './FeegowAgendaAppointmentCard';

type Props = {
  date: string;
  onDateChange: (date: string) => void;
  appointments: AppointmentDto[];
  roomLabel?: string;
  blockedSlots?: AgendaBlockSlot[];
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onCreateAt?: (slotIso: string) => void;
};

function WeeklyDayColumn({
  day,
  roomLabel,
  timeSlots,
  byDaySlot,
  blockedSlots,
  canManage,
  onStatusChange,
  onCreateAt,
}: {
  day: string;
  roomLabel: string;
  timeSlots: string[];
  byDaySlot: Map<string, AppointmentDto[]>;
  blockedSlots?: AgendaBlockSlot[];
  canManage?: boolean;
  onStatusChange?: (id: string, status: number) => void;
  onCreateAt?: (slotIso: string) => void;
}) {
  const { label, isToday } = formatWeeklyColumnHeader(day);

  return (
    <div className={`feegow-weekly-col${isToday ? ' is-today' : ''}`}>
      <div className="feegow-weekly-col-head">
        <strong>{label}</strong>
        <span>{roomLabel}</span>
      </div>
      <div className="feegow-weekly-col-body">
        {AGENDA_DAY_RANGES.map(([startHour, endHour], blockIndex) => {
          const blockSlots = timeSlots.filter((time) => {
            const h = Number(time.split(':')[0]);
            return h >= startHour && h < endHour;
          });
          return (
            <div key={`${day}-${startHour}`} className="feegow-weekly-block">
              {blockIndex > 0 ? <div className="feegow-weekly-block-gap" aria-hidden /> : null}
              {blockSlots.map((time) => {
                const items = byDaySlot.get(`${day}|${time}`) ?? [];
                const blockInfo = isSlotBlocked(blockedSlots ?? [], time);
                const slotIso = `${day}T${time}`;
                if (blockInfo) {
                  return (
                    <div key={time} className="feegow-agenda-slot-blocked feegow-weekly-slot-blocked" title={blockInfo.reason}>
                      <span>Bloqueado</span>
                    </div>
                  );
                }
                if (items.length > 0) {
                  return items.map((appt) => (
                    <FeegowAgendaAppointmentCard
                      key={appt.id}
                      appointment={appt}
                      compact
                      showTimeInTitle
                      canManage={canManage}
                      onStatusChange={onStatusChange}
                    />
                  ));
                }
                if (canManage && onCreateAt) {
                  return (
                    <button
                      key={time}
                      type="button"
                      className="feegow-agenda-time-pill feegow-weekly-slot-btn"
                      onClick={() => onCreateAt(slotIso)}
                    >
                      {time}
                    </button>
                  );
                }
                return (
                  <div key={time} className="feegow-agenda-time-pill feegow-weekly-slot-btn feegow-weekly-slot-static">
                    {time}
                  </div>
                );
              })}
            </div>
          );
        })}
      </div>
    </div>
  );
}

export function FeegowWeeklyAgenda({
  date,
  onDateChange,
  appointments,
  roomLabel,
  blockedSlots,
  canManage,
  onStatusChange,
  onCreateAt,
}: Props) {
  const slotMinutes = 30;
  const weekDates = useMemo(() => weekDatesFrom(date), [date]);
  const timeSlots = useMemo(() => buildAgendaTimeSlots(slotMinutes), []);
  const byDaySlot = useMemo(
    () => groupAppointmentsByDateAndSlot(appointments, slotMinutes),
    [appointments],
  );
  const institution = getInstitutionShortName();
  const room = roomLabel ? `${roomLabel} (${institution})` : `CONSULTÓRIO 01 (${institution})`;

  return (
    <div className="feegow-agenda-schedule feegow-weekly-agenda">
      <header className="feegow-agenda-schedule-head">
        <div className="feegow-agenda-schedule-head-row">
          <div>
            <h1 className="feegow-agenda-schedule-title">Agenda Semanal</h1>
            <p className="feegow-agenda-schedule-date">
              <span className="feegow-agenda-cal-icon" aria-hidden>📅</span>
              <span className="feegow-crumb-sep">/</span>
              {formatWeekRangeLabelLong(date)}
            </p>
          </div>
          <div className="feegow-week-nav">
            <button type="button" className="feegow-week-nav-btn" onClick={() => onDateChange(shiftWeek(date, -1))} aria-label="Semana anterior">‹</button>
            <button type="button" className="feegow-week-nav-btn" onClick={() => onDateChange(shiftWeek(date, 1))} aria-label="Próxima semana">›</button>
            <button type="button" className="feegow-week-nav-btn feegow-week-today-btn" onClick={() => onDateChange(new Date().toISOString().slice(0, 10))}>
              Hoje
            </button>
          </div>
        </div>
      </header>

      <div className="feegow-agenda-schedule-body feegow-weekly-columns-wrap">
        <div className="feegow-weekly-columns">
          {weekDates.map((day) => (
            <WeeklyDayColumn
              key={day}
              day={day}
              roomLabel={room}
              timeSlots={timeSlots}
              byDaySlot={byDaySlot}
              blockedSlots={blockedSlots}
              canManage={canManage}
              onStatusChange={onStatusChange}
              onCreateAt={onCreateAt}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

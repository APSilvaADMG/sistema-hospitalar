import { useEffect, useMemo, useState } from 'react';
import type { AppointmentDto, HealthInsuranceDto, ProfessionalDto } from '../../../api/client';
import { formatBrTime } from '../../../utils/dateUtils';
import {
  AGENDA_DAY_RANGES,
  buildAgendaTimeSlots,
  localDateKey,
} from '../../../utils/agendaGridUtils';
import { FeegowAgendaAppointmentCard } from './FeegowAgendaAppointmentCard';

type Props = {
  date: string;
  appointments: AppointmentDto[];
  professionals: ProfessionalDto[];
  insurances: HealthInsuranceDto[];
  onlyEmptySlots?: boolean;
  canManage?: boolean;
  onCreateAt?: (slotIso: string, professionalId: string) => void;
};

type MultiFilters = {
  procedure: string;
  professionalId: string;
  specialtyId: string;
  insuranceId: string;
  location: string;
  equipment: string;
};

const EMPTY_FILTERS: MultiFilters = {
  procedure: '',
  professionalId: '',
  specialtyId: '',
  insuranceId: '',
  location: '',
  equipment: '',
};

const FILTER_FIELDS: { key: keyof MultiFilters; label: string; placeholder: string }[] = [
  { key: 'procedure', label: 'Procedimento', placeholder: 'Selecione' },
  { key: 'professionalId', label: 'Profissionais', placeholder: 'Selecione' },
  { key: 'specialtyId', label: 'Especialidades', placeholder: 'Selecione' },
  { key: 'insuranceId', label: 'Convênios', placeholder: 'Selecione' },
  { key: 'location', label: 'Locais', placeholder: 'Selecione' },
  { key: 'equipment', label: 'Equipamentos', placeholder: 'Selecione' },
];

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

export function FeegowMultipleAgenda({
  date,
  appointments,
  professionals,
  insurances,
  onlyEmptySlots = false,
  canManage,
  onCreateAt,
}: Props) {
  const [filters, setFilters] = useState<MultiFilters>(EMPTY_FILTERS);
  const [searched, setSearched] = useState(false);

  const specialties = useMemo(() => {
    const map = new Map<string, string>();
    for (const p of professionals) {
      map.set(p.specialtyId, p.specialtyName);
    }
    return Array.from(map.entries()).map(([id, name]) => ({ id, name }));
  }, [professionals]);

  const dayAppointments = useMemo(
    () => appointments.filter((a) => localDateKey(a.scheduledAt) === date),
    [appointments, date],
  );

  const hasAnyFilter = Object.values(filters).some(Boolean);

  useEffect(() => {
    if (hasAnyFilter) {
      setSearched(true);
    } else {
      setSearched(false);
    }
  }, [filters, hasAnyFilter]);

  const groupedResults = useMemo(() => {
    if (!searched || !hasAnyFilter) return [];

    const slots = buildAgendaTimeSlots(30);
    const filteredPros = professionals.filter((p) => {
      if (filters.professionalId && p.id !== filters.professionalId) return false;
      if (filters.specialtyId && p.specialtyId !== filters.specialtyId) return false;
      return true;
    });

    return filteredPros.map((pro) => {
      const items: { time: string; appointment?: AppointmentDto }[] = [];
      for (const time of slots) {
        const appt = dayAppointments.find((a) => {
          if (a.professionalId !== pro.id) return false;
          return formatBrTime(a.scheduledAt) === time;
        });
        if (filters.procedure && appt && !(appt.reason?.toLowerCase().includes(filters.procedure.toLowerCase()))) {
          continue;
        }
        if (filters.location && appt?.room && !appt.room.toLowerCase().includes(filters.location.toLowerCase())) {
          continue;
        }
        if (onlyEmptySlots && appt) continue;
        if (!onlyEmptySlots && !appt && filters.procedure) continue;
        items.push({ time, appointment: appt });
      }
      return { professional: pro, items: items.slice(0, 24) };
    }).filter((group) => group.items.length > 0);
  }, [searched, hasAnyFilter, filters, professionals, dayAppointments, onlyEmptySlots]);

  function patchFilter<K extends keyof MultiFilters>(key: K, value: MultiFilters[K]) {
    setFilters((prev) => ({ ...prev, [key]: value }));
  }

  function renderFilterOptions(key: keyof MultiFilters) {
    switch (key) {
      case 'procedure':
        return (
          <>
            <option value="">Selecione</option>
            <option value="consulta">Consulta</option>
            <option value="retorno">Retorno</option>
            <option value="exame">Exame</option>
          </>
        );
      case 'professionalId':
        return (
          <>
            <option value="">Selecione</option>
            {professionals.map((p) => (
              <option key={p.id} value={p.id}>{p.fullName}</option>
            ))}
          </>
        );
      case 'specialtyId':
        return (
          <>
            <option value="">Selecione</option>
            {specialties.map((s) => (
              <option key={s.id} value={s.id}>{s.name}</option>
            ))}
          </>
        );
      case 'insuranceId':
        return (
          <>
            <option value="">Selecione</option>
            {insurances.map((ins) => (
              <option key={ins.id} value={ins.id}>{ins.name}</option>
            ))}
          </>
        );
      case 'location':
        return (
          <>
            <option value="">Selecione</option>
            <option value="consultorio">Consultório 01</option>
            <option value="sala">Sala de espera</option>
          </>
        );
      case 'equipment':
        return (
          <>
            <option value="">Selecione</option>
            <option value="ultrassom">Ultrassom</option>
            <option value="raio-x">Raio-X</option>
          </>
        );
      default:
        return <option value="">Selecione</option>;
    }
  }

  return (
    <div className="feegow-agenda-schedule feegow-multiple-agenda">
      <header className="feegow-agenda-schedule-head">
        <h1 className="feegow-agenda-schedule-title">
          Agenda múltipla
          <span className="feegow-title-icons" aria-hidden>
            <span className="feegow-agenda-cal-icon">📅</span>
            <span className="feegow-folder-icon">📁</span>
          </span>
        </h1>
        <p className="feegow-agenda-schedule-date feegow-multi-date">
          <span className="feegow-agenda-cal-icon" aria-hidden>📅</span>
          <span className="feegow-crumb-sep">/</span>
          {formatAgendaTitle(date)}
        </p>
      </header>

      <div className="feegow-multi-filters">
        {FILTER_FIELDS.map((field) => (
          <label key={field.key} className="feegow-field">
            <span>{field.label}</span>
            <select
              id={`multi-${field.key}`}
              value={filters[field.key]}
              onChange={(e) => patchFilter(field.key, e.target.value)}
            >
              {renderFilterOptions(field.key)}
            </select>
          </label>
        ))}
      </div>

      <div className="feegow-agenda-schedule-body feegow-multiple-results">
        {!searched || !hasAnyFilter ? (
          <p className="feegow-agenda-empty-hint">
            Selecione os parâmetros acima para buscar na agenda.
          </p>
        ) : groupedResults.length === 0 ? (
          <p className="feegow-agenda-empty-hint">Nenhum horário encontrado para os filtros selecionados.</p>
        ) : (
          <div className="feegow-multi-columns">
            {groupedResults.map(({ professional, items }) => (
              <div key={professional.id} className="feegow-multi-col">
                <div className="feegow-multi-col-head">
                  <strong>{professional.fullName}</strong>
                  <span>{professional.specialtyName}</span>
                </div>
                <div className="feegow-multi-col-body">
                  {AGENDA_DAY_RANGES.map(([startHour, endHour], blockIndex) => {
                    const blockItems = items.filter(({ time }) => {
                      const h = Number(time.split(':')[0]);
                      return h >= startHour && h < endHour;
                    });
                    if (blockItems.length === 0) return null;
                    return (
                      <div key={`${professional.id}-${startHour}`} className="feegow-weekly-block">
                        {blockIndex > 0 ? <div className="feegow-weekly-block-gap" aria-hidden /> : null}
                        {blockItems.map(({ time, appointment }) => {
                          if (appointment) {
                            return (
                              <FeegowAgendaAppointmentCard
                                key={`${professional.id}-${time}`}
                                appointment={appointment}
                                compact
                                showTimeInTitle
                              />
                            );
                          }
                          if (canManage && onCreateAt) {
                            return (
                              <button
                                key={`${professional.id}-${time}`}
                                type="button"
                                className="feegow-agenda-time-pill feegow-weekly-slot-btn"
                                onClick={() => onCreateAt(`${date}T${time}`, professional.id)}
                              >
                                {time}
                              </button>
                            );
                          }
                          return (
                            <div key={`${professional.id}-${time}`} className="feegow-agenda-time-pill feegow-weekly-slot-btn feegow-weekly-slot-static">
                              {time}
                            </div>
                          );
                        })}
                      </div>
                    );
                  })}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

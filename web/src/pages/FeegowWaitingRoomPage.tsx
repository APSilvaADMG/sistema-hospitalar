import { useCallback, useEffect, useMemo, useState } from 'react';
import { api, type AppointmentDto, type EmergencyVisitDto, type ProfessionalDto } from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { FeegowEsperaScreenLayout } from '../components/feegow/espera/FeegowEsperaScreenLayout';
import {
  computeWaitingRoomStats,
  FeegowWaitingRoom,
  type SortKey,
  type StatusFilterKey,
} from '../components/feegow/espera/FeegowWaitingRoom';
import { syncPatientCallToTv } from '../utils/waitingRoomAnnounce';
import { printDailyAgendaReport, printEmergencyQueueSummary } from '../utils/printTemplates';

function todayBrazil(): string {
  return new Date().toLocaleDateString('en-CA', { timeZone: 'America/Sao_Paulo' });
}

export function FeegowWaitingRoomPage() {
  const { hasPermission } = useAuth();
  const canManage = hasPermission('patients.create', 'patients.update', 'reports.read');
  const canCallTv = hasPermission('connect.write');

  const [selectedDate, setSelectedDate] = useState(todayBrazil);
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [emergencyVisits, setEmergencyVisits] = useState<EmergencyVisitDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [statusFilter, setStatusFilter] = useState<StatusFilterKey>('waiting-in-care');
  const [sortBy, setSortBy] = useState<SortKey>('scheduled');
  const [professionalFilter, setProfessionalFilter] = useState('');
  const [specialtyFilter, setSpecialtyFilter] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const stats = useMemo(() => computeWaitingRoomStats(appointments), [appointments]);

  const sidebarCounts = useMemo(() => ({
    waiting: stats.waiting,
    inCare: stats.inCare,
    completedToday: stats.completedToday,
    emergencyWaiting: emergencyVisits.length,
    byRoom: stats.byRoom.map((r) => ({ room: r.room, waiting: r.waiting, inCare: r.inCare })),
  }), [stats, emergencyVisits.length]);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [list, pros, emergency] = await Promise.all([
        api.getAppointments(selectedDate),
        api.getProfessionals(),
        api.getEmergencyVisits('Waiting').catch(() => [] as EmergencyVisitDto[]),
      ]);
      setAppointments(list);
      setProfessionals(pros);
      setEmergencyVisits(emergency);
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : 'Não foi possível carregar a sala de espera.');
    } finally {
      setLoading(false);
    }
  }, [selectedDate]);

  useEffect(() => {
    load().catch(console.error);
    const timer = window.setInterval(() => {
      load().catch(console.error);
    }, 60_000);
    return () => window.clearInterval(timer);
  }, [load]);

  async function updateStatus(id: string, status: number, message: string) {
    setError('');
    setSuccess('');
    try {
      await api.updateAppointmentStatus(id, status);
      setSuccess(message);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível atualizar o status.');
    }
  }

  async function handlePrintPsQueue() {
    try {
      const allVisits = await api.getEmergencyVisits();
      const waiting = allVisits.filter((v) => v.status === 'Waiting').length;
      const inCare = allVisits.filter((v) => v.status === 'InCare').length;
      const discharged = allVisits.filter((v) => v.status === 'Discharged').length;
      const critical = allVisits.filter((v) => v.urgency === 'Emergency' || v.urgency === 'High').length;
      printEmergencyQueueSummary(allVisits, {
        total: allVisits.length,
        waiting,
        inCare,
        discharged,
        critical,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível gerar o relatório do PS.');
    }
  }

  function handlePrintDailyAgenda() {
    printDailyAgendaReport(selectedDate, appointments, {
      total: appointments.length,
      waiting: stats.waiting,
      inProgress: stats.inCare,
      done: stats.completedToday,
    });
  }

  if (!hasPermission('patients.read', 'reports.read')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <FeegowEsperaScreenLayout error={error} success={success} sidebarCounts={sidebarCounts}>
      <FeegowWaitingRoom
        appointments={appointments}
        emergencyVisits={emergencyVisits}
        professionals={professionals}
        selectedDate={selectedDate}
        onDateChange={setSelectedDate}
        stats={stats}
        statusFilter={statusFilter}
        onStatusFilterChange={setStatusFilter}
        sortBy={sortBy}
        onSortByChange={setSortBy}
        professionalFilter={professionalFilter}
        onProfessionalFilterChange={setProfessionalFilter}
        specialtyFilter={specialtyFilter}
        onSpecialtyFilterChange={setSpecialtyFilter}
        onRefresh={() => load().catch(console.error)}
        loading={loading}
        canManage={canManage}
        onPrintPsQueue={handlePrintPsQueue}
        onPrintDailyAgenda={handlePrintDailyAgenda}
        onCallPatient={async (id) => {
          const appt = appointments.find((a) => a.id === id);
          if (!appt) return;

          setError('');
          setSuccess('');
          try {
            await api.updateAppointmentStatus(id, 3);
            if (canCallTv) {
              await syncPatientCallToTv(
                appt.patientName,
                appt.room,
                appt.specialtyName,
                appt.id.replace(/-/g, '').slice(-4).toUpperCase(),
              );
            }
            setSuccess(`${appt.patientName} chamado para atendimento.`);
            await load();
          } catch (err) {
            setError(err instanceof Error ? err.message : 'Não foi possível chamar o paciente.');
          }
        }}
        onFinishCare={(id) => updateStatus(id, 4, 'Atendimento finalizado.')}
      />
    </FeegowEsperaScreenLayout>
  );
}

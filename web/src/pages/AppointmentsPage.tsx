import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useLocation, useSearchParams } from 'react-router-dom';
import {
  api,
  appointmentStatusLabel,
  appointmentStatusLabels,
  isAppointmentStatus,
  normalizeAppointmentStatus,
  type AppointmentDto,
  type ConsultingRoomScheduleDto,
  type HealthInsuranceDto,
  type PatientDto,
  type ProfessionalDto,
} from '../api/client';
import { ClinicalGuideCaptureModal } from '../components/funi/ClinicalGuideCaptureModal';
import { PatientWorkspaceShell } from '../components/patient-workspace/PatientWorkspaceShell';
import { resolveProfessionalRoom } from '../utils/consultingRoomUtils';
import { formatCpfInput, onlyDigits } from '../utils/inputMasks';
import { AppointmentStatusBadge } from '../components/AppointmentStatusBadge';
import { DateNavigator } from '../components/DateNavigator';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { ModulePageChrome } from '../components/ModulePageChrome';
import { appointmentTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useAuth } from '../auth/AuthContext';
import { formatBrTime } from '../utils/dateUtils';
import { AppointmentDayTimeline } from '../components/AppointmentDayTimeline';
import { FeegowAgendaBlockModal } from '../components/feegow/agenda/FeegowAgendaBlockModal';
import { FeegowAgendaBulkModal } from '../components/feegow/agenda/FeegowAgendaBulkModal';
import { FeegowAgendaGradeModal } from '../components/feegow/agenda/FeegowAgendaGradeModal';
import { FeegowAgendaLayout } from '../components/feegow/agenda/FeegowAgendaLayout';
import { FeegowDailyAgenda } from '../components/feegow/agenda/FeegowDailyAgenda';
import { FeegowAgendaMap } from '../components/feegow/agenda/FeegowAgendaMap';
import { FeegowCheckInAgenda } from '../components/feegow/agenda/FeegowCheckInAgenda';
import { FeegowConfirmAgenda } from '../components/feegow/agenda/FeegowConfirmAgenda';
import { FeegowEquipmentAgenda } from '../components/feegow/agenda/FeegowEquipmentAgenda';
import { FeegowMultipleAgenda } from '../components/feegow/agenda/FeegowMultipleAgenda';
import { FeegowWeeklyAgenda } from '../components/feegow/agenda/FeegowWeeklyAgenda';
import { addAgendaBlock, isSlotBlocked, loadAgendaBlocks, type AgendaBlockSlot } from '../utils/agendaBlocks';
import { appointmentInTimeRange, suggestEncaixeDateTime } from '../utils/agendaSlots';
import { localDateKey, parseAgendaViewMode, toIsoDate, weekDatesFrom, type FeegowAgendaViewMode } from '../utils/agendaGridUtils';
import { usePersistedFilters } from '../hooks/usePersistedFilters';
import { FILTER_STORAGE_KEYS } from '../utils/persistedFilters';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';
import { isFeegowDailyAgendaRoute } from '../utils/feegowRoutes';
import { printDailyAgendaReport } from '../utils/printTemplates';

const emptyForm = {
  patientId: '',
  professionalId: '',
  scheduledAt: '',
  durationMinutes: 30,
  reason: '',
  room: '',
};

type ViewMode = 'timeline' | 'list';

function initials(name: string) {
  return name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((p) => p[0]?.toUpperCase() ?? '')
    .join('');
}

type AppointmentsPageProps = {
  embedded?: boolean;
  sectionBasePath?: string;
  forcedSection?: string;
};

export function AppointmentsPage({
  embedded = false,
  sectionBasePath,
  forcedSection,
}: AppointmentsPageProps = {}) {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const navBasePath = sectionBasePath ?? '/agendamentos';
  const { section } = useModuleSection(navBasePath);
  const activeSection = forcedSection ?? (section || '');

  const { hasPermission } = useAuth();
  const { appearance } = useAppearance();
  const feegowAgendaScreen = isFeegowBrand(appearance.brand) && isFeegowDailyAgendaRoute(pathname);
  const showAppointmentSubNav = embedded
    && navBasePath.includes('/agendamentos')
    && activeSection !== 'check-in'
    && !feegowAgendaScreen;
  const canManage = hasPermission('patients.create', 'reports.read');

  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [roomSchedules, setRoomSchedules] = useState<ConsultingRoomScheduleDto[]>([]);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const { filters, patch } = usePersistedFilters(FILTER_STORAGE_KEYS.appointments, {
    date: new Date().toISOString().slice(0, 10),
    view: 'timeline' as ViewMode,
    professionalFilter: '',
    statusFilter: '',
    search: '',
  });
  const { date, view, professionalFilter, statusFilter, search } = filters;
  const agendaViewMode = parseAgendaViewMode(activeSection, pathname);
  const useFeegowAgendaView = feegowAgendaScreen;
  const [showModal, setShowModal] = useState(false);
  const [onlyEmptySlots, setOnlyEmptySlots] = useState(false);
  const [equipmentId, setEquipmentId] = useState('');
  const [createMode, setCreateMode] = useState<'normal' | 'encaixe'>('normal');
  const [showBlockModal, setShowBlockModal] = useState(false);
  const [showBulkModal, setShowBulkModal] = useState(false);
  const [showGradeModal, setShowGradeModal] = useState(false);
  const [agendaBlocks, setAgendaBlocks] = useState<AgendaBlockSlot[]>([]);
  const [checkInCpf, setCheckInCpf] = useState('');
  const [checkInMsg, setCheckInMsg] = useState('');
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [clinicalAppointment, setClinicalAppointment] = useState<AppointmentDto | null>(null);
  const [searchParams, setSearchParams] = useSearchParams();

  async function loadData(selectedDate: string, viewMode: FeegowAgendaViewMode = agendaViewMode) {
    const [patientList, professionalList, schedules] = await Promise.all([
      api.getPatients(undefined, 1),
      api.getProfessionals(),
      api.getRoomSchedules(),
    ]);
    let appointmentList: AppointmentDto[];
    if (viewMode === 'semanal') {
      const weekDays = weekDatesFrom(selectedDate);
      const lists = await Promise.all(weekDays.map((day) => api.getAppointments(day)));
      appointmentList = lists.flat();
    } else if (viewMode === 'confirmar') {
      const days = Array.from({ length: 14 }, (_, i) => {
        const d = new Date(`${selectedDate}T12:00:00`);
        d.setDate(d.getDate() + i);
        return toIsoDate(d);
      });
      const lists = await Promise.all(days.map((day) => api.getAppointments(day)));
      appointmentList = lists.flat();
    } else {
      appointmentList = await api.getAppointments(selectedDate);
    }
    setAppointments(appointmentList);
    setPatients(patientList.items);
    setProfessionals(professionalList);
    setRoomSchedules(schedules);
  }

  useEffect(() => {
    loadData(date, agendaViewMode).catch(console.error);
    api.getHealthInsurances().then((list) => setInsurances(Array.isArray(list) ? list : [])).catch(console.error);
  }, [date, agendaViewMode]);

  useEffect(() => {
    const profId = professionalFilter || professionals[0]?.id || '';
    setAgendaBlocks(loadAgendaBlocks(profId, date));
  }, [professionalFilter, professionals, date]);

  const filtered = useMemo(() => {
    return appointments
      .filter((a) => {
        if (agendaViewMode !== 'diaria') return true;
        if (activeSection === 'consultas') return true;
        if (activeSection === 'retornos') return (a.reason?.toLowerCase().includes('retorno') ?? false);
        if (activeSection === 'encaminhamentos') return isAppointmentStatus(a.status, 3) || (a.reason?.toLowerCase().includes('encamin') ?? false);
        if (activeSection === 'check-in') return isAppointmentStatus(a.status, 1, 2);
        return true;
      })
      .filter((a) => !professionalFilter || a.professionalId === professionalFilter)
      .filter((a) => !statusFilter || normalizeAppointmentStatus(a.status) === Number(statusFilter))
      .filter((a) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          a.patientName.toLowerCase().includes(term)
          || a.professionalName.toLowerCase().includes(term)
          || (a.reason?.toLowerCase().includes(term) ?? false)
        );
      })
      .sort((a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime());
  }, [appointments, professionalFilter, statusFilter, search, activeSection, agendaViewMode]);

  const sidebarAppointments = useMemo(() => {
    if (agendaViewMode === 'semanal') {
      return filtered.filter((a) => localDateKey(a.scheduledAt) === date);
    }
    return filtered;
  }, [filtered, agendaViewMode, date]);

  async function handleCheckIn(appointmentId: string) {
    setCheckInMsg('');
    try {
      await api.updateAppointmentStatus(appointmentId, 2);
      setSuccess('Check-in realizado — paciente confirmado na recepção.');
      await loadData(date, agendaViewMode);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro no check-in.');
    }
  }

  async function handleConfirmAppointment(appointmentId: string) {
    try {
      await api.updateAppointmentStatus(appointmentId, 2);
      setSuccess('Agendamento confirmado.');
      await loadData(date, agendaViewMode);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao confirmar agendamento.');
    }
  }

  async function handleKioskCheckIn(e: FormEvent) {
    e.preventDefault();
    setCheckInMsg('');
    try {
      const cpf = onlyDigits(checkInCpf);
      const result = await api.kioskCheckIn({ cpf: cpf || undefined });
      setCheckInMsg(result.message);
      if (result.success) await loadData(date, agendaViewMode);
    } catch (err) {
      setCheckInMsg(err instanceof Error ? err.message : 'Erro no totem.');
    }
  }

  const stats = useMemo(() => ({
    total: appointments.length,
    confirmed: appointments.filter((a) => isAppointmentStatus(a.status, 2)).length,
    waiting: appointments.filter((a) => isAppointmentStatus(a.status, 1, 2)).length,
    inProgress: appointments.filter((a) => isAppointmentStatus(a.status, 3)).length,
    done: appointments.filter((a) => isAppointmentStatus(a.status, 4)).length,
    cancelled: appointments.filter((a) => isAppointmentStatus(a.status, 5, 6)).length,
  }), [appointments]);

  function handlePrintDailyAgenda() {
    printDailyAgendaReport(date, filtered.length > 0 ? filtered : appointments, {
      total: stats.total,
      confirmed: stats.confirmed,
      waiting: stats.waiting,
      inProgress: stats.inProgress,
      done: stats.done,
    });
  }

  const selectedProfessional = useMemo(
    () => professionals.find((p) => p.id === professionalFilter),
    [professionals, professionalFilter],
  );

  function applyProfessionalRoom(professionalId: string, scheduledAt: string) {
    return resolveProfessionalRoom(professionalId, scheduledAt, roomSchedules);
  }

  function resolveAgendaProfessionalId() {
    return professionalFilter || professionals[0]?.id || '';
  }

  function openNewModal(slotIso?: string, professionalId?: string) {
    const profId = professionalId || resolveAgendaProfessionalId();
    const defaultTime = slotIso ?? `${date}T09:00`;
    if (slotIso) {
      const slotTime = slotIso.slice(11, 16);
      if (isSlotBlocked(agendaBlocks, slotTime)) {
        setError('Este horário está bloqueado na agenda.');
        return;
      }
    }
    setCreateMode('normal');
    setForm({
      ...emptyForm,
      professionalId: profId,
      scheduledAt: defaultTime,
      room: profId ? applyProfessionalRoom(profId, defaultTime) : '',
    });
    setShowModal(true);
  }

  useEffect(() => {
    const agendamentoId = searchParams.get('agendamento');
    if (agendamentoId && appointments.length > 0) {
      const appt = appointments.find((a) => a.id === agendamentoId);
      if (appt) patch({ search: appt.patientName });
      const next = new URLSearchParams(searchParams);
      next.delete('agendamento');
      setSearchParams(next, { replace: true });
      return;
    }
    if (searchParams.get('novo') === '1' && professionals.length > 0) {
      openNewModal();
      const next = new URLSearchParams(searchParams);
      next.delete('novo');
      setSearchParams(next, { replace: true });
    }
  }, [searchParams, appointments, professionals, setSearchParams, patch]);

  function openEncaixeModal() {
    const profId = resolveAgendaProfessionalId();
    if (!profId) {
      setError('Selecione um profissional para o encaixe.');
      return;
    }
    const slot = suggestEncaixeDateTime(date, filtered, profId);
    setCreateMode('encaixe');
    setForm({
      ...emptyForm,
      professionalId: profId,
      scheduledAt: slot,
      durationMinutes: 15,
      reason: 'Encaixe',
      room: applyProfessionalRoom(profId, slot),
    });
    setShowModal(true);
  }

  async function handleAgendaBlock(payload: {
    startTime: string;
    endTime: string;
    reason: string;
    cancelExisting: boolean;
    blockFullDay: boolean;
  }) {
    const profId = resolveAgendaProfessionalId();
    if (!profId) {
      throw new Error('Selecione um profissional.');
    }

    if (payload.blockFullDay) {
      const result = await api.blockConnectSchedule({
        professionalId: profId,
        date,
        reason: payload.reason,
      });
      setSuccess(
        `Dia bloqueado. ${result.affectedAppointments} consulta(s) cancelada(s)`
        + (result.notificationsSent ? ` · ${result.notificationsSent} paciente(s) notificado(s).` : '.'),
      );
    } else {
      const blocks = addAgendaBlock(profId, date, {
        startTime: payload.startTime,
        endTime: payload.endTime,
        reason: payload.reason,
      });
      setAgendaBlocks(blocks);

      if (payload.cancelExisting) {
        const toCancel = filtered.filter(
          (a) => a.professionalId === profId
            && !isAppointmentStatus(a.status, 5)
            && appointmentInTimeRange(a, date, payload.startTime, payload.endTime),
        );
        await Promise.all(toCancel.map((a) => api.updateAppointmentStatus(a.id, 5)));
        setSuccess(
          toCancel.length > 0
            ? `Período bloqueado. ${toCancel.length} consulta(s) cancelada(s).`
            : 'Período bloqueado na agenda.',
        );
      } else {
        setSuccess('Período bloqueado na agenda.');
      }
    }

    await loadData(date, agendaViewMode);
  }

  async function handleBulkStatusChange(appointmentIds: string[], status: number) {
    await Promise.all(appointmentIds.map((id) => api.updateAppointmentStatus(id, status)));
    setSuccess(`${appointmentIds.length} agendamento(s) atualizado(s).`);
    await loadData(date, agendaViewMode);
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');

    try {
      const result = await api.createAppointment({
        ...form,
        scheduledAt: new Date(form.scheduledAt).toISOString(),
        durationMinutes: Number(form.durationMinutes),
      });
      const warnText = result.warnings
        .map((w) => w.replace('[ELIGIBILITY_WARN] ', '').replace('[ELIGIBILITY_BLOCK] ', ''))
        .join(' ');
      setSuccess(
        (createMode === 'encaixe' ? 'Encaixe criado com sucesso.' : 'Agendamento criado com sucesso.')
        + (warnText ? ` Aviso: ${warnText}` : ''),
      );
      setForm(emptyForm);
      setCreateMode('normal');
      setShowModal(false);
      await loadData(date, agendaViewMode);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar agendamento.');
    }
  }

  async function handleStatusChange(id: string, status: number) {
    setError('');
    setSuccess('');
    try {
      await api.updateAppointmentStatus(id, status);
      if (status === 4) {
        setSuccess('Consulta concluída. Conta financeira gerada automaticamente (R$ 250,00).');
      } else {
        setSuccess('Status atualizado.');
      }
      await loadData(date, agendaViewMode);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao atualizar status.');
    }
  }

  const agendaRoomLabel = useMemo(() => {
    const fromAppt = filtered.find((a) => a.room)?.room;
    if (fromAppt) return fromAppt.toUpperCase().includes('CONSULT') ? fromAppt : `CONSULTÓRIO ${fromAppt}`;
    if (selectedProfessional && form.room) return form.room;
    return 'CONSULTÓRIO 01';
  }, [filtered, form.room, selectedProfessional]);

  const feegowAgendaModals = (
    <>
      <Modal
        open={showModal}
        onClose={() => { setShowModal(false); setCreateMode('normal'); }}
        title={createMode === 'encaixe' ? 'Encaixe na agenda' : 'Novo agendamento'}
        subtitle={
          createMode === 'encaixe'
            ? 'Horário sugerido no próximo espaço livre — ajuste se necessário.'
            : 'Preencha os dados para reservar um horário na agenda.'
        }
        width="lg"
      >
        <form className="feegow-form-grid" onSubmit={handleCreate}>
          <label className="feegow-field feegow-field-grow3">
            <span>Paciente<span className="feegow-req">*</span></span>
            <select
              id="patientId"
              required
              value={form.patientId}
              onChange={(e) => setForm({ ...form, patientId: e.target.value })}
            >
              <option value="">Selecione o paciente</option>
              {patients.map((patient) => (
                <option key={patient.id} value={patient.id}>{patient.fullName}</option>
              ))}
            </select>
          </label>
          <label className="feegow-field feegow-field-grow3">
            <span>Profissional<span className="feegow-req">*</span></span>
            <select
              id="professionalId"
              required
              value={form.professionalId}
              onChange={(e) => {
                const professionalId = e.target.value;
                setForm({
                  ...form,
                  professionalId,
                  room: applyProfessionalRoom(professionalId, form.scheduledAt),
                });
              }}
            >
              <option value="">Selecione o médico</option>
              {professionals.map((professional) => (
                <option key={professional.id} value={professional.id}>
                  {professional.fullName} — {professional.specialtyName}
                </option>
              ))}
            </select>
          </label>
          <label className="feegow-field feegow-field-grow2">
            <span>Data e hora<span className="feegow-req">*</span></span>
            <input
              id="scheduledAt"
              type="datetime-local"
              required
              value={form.scheduledAt}
              onChange={(e) => {
                const scheduledAt = e.target.value;
                setForm({
                  ...form,
                  scheduledAt,
                  room: form.professionalId
                    ? applyProfessionalRoom(form.professionalId, scheduledAt)
                    : form.room,
                });
              }}
            />
          </label>
          <label className="feegow-field">
            <span>Duração (min)</span>
            <select
              id="durationMinutes"
              value={form.durationMinutes}
              onChange={(e) => setForm({ ...form, durationMinutes: Number(e.target.value) })}
            >
              <option value={15}>15 min</option>
              <option value={30}>30 min</option>
              <option value={45}>45 min</option>
              <option value={60}>60 min</option>
            </select>
          </label>
          <label className="feegow-field feegow-field-grow2">
            <span>Motivo da consulta</span>
            <input
              id="reason"
              placeholder="Ex.: retorno, primeira consulta..."
              value={form.reason}
              onChange={(e) => setForm({ ...form, reason: e.target.value })}
            />
          </label>
          <label className="feegow-field feegow-field-grow2">
            <span>Sala / consultório</span>
            <input
              id="room"
              placeholder="Ex.: Consultório 101"
              value={form.room}
              onChange={(e) => setForm({ ...form, room: e.target.value })}
            />
          </label>
          <div className="feegow-form-actions">
            <button className="feegow-form-btn-cancel" type="button" onClick={() => setShowModal(false)}>
              Cancelar
            </button>
            <button className="feegow-patient-save-btn" type="submit">
              {createMode === 'encaixe' ? 'Confirmar encaixe' : 'Confirmar agendamento'}
            </button>
          </div>
        </form>
      </Modal>

      <FeegowAgendaBlockModal
        open={showBlockModal}
        date={date}
        professionalName={selectedProfessional?.fullName}
        onClose={() => setShowBlockModal(false)}
        onConfirm={handleAgendaBlock}
      />

      <FeegowAgendaBulkModal
        open={showBulkModal}
        date={date}
        appointments={filtered}
        professionalName={selectedProfessional?.fullName}
        onClose={() => setShowBulkModal(false)}
        onApply={handleBulkStatusChange}
      />

      <FeegowAgendaGradeModal
        open={showGradeModal}
        professionalId={resolveAgendaProfessionalId()}
        professionalName={selectedProfessional?.fullName ?? professionals[0]?.fullName}
        schedules={roomSchedules}
        onClose={() => setShowGradeModal(false)}
      />

      {clinicalAppointment && (
        <ClinicalGuideCaptureModal
          open
          onClose={() => setClinicalAppointment(null)}
          guideType={1}
          patients={patients}
          insurances={insurances}
          patientId={clinicalAppointment.patientId}
          clinicalContext={{
            appointmentId: clinicalAppointment.id,
            label: `Consulta ${formatBrTime(clinicalAppointment.scheduledAt)} — ${clinicalAppointment.patientName}`,
          }}
        />
      )}
    </>
  );

  const feegowAgendaCommon = {
    date,
    onDateChange: (value: string) => patch({ date: value }),
    appointments: sidebarAppointments,
    professionals,
    selectedProfessionalId: professionalFilter,
    onProfessionalChange: (id: string) => patch({ professionalFilter: id }),
    onRefresh: () => loadData(date, agendaViewMode),
    canManage,
    onEncaixe: canManage ? openEncaixeModal : undefined,
    onBlock: canManage ? () => setShowBlockModal(true) : undefined,
    onBulkEdit: canManage ? () => setShowBulkModal(true) : undefined,
    onGrade: () => setShowGradeModal(true),
    error,
    success,
  };

  if (useFeegowAgendaView) {
    return (
      <>
        {agendaViewMode === 'diaria' ? (
          <FeegowDailyAgenda
            {...feegowAgendaCommon}
            appointments={filtered}
            roomLabel={agendaRoomLabel}
            onStatusChange={handleStatusChange}
            onCreateAt={canManage ? openNewModal : undefined}
            blockedSlots={agendaBlocks}
            onPrintReport={handlePrintDailyAgenda}
          />
        ) : agendaViewMode === 'mapa' ? (
          <FeegowAgendaMap professionals={professionals} />
        ) : agendaViewMode === 'check-in' ? (
          <FeegowCheckInAgenda
            date={date}
            appointments={filtered}
            professionals={professionals}
            onCheckIn={handleCheckIn}
            canManage={canManage}
          />
        ) : agendaViewMode === 'confirmar' ? (
          <FeegowConfirmAgenda
            appointments={appointments}
            professionals={professionals}
            onConfirm={canManage ? handleConfirmAppointment : undefined}
            canManage={canManage}
          />
        ) : (
          <FeegowAgendaLayout
            {...feegowAgendaCommon}
            variant={
              agendaViewMode === 'semanal' ? 'weekly'
                : agendaViewMode === 'multipla' ? 'multiple'
                  : agendaViewMode === 'equipamentos' ? 'equipment'
                    : 'daily'
            }
            onlyEmptySlots={onlyEmptySlots}
            onOnlyEmptySlotsChange={setOnlyEmptySlots}
            selectedEquipmentId={equipmentId}
            onEquipmentChange={setEquipmentId}
            onBulkEdit={agendaViewMode === 'multipla' || agendaViewMode === 'equipamentos' ? undefined : feegowAgendaCommon.onBulkEdit}
          >
            {agendaViewMode === 'semanal' ? (
              <FeegowWeeklyAgenda
                date={date}
                onDateChange={(value) => patch({ date: value })}
                appointments={filtered}
                roomLabel={agendaRoomLabel}
                blockedSlots={agendaBlocks}
                canManage={canManage}
                onStatusChange={handleStatusChange}
                onCreateAt={canManage ? openNewModal : undefined}
              />
            ) : agendaViewMode === 'multipla' ? (
              <FeegowMultipleAgenda
                date={date}
                appointments={appointments}
                professionals={professionals}
                insurances={insurances}
                onlyEmptySlots={onlyEmptySlots}
                canManage={canManage}
                onCreateAt={canManage ? (slot, proId) => openNewModal(slot, proId) : undefined}
              />
            ) : (
              <FeegowEquipmentAgenda date={date} equipmentId={equipmentId} />
            )}
          </FeegowAgendaLayout>
        )}
        {feegowAgendaModals}
      </>
    );
  }

  const pageBody = (
    <>
      {showAppointmentSubNav && (
        <ModuleNav basePath={navBasePath} tabs={appointmentTabs} contextId="reception" />
      )}

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}
      {checkInMsg && <div className="alert alert-info">{checkInMsg}</div>}

      {activeSection === 'check-in' && (
        <>
          <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={handleKioskCheckIn}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Check-in por CPF (totem)</h3>
            <div className="form-field"><label>CPF do paciente</label>
              <input
                value={checkInCpf}
                onChange={(e) => setCheckInCpf(formatCpfInput(e.target.value))}
                placeholder="000.000.000-00"
                inputMode="numeric"
              />
            </div>
            <div className="form-actions">
              <button className="btn" type="submit">Validar check-in</button>
              <Link to="/acesso-fisico/totens" className="btn btn-secondary">Totens</Link>
            </div>
          </form>
          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Check-in manual — agendamentos do dia</div>
            <table className="data-table">
              <thead><tr><th>Horário</th><th>Paciente</th><th>Profissional</th><th>Status</th><th /></tr></thead>
              <tbody>
                {filtered.map((a) => (
                  <tr key={a.id}>
                    <td>{formatBrTime(a.scheduledAt)}</td>
                    <td>{a.patientName}</td>
                    <td>{a.professionalName}</td>
                    <td>{appointmentStatusLabel(a.status)}</td>
                    <td>
                      {isAppointmentStatus(a.status, 1) && canManage && (
                        <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleCheckIn(a.id)}>
                          Check-in
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}

      <div className="appt-kpi-grid">
        <KpiCard label="Total do dia" value={stats.total} variant="primary" />
        <KpiCard label="Confirmados" value={stats.confirmed} variant="info" />
        <KpiCard label="Aguardando" value={stats.waiting} variant="warning" />
        <KpiCard label="Em atendimento" value={stats.inProgress} variant="neutral" />
        <KpiCard label="Concluídos" value={stats.done} variant="success" />
        <KpiCard label="Cancelados / faltas" value={stats.cancelled} variant="danger" />
      </div>

      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <button type="button" className="btn btn-secondary btn-sm" onClick={handlePrintDailyAgenda}>
          Imprimir agenda do dia
        </button>
      </div>

      <div className="card-panel appt-panel">
        <div className="appt-panel-toolbar">
          <DateNavigator date={date} onChange={(value) => patch({ date: value })} />
          <div className="view-tabs">
            <button
              type="button"
              className={`view-tab${view === 'timeline' ? ' active' : ''}`}
              onClick={() => patch({ view: 'timeline' })}
            >
              Agenda do dia
            </button>
            <button
              type="button"
              className={`view-tab${view === 'list' ? ' active' : ''}`}
              onClick={() => patch({ view: 'list' })}
            >
              Lista
            </button>
          </div>
        </div>

        <FilterBar>
          <div className="filter-field w-xl">
            <label htmlFor="profFilter">Profissional</label>
            <select
              id="profFilter"
              value={professionalFilter}
              onChange={(e) => patch({ professionalFilter: e.target.value })}
            >
              <option value="">Todos</option>
              {professionals.map((p) => (
                <option key={p.id} value={p.id}>{p.fullName}</option>
              ))}
            </select>
          </div>
          <div className="filter-field w-md">
            <label htmlFor="statusFilter">Status</label>
            <select
              id="statusFilter"
              value={statusFilter}
              onChange={(e) => patch({ statusFilter: e.target.value })}
            >
              <option value="">Todos</option>
              {Object.entries(appointmentStatusLabels).map(([value, label]) => (
                <option key={value} value={value}>{label}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="searchAppt">Buscar paciente</label>
            <input
              id="searchAppt"
              placeholder="Nome do paciente ou motivo..."
              value={search}
              onChange={(e) => patch({ search: e.target.value })}
            />
          </div>
        </FilterBar>

        <div className="card-panel-body appt-panel-body">
          {view === 'timeline' ? (
            <AppointmentDayTimeline
              appointments={filtered}
              date={date}
              professionalName={selectedProfessional?.fullName}
              canManage={canManage}
              onStatusChange={handleStatusChange}
              onClinicalData={setClinicalAppointment}
              onCreateAt={canManage ? openNewModal : undefined}
            />
          ) : (
            <div style={{ padding: 0, margin: '-20px' }}>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Horário</th>
                    <th>Paciente</th>
                    <th>Profissional</th>
                    <th>Especialidade</th>
                    <th>Status</th>
                    <th>Sala</th>
                    <th>Motivo</th>
                    <th>Ações</th>
                  </tr>
                </thead>
                <tbody>
                  {filtered.map((appt) => (
                    <tr key={appt.id}>
                      <td>
                        <strong>{formatBrTime(appt.scheduledAt)}</strong>
                        <div className="table-sub">{appt.durationMinutes} min</div>
                      </td>
                      <td>
                        <div className="table-cell-with-avatar">
                          <span className="appt-avatar appt-avatar-sm">{initials(appt.patientName)}</span>
                          <strong>{appt.patientName}</strong>
                        </div>
                      </td>
                      <td>{appt.professionalName}</td>
                      <td>{appt.specialtyName}</td>
                      <td><AppointmentStatusBadge status={appt.status} /></td>
                      <td>{appt.room ?? '—'}</td>
                      <td>{appt.reason ?? '—'}</td>
                      <td>
                        <div className="table-actions">
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() => setClinicalAppointment(appt)}
                          >
                            Dados TISS
                          </button>
                          <select
                            className="appt-status-select"
                            value={normalizeAppointmentStatus(appt.status)}
                            onChange={(e) => handleStatusChange(appt.id, Number(e.target.value))}
                          >
                            {Object.entries(appointmentStatusLabels).map(([value, label]) => (
                              <option key={value} value={value}>{label}</option>
                            ))}
                          </select>
                        </div>
                      </td>
                    </tr>
                  ))}
                  {filtered.length === 0 && (
                    <tr>
                      <td colSpan={8} style={{ textAlign: 'center', padding: 32, color: 'var(--muted)' }}>
                        Nenhum agendamento para esta data.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

    </>
  );

  return (
    <>
      <ModulePageChrome
        embedded={embedded}
        eyebrow="Atendimento"
        title={activeSection ? breadcrumb.title : 'Agendamentos'}
        subtitle="Agenda diária por profissional — consultas, confirmações e fluxo de atendimento."
        basePath={navBasePath}
        tabs={appointmentTabs}
        contextId="reception"
        actions={
          <>
            <button type="button" className="btn btn-secondary" onClick={handlePrintDailyAgenda}>
              Imprimir agenda
            </button>
            {canManage ? (
              <button className="btn" type="button" onClick={() => openNewModal()}>
                + Novo agendamento
              </button>
            ) : null}
          </>
        }
      >
        {embedded || feegowAgendaScreen ? (
          pageBody
        ) : (
          <PatientWorkspaceShell moduleId="reception" patients={patients} hidePickerWhenSelected>
            {pageBody}
          </PatientWorkspaceShell>
        )}
      </ModulePageChrome>

      {feegowAgendaModals}
    </>
  );
}

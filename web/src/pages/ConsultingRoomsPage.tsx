import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  consultingRoomStatusLabels,
  dayOfWeekLabels,
  type ConsultingRoomDto,
  type ConsultingRoomScheduleDto,
  type ProfessionalDto,
  type SpecialtyDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModulePageChrome } from '../components/ModulePageChrome';
import { consultingRoomsTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import { useAuth } from '../auth/AuthContext';
import { useLocation } from 'react-router-dom';

type ConsultingRoomsPageProps = {
  embedded?: boolean;
  sectionBasePath?: string;
};

export function ConsultingRoomsPage({
  embedded = false,
  sectionBasePath,
}: ConsultingRoomsPageProps = {}) {
  const { hasPermission } = useAuth();
  const canManage = hasPermission('patients.create', 'reports.read');
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const navBasePath = sectionBasePath ?? '/consultorios';
  const { section } = useModuleSection(navBasePath);
  const activeSection = section || '';

  const [rooms, setRooms] = useState<ConsultingRoomDto[]>([]);
  const [schedules, setSchedules] = useState<ConsultingRoomScheduleDto[]>([]);
  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [roomForm, setRoomForm] = useState({ name: '', floor: '', building: '', specialtyId: '' });
  const [scheduleForm, setScheduleForm] = useState({
    consultingRoomId: '',
    professionalId: '',
    dayOfWeek: 'Monday',
    startTime: '08:00',
    endTime: '12:00',
  });
  const [showRoomModal, setShowRoomModal] = useState(false);
  const [showScheduleModal, setShowScheduleModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [specialtyFilter, setSpecialtyFilter] = useState('');
  const [dayFilter, setDayFilter] = useState('');
  const [search, setSearch] = useState('');

  useEffect(() => {
    load();
    api.getSpecialties().then(setSpecialties).catch(console.error);
    api.getProfessionals().then(setProfessionals).catch(console.error);
  }, []);

  async function load() {
    const [r, s] = await Promise.all([api.getConsultingRooms(), api.getRoomSchedules()]);
    setRooms(r);
    setSchedules(s);
  }

  const filteredRooms = useMemo(() => rooms
    .filter((r) => !statusFilter || r.status === statusFilter)
    .filter((r) => !specialtyFilter || r.specialtyName === specialtyFilter)
    .filter((r) => {
      if (!search.trim()) return true;
      const term = search.toLowerCase();
      return r.name.toLowerCase().includes(term)
        || (r.building?.toLowerCase().includes(term) ?? false)
        || (r.specialtyName?.toLowerCase().includes(term) ?? false);
    }), [rooms, statusFilter, specialtyFilter, search]);

  const filteredSchedules = useMemo(() => schedules
    .filter((s) => !dayFilter || s.dayOfWeek === dayFilter)
    .filter((s) => !specialtyFilter || s.specialtyName === specialtyFilter)
    .filter((s) => {
      if (!search.trim()) return true;
      const term = search.toLowerCase();
      return s.roomName.toLowerCase().includes(term)
        || s.professionalName.toLowerCase().includes(term)
        || s.specialtyName.toLowerCase().includes(term);
    }), [schedules, dayFilter, specialtyFilter, search]);

  const stats = useMemo(() => ({
    totalRooms: rooms.length,
    available: rooms.filter((r) => r.status === 'Available').length,
    occupied: rooms.filter((r) => r.status === 'Occupied').length,
    maintenance: rooms.filter((r) => r.status === 'Maintenance').length,
    schedules: schedules.length,
  }), [rooms, schedules]);

  const specialtyNames = useMemo(() => {
    const names = new Set<string>();
    rooms.forEach((r) => { if (r.specialtyName) names.add(r.specialtyName); });
    schedules.forEach((s) => names.add(s.specialtyName));
    return [...names].sort();
  }, [rooms, schedules]);

  async function handleCreateRoom(e: FormEvent) {
    e.preventDefault();
    await api.createConsultingRoom({
      name: roomForm.name,
      floor: roomForm.floor || undefined,
      building: roomForm.building || undefined,
      specialtyId: roomForm.specialtyId || undefined,
    });
    setRoomForm({ name: '', floor: '', building: '', specialtyId: '' });
    setShowRoomModal(false);
    await load();
  }

  async function handleCreateSchedule(e: FormEvent) {
    e.preventDefault();
    await api.createRoomSchedule(scheduleForm);
    setShowScheduleModal(false);
    await load();
  }

  return (
    <>
      <ModulePageChrome
        embedded={embedded}
        eyebrow="Atendimento"
        title={activeSection ? breadcrumb.title : 'Consultórios'}
        subtitle="Salas ambulatoriais, disponibilidade e escalas semanais por especialidade."
        basePath={navBasePath}
        tabs={consultingRoomsTabs}
        contextId="reception"
        actions={
          canManage ? (
            <div style={{ display: 'flex', gap: 8 }}>
              <button className="btn btn-secondary" type="button" onClick={() => setShowScheduleModal(true)}>
                + Nova escala
              </button>
              <button className="btn" type="button" onClick={() => setShowRoomModal(true)}>
                + Novo consultório
              </button>
            </div>
          ) : undefined
        }
      >
      <div className="kpi-grid">
        <KpiCard label="Consultórios" value={stats.totalRooms} variant="primary" />
        <KpiCard label="Disponíveis" value={stats.available} variant="success" />
        <KpiCard label="Ocupados" value={stats.occupied} variant="warning" />
        <KpiCard label="Em manutenção" value={stats.maintenance} variant="neutral" />
        <KpiCard label="Escalas ativas" value={stats.schedules} variant="info" />
      </div>

      <div className="card-panel appt-panel">
        <FilterBar>
          {!activeSection || activeSection === '' ? (
            <div className="filter-field w-md">
              <label htmlFor="roomStatus">Status</label>
              <select id="roomStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
                <option value="">Todos</option>
                {Object.entries(consultingRoomStatusLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
          ) : (
            <div className="filter-field w-md">
              <label htmlFor="dayFilter">Dia da semana</label>
              <select id="dayFilter" value={dayFilter} onChange={(e) => setDayFilter(e.target.value)}>
                <option value="">Todos</option>
                {Object.entries(dayOfWeekLabels).map(([k, v]) => (
                  <option key={k} value={k}>{v}</option>
                ))}
              </select>
            </div>
          )}
          <div className="filter-field w-lg">
            <label htmlFor="specFilter">Especialidade</label>
            <select id="specFilter" value={specialtyFilter} onChange={(e) => setSpecialtyFilter(e.target.value)}>
              <option value="">Todas</option>
              {specialtyNames.map((name) => (
                <option key={name} value={name}>{name}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="roomSearch">Buscar</label>
            <input
              id="roomSearch"
              placeholder={activeSection !== 'escalas' ? 'Nome, prédio ou especialidade...' : 'Sala, profissional...'}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>

        <div className="card-panel-body" style={{ padding: 0 }}>
          {activeSection !== 'escalas' ? (
            <table className="data-table">
              <thead>
                <tr><th>Consultório</th><th>Prédio</th><th>Andar</th><th>Especialidade</th><th>Status</th></tr>
              </thead>
              <tbody>
                {filteredRooms.map((r) => (
                  <tr key={r.id}>
                    <td><strong>{r.name}</strong></td>
                    <td>{r.building ?? '—'}</td>
                    <td>{r.floor ?? '—'}</td>
                    <td>{r.specialtyName ?? '—'}</td>
                    <td>
                      <span className={`appt-status room-status-${r.status.toLowerCase()}`}>
                        {consultingRoomStatusLabels[r.status] ?? r.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {filteredRooms.length === 0 && (
                  <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum consultório encontrado.</td></tr>
                )}
              </tbody>
            </table>
          ) : (
            <table className="data-table">
              <thead>
                <tr><th>Sala</th><th>Profissional</th><th>Especialidade</th><th>Dia</th><th>Horário</th></tr>
              </thead>
              <tbody>
                {filteredSchedules.map((s) => (
                  <tr key={s.id}>
                    <td><strong>{s.roomName}</strong></td>
                    <td>{s.professionalName}</td>
                    <td>{s.specialtyName}</td>
                    <td>{dayOfWeekLabels[s.dayOfWeek] ?? s.dayOfWeek}</td>
                    <td>{s.startTime} – {s.endTime}</td>
                  </tr>
                ))}
                {filteredSchedules.length === 0 && (
                  <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma escala encontrada.</td></tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>

      </ModulePageChrome>

      <Modal open={showRoomModal} onClose={() => setShowRoomModal(false)} title="Novo consultório" subtitle="Cadastro de sala ambulatorial." width="lg">
        <form onSubmit={handleCreateRoom} className="form-grid">
          <div className="form-field">
            <label>Nome *</label>
            <input placeholder="Ex.: Consultório 201" value={roomForm.name} onChange={(e) => setRoomForm({ ...roomForm, name: e.target.value })} required />
          </div>
          <div className="form-field">
            <label>Prédio</label>
            <input value={roomForm.building} onChange={(e) => setRoomForm({ ...roomForm, building: e.target.value })} />
          </div>
          <div className="form-field">
            <label>Andar</label>
            <input value={roomForm.floor} onChange={(e) => setRoomForm({ ...roomForm, floor: e.target.value })} />
          </div>
          <div className="form-field">
            <label>Especialidade</label>
            <select value={roomForm.specialtyId} onChange={(e) => setRoomForm({ ...roomForm, specialtyId: e.target.value })}>
              <option value="">Opcional</option>
              {specialties.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowRoomModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Cadastrar sala</button>
          </div>
        </form>
      </Modal>

      <Modal open={showScheduleModal} onClose={() => setShowScheduleModal(false)} title="Nova escala" subtitle="Horário semanal de atendimento no consultório." width="lg">
        <form onSubmit={handleCreateSchedule} className="form-grid">
          <div className="form-field">
            <label>Consultório *</label>
            <select value={scheduleForm.consultingRoomId} onChange={(e) => setScheduleForm({ ...scheduleForm, consultingRoomId: e.target.value })} required>
              <option value="">Selecione...</option>
              {rooms.map((r) => <option key={r.id} value={r.id}>{r.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Profissional *</label>
            <select value={scheduleForm.professionalId} onChange={(e) => setScheduleForm({ ...scheduleForm, professionalId: e.target.value })} required>
              <option value="">Selecione...</option>
              {professionals.map((p) => <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Dia da semana *</label>
            <select value={scheduleForm.dayOfWeek} onChange={(e) => setScheduleForm({ ...scheduleForm, dayOfWeek: e.target.value })}>
              {Object.entries(dayOfWeekLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Horário *</label>
            <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
              <input type="time" value={scheduleForm.startTime} onChange={(e) => setScheduleForm({ ...scheduleForm, startTime: e.target.value })} required />
              <span>até</span>
              <input type="time" value={scheduleForm.endTime} onChange={(e) => setScheduleForm({ ...scheduleForm, endTime: e.target.value })} required />
            </div>
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowScheduleModal(false)}>Cancelar</button>
            <button type="submit" className="btn">Adicionar escala</button>
          </div>
        </form>
      </Modal>
    </>
  );
}

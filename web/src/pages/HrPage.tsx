import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  api, employeeRoleLabels, shiftTypeLabels,
  type CreateEmployeeHrEventRequest, type CreateEmployeeRequest, type DepartmentDto, type EmployeeDto,
  type EmployeeDetailDto, type EmployeeHrEventDto, type EmployeeShiftDto, type HrDashboardDto,
  type UpdateEmployeeRequest,
} from '../api/client';
import { AddressFields } from '../components/AddressFields';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { hrTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { PersonAvatar } from '../components/PersonAvatar';
import { formatBrDate } from '../utils/dateUtils';
import { normalizeCepDigits } from '../utils/cepLookup';
import { PhotoCapture } from '../components/PhotoCapture';
import { useAuth } from '../auth/AuthContext';
import { PayrollPage } from './PayrollPage';
import { FeegowRhScreenLayout } from '../components/feegow/rh/FeegowRhScreenLayout';

function formatBrl(value: number) {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

function toIsoDate(d: Date) {
  return d.toISOString().slice(0, 10);
}

function getWeekRange(anchor: string) {
  const date = new Date(`${anchor}T12:00:00`);
  const day = date.getDay();
  const mondayOffset = day === 0 ? -6 : 1 - day;
  const monday = new Date(date);
  monday.setDate(date.getDate() + mondayOffset);
  const sunday = new Date(monday);
  sunday.setDate(monday.getDate() + 6);
  return { from: toIsoDate(monday), to: toIsoDate(sunday), monday, sunday };
}

function shiftWeek(anchor: string, delta: number) {
  const d = new Date(`${anchor}T12:00:00`);
  d.setDate(d.getDate() + delta * 7);
  return toIsoDate(d);
}

const emptyEmpForm: CreateEmployeeRequest = {
  fullName: '',
  socialName: '',
  cpf: '',
  rg: '',
  birthDate: '',
  gender: 0,
  email: '',
  phone: '',
  mobilePhone: '',
  jobTitle: '',
  role: 1,
  departmentId: '',
  hireDate: new Date().toISOString().slice(0, 10),
  baseSalary: 0,
  addressStreet: '',
  addressNumber: '',
  addressComplement: '',
  addressNeighborhood: '',
  addressCity: '',
  addressState: '',
  addressZipCode: '',
  emergencyContactName: '',
  emergencyContactPhone: '',
  notes: '',
  photoData: undefined,
};

function detailToForm(d: EmployeeDetailDto): CreateEmployeeRequest & { isActive: boolean } {
  return {
    fullName: d.fullName,
    socialName: d.socialName ?? '',
    cpf: d.cpf ?? '',
    rg: d.rg ?? '',
    birthDate: d.birthDate ?? '',
    gender: d.gender,
    email: d.email ?? '',
    phone: d.phone ?? '',
    mobilePhone: d.mobilePhone ?? '',
    jobTitle: d.jobTitle ?? '',
    role: d.role,
    departmentId: d.departmentId,
    hireDate: d.hireDate,
    baseSalary: d.baseSalary,
    addressStreet: d.addressStreet ?? '',
    addressNumber: d.addressNumber ?? '',
    addressComplement: d.addressComplement ?? '',
    addressNeighborhood: d.addressNeighborhood ?? '',
    addressCity: d.addressCity ?? '',
    addressState: d.addressState ?? '',
    addressZipCode: d.addressZipCode ?? '',
    emergencyContactName: d.emergencyContactName ?? '',
    emergencyContactPhone: d.emergencyContactPhone ?? '',
    notes: d.notes ?? '',
    photoData: d.photoData,
    isActive: d.isActive,
  };
}

type HrEventSection = 'ferias' | 'treinamentos' | 'avaliacoes';
type HrEventStatus = 'Agendado' | 'Em andamento' | 'Concluído';

function hrEventTypeForSection(section: HrEventSection): number {
  if (section === 'ferias') return 1;
  if (section === 'treinamentos') return 2;
  return 3;
}

function hrEventStatus(start: string, end?: string): HrEventStatus {
  const today = toIsoDate(new Date());
  const endDate = end ?? start;
  if (start > today) return 'Agendado';
  if (endDate < today) return 'Concluído';
  return 'Em andamento';
}

function statusBadgeClass(status: HrEventStatus) {
  if (status === 'Agendado') return 'badge badge-warning';
  if (status === 'Em andamento') return 'badge badge-info';
  return 'badge badge-success';
}

function detailPlaceholder(section: HrEventSection): string {
  if (section === 'ferias') return 'Ex.: 15 dias — período aquisitivo';
  if (section === 'treinamentos') return 'Ex.: ACLS — 8 horas — certificação';
  return 'Ex.: Nota 4,5 — metas atingidas';
}

export function HrPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/rh');
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [departments, setDepartments] = useState<DepartmentDto[]>([]);
  const [employees, setEmployees] = useState<EmployeeDto[]>([]);
  const [shifts, setShifts] = useState<EmployeeShiftDto[]>([]);
  const [dashboard, setDashboard] = useState<HrDashboardDto | null>(null);
  const [weekAnchor, setWeekAnchor] = useState(toIsoDate(new Date()));
  const [empForm, setEmpForm] = useState(emptyEmpForm);
  const [isActive, setIsActive] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showEmpModal, setShowEmpModal] = useState(false);
  const [shiftForm, setShiftForm] = useState({
    employeeId: '',
    departmentId: '',
    shiftDate: toIsoDate(new Date()),
    shiftType: 1,
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [hrEvents, setHrEvents] = useState<EmployeeHrEventDto[]>([]);
  const [hrEventsLoading, setHrEventsLoading] = useState(false);
  const [hrForm, setHrForm] = useState({ employeeId: '', title: '', detail: '', start: '', end: '' });

  const weekRange = useMemo(() => getWeekRange(weekAnchor), [weekAnchor]);
  const usesWeekView = activeSection === 'escalas' || activeSection === 'plantoes';

  const eventKpis = useMemo(() => {
    const today = toIsoDate(new Date());
    const statuses = hrEvents.map((e) => hrEventStatus(e.startDate, e.endDate));
    return {
      total: hrEvents.length,
      scheduled: statuses.filter((s) => s === 'Agendado').length,
      inProgress: statuses.filter((s) => s === 'Em andamento').length,
      completed: statuses.filter((s) => s === 'Concluído').length,
      onVacationToday: hrEvents.filter((e) => {
        const end = e.endDate ?? e.startDate;
        return e.startDate <= today && end >= today;
      }).length,
    };
  }, [hrEvents]);

  async function loadShifts() {
    if (usesWeekView) {
      const params = activeSection === 'plantoes'
        ? { from: weekRange.from, to: weekRange.to, shiftType: 3 }
        : { from: weekRange.from, to: weekRange.to };
      return api.getShifts(params);
    }
    return api.getShifts({ date: toIsoDate(new Date()) });
  }

  async function load() {
    const [deptList, empList, dash, shiftList] = await Promise.all([
      api.getDepartments(),
      api.getEmployees(),
      api.getHrDashboard(),
      loadShifts(),
    ]);
    setDepartments(deptList);
    setEmployees(empList);
    setDashboard(dash);
    setShifts(shiftList);
  }

  useEffect(() => {
    load().catch(console.error);
  }, [activeSection, weekAnchor]);

  useEffect(() => {
    if (activeSection !== 'ferias' && activeSection !== 'treinamentos' && activeSection !== 'avaliacoes') {
      return;
    }
    setHrEventsLoading(true);
    api.getHrEvents(hrEventTypeForSection(activeSection))
      .then(setHrEvents)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar registros.'))
      .finally(() => setHrEventsLoading(false));
  }, [activeSection]);

  function openCreateEmployee() {
    setEditingId(null);
    setEmpForm(emptyEmpForm);
    setIsActive(true);
    setShowEmpModal(true);
  }

  async function openEditEmployee(id: string) {
    const detail = await api.getEmployee(id);
    const mapped = detailToForm(detail);
    setEditingId(id);
    setEmpForm(mapped);
    setIsActive(mapped.isActive);
    setShowEmpModal(true);
  }

  async function handleCreateEmployee(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');

    const payload: CreateEmployeeRequest = {
      ...empForm,
      birthDate: empForm.birthDate || undefined,
      photoData: empForm.photoData || undefined,
      socialName: empForm.socialName || undefined,
      cpf: empForm.cpf || undefined,
      rg: empForm.rg || undefined,
      email: empForm.email || undefined,
      phone: empForm.phone || undefined,
      mobilePhone: empForm.mobilePhone || undefined,
      jobTitle: empForm.jobTitle || undefined,
      addressStreet: empForm.addressStreet || undefined,
      addressNumber: empForm.addressNumber || undefined,
      addressComplement: empForm.addressComplement || undefined,
      addressNeighborhood: empForm.addressNeighborhood || undefined,
      addressCity: empForm.addressCity || undefined,
      addressState: empForm.addressState || undefined,
      addressZipCode: empForm.addressZipCode ? normalizeCepDigits(empForm.addressZipCode) : undefined,
      emergencyContactName: empForm.emergencyContactName || undefined,
      emergencyContactPhone: empForm.emergencyContactPhone || undefined,
      notes: empForm.notes || undefined,
      baseSalary: Number(empForm.baseSalary) || 0,
    };

    try {
      if (editingId) {
        await api.updateEmployee(editingId, { ...payload, isActive } as UpdateEmployeeRequest);
        setSuccess('Colaborador atualizado.');
      } else {
        await api.createEmployee(payload);
        setSuccess('Colaborador cadastrado.');
      }
      setShowEmpModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar colaborador.');
    }
  }

  async function handleCreateShift(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createShift({
        ...shiftForm,
        shiftDate: usesWeekView ? shiftForm.shiftDate : weekRange.from,
      });
      setSuccess('Escala registrada.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar escala.');
    }
  }

  async function handleDeleteShift(id: string) {
    if (!window.confirm('Remover esta escala?')) return;
    setError('');
    setSuccess('');
    try {
      await api.deleteShift(id);
      setSuccess('Escala removida.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao remover escala.');
    }
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito à equipe administrativa.</div>;
  }

  if (activeSection === 'folha') {
    return <PayrollPage />;
  }

  function renderKpis() {
    if (activeSection === 'ferias') {
      return (
        <div className="kpi-grid">
          <KpiCard label="Em férias hoje" value={eventKpis.onVacationToday} variant="primary" />
          <KpiCard label="Agendadas" value={eventKpis.scheduled} variant="warning" />
          <KpiCard label="Em andamento" value={eventKpis.inProgress} variant="info" />
          <KpiCard label="Concluídas" value={eventKpis.completed} variant="success" />
        </div>
      );
    }
    if (activeSection === 'treinamentos') {
      return (
        <div className="kpi-grid">
          <KpiCard label="Treinamentos no mês" value={dashboard?.trainingsThisMonth ?? 0} variant="primary" />
          <KpiCard label="Agendados" value={eventKpis.scheduled} variant="warning" />
          <KpiCard label="Em andamento" value={eventKpis.inProgress} variant="info" />
          <KpiCard label="Concluídos" value={eventKpis.completed} variant="success" />
        </div>
      );
    }
    if (activeSection === 'avaliacoes') {
      return (
        <div className="kpi-grid">
          <KpiCard label="Avaliações no trimestre" value={dashboard?.reviewsThisQuarter ?? 0} variant="primary" />
          <KpiCard label="Pendentes" value={eventKpis.scheduled + eventKpis.inProgress} variant="warning" />
          <KpiCard label="Concluídas" value={eventKpis.completed} variant="success" />
          <KpiCard label="Total registradas" value={eventKpis.total} variant="neutral" />
        </div>
      );
    }
    if (activeSection === 'plantoes') {
      return (
        <div className="kpi-grid">
          <KpiCard label="Plantões noturnos (semana)" value={shifts.length} variant="primary" />
          <KpiCard label="Noturnos (dashboard)" value={dashboard?.nightShiftsThisWeek ?? 0} variant="info" />
          <KpiCard label="Escalas na semana" value={dashboard?.shiftsThisWeek ?? 0} variant="neutral" />
          <KpiCard label="Colaboradores ativos" value={dashboard?.activeEmployees ?? 0} variant="success" />
        </div>
      );
    }
    if (activeSection === 'escalas') {
      return (
        <div className="kpi-grid">
          <KpiCard label="Escalas na semana" value={shifts.length} variant="primary" />
          <KpiCard label="Plantões noturnos" value={dashboard?.nightShiftsThisWeek ?? 0} variant="info" />
          <KpiCard label="Colaboradores ativos" value={dashboard?.activeEmployees ?? 0} variant="success" />
          <KpiCard label="Em férias hoje" value={dashboard?.onVacationToday ?? 0} variant="warning" />
        </div>
      );
    }
    return (
      <div className="kpi-grid">
        <KpiCard label="Colaboradores ativos" value={dashboard?.activeEmployees ?? employees.length} variant="primary" />
        <KpiCard label="Escalas na semana" value={dashboard?.shiftsThisWeek ?? 0} variant="info" />
        <KpiCard label="Em férias hoje" value={dashboard?.onVacationToday ?? 0} variant="warning" />
        <KpiCard
          label="Última folha (líquido)"
          value={dashboard?.latestPayrollNet != null ? formatBrl(dashboard.latestPayrollNet) : '—'}
          variant="success"
        />
      </div>
    );
  }

  return (
    <FeegowRhScreenLayout>
    <>
      <PageHeader eyebrow="Administrativo" title={activeSection ? breadcrumb.title : 'Recursos Humanos'} subtitle="Colaboradores, departamentos e escalas — cadastro completo com foto.">
        <div className="page-header-tools">
          {usesWeekView && (
            <div className="filter-field" style={{ display: 'flex', alignItems: 'flex-end', gap: 8 }}>
              <button className="btn btn-secondary btn-sm" type="button" onClick={() => setWeekAnchor(shiftWeek(weekAnchor, -1))}>← Semana anterior</button>
              <span style={{ padding: '0 8px', fontSize: 14 }}>
                {formatBrDate(weekRange.from)} — {formatBrDate(weekRange.to)}
              </span>
              <button className="btn btn-secondary btn-sm" type="button" onClick={() => setWeekAnchor(shiftWeek(weekAnchor, 1))}>Próxima semana →</button>
            </div>
          )}
          {activeSection === '' && (
            <button className="btn" type="button" onClick={openCreateEmployee}>+ Novo colaborador</button>
          )}
        </div>
      </PageHeader>

      <ModuleNav basePath="/rh" tabs={hrTabs} contextId="humanResources" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {renderKpis()}

      {activeSection === '' && dashboard && (
        <div className="card" style={{ marginBottom: 20, display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16, flexWrap: 'wrap' }}>
          <div>
            <strong>Folha de pagamento</strong>
            <div style={{ color: 'var(--text-muted)', fontSize: 14 }}>
              {dashboard.payrollEmployeeCount > 0
                ? `${dashboard.payrollEmployeeCount} colaboradores na última competência · líquido ${formatBrl(dashboard.latestPayrollNet ?? 0)}`
                : 'Nenhuma folha gerada ainda.'}
            </div>
          </div>
          <Link className="btn btn-secondary" to="/rh/folha">Abrir folha →</Link>
        </div>
      )}

      {(activeSection === 'ferias' || activeSection === 'treinamentos' || activeSection === 'avaliacoes') && (
        <form className="card form-grid" style={{ marginBottom: 20 }} onSubmit={async (e) => {
          e.preventDefault();
          if (!hrForm.employeeId || !hrForm.title.trim() || !hrForm.detail.trim()) return;
          setError('');
          setSuccess('');
          const section = activeSection as HrEventSection;
          const payload: CreateEmployeeHrEventRequest = {
            employeeId: hrForm.employeeId,
            eventType: hrEventTypeForSection(section),
            title: hrForm.title.trim(),
            detail: hrForm.detail.trim(),
            startDate: hrForm.start || toIsoDate(new Date()),
            endDate: hrForm.end || undefined,
          };
          try {
            await api.createHrEvent(payload);
            const list = await api.getHrEvents(hrEventTypeForSection(section));
            setHrEvents(list);
            setHrForm({ employeeId: '', title: '', detail: '', start: '', end: '' });
            setSuccess('Registro salvo.');
          } catch (err) {
            setError(err instanceof Error ? err.message : 'Erro ao salvar registro.');
          }
        }}>
          <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>{breadcrumb.title}</h3>
          <div className="form-field"><label>Colaborador</label>
            <select value={hrForm.employeeId} onChange={(e) => setHrForm({ ...hrForm, employeeId: e.target.value })} required>
              <option value="">Selecione</option>
              {employees.map((e) => <option key={e.id} value={e.id}>{e.fullName}</option>)}
            </select>
          </div>
          <div className="form-field"><label>Título</label>
            <input
              value={hrForm.title}
              onChange={(e) => setHrForm({ ...hrForm, title: e.target.value })}
              required
              placeholder={activeSection === 'ferias' ? 'Ex.: Férias — julho' : activeSection === 'treinamentos' ? 'Ex.: Treinamento ACLS' : 'Ex.: Avaliação semestral'}
            />
          </div>
          <div className="form-field"><label>Data início</label>
            <input type="date" value={hrForm.start} onChange={(e) => setHrForm({ ...hrForm, start: e.target.value })} />
          </div>
          {(activeSection === 'ferias' || activeSection === 'treinamentos') && (
            <div className="form-field"><label>Data fim</label>
              <input type="date" value={hrForm.end} onChange={(e) => setHrForm({ ...hrForm, end: e.target.value })} />
            </div>
          )}
          <div className="form-field full"><label>Detalhe</label>
            <input
              value={hrForm.detail}
              onChange={(e) => setHrForm({ ...hrForm, detail: e.target.value })}
              required
              placeholder={detailPlaceholder(activeSection as HrEventSection)}
            />
          </div>
          <div className="form-actions"><button className="btn" type="submit">Registrar</button></div>
        </form>
      )}

      {(activeSection === 'ferias' || activeSection === 'treinamentos' || activeSection === 'avaliacoes') && (
        <div className="card-panel appt-panel" style={{ marginBottom: 20 }}>
          <table className="data-table">
            <thead><tr><th>Data</th><th>Colaborador</th><th>Título</th><th>Detalhe</th><th>Status</th></tr></thead>
            <tbody>
              {hrEventsLoading && <tr><td colSpan={5}>Carregando...</td></tr>}
              {!hrEventsLoading && hrEvents.map((r) => {
                const status = hrEventStatus(r.startDate, r.endDate);
                return (
                  <tr key={r.id}>
                    <td>{formatBrDate(r.startDate)}{r.endDate ? ` — ${formatBrDate(r.endDate)}` : ''}</td>
                    <td>{r.employeeName}</td>
                    <td>{r.title}</td>
                    <td>{r.detail}</td>
                    <td><span className={statusBadgeClass(status)}>{status}</span></td>
                  </tr>
                );
              })}
              {!hrEventsLoading && hrEvents.length === 0 && <tr><td colSpan={5}>Nenhum registro.</td></tr>}
            </tbody>
          </table>
        </div>
      )}

      {activeSection === '' && (
      <div className="card-panel appt-panel" style={{ marginBottom: 20 }}>
        <div className="card-panel-header">Colaboradores</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Foto</th><th>Nome</th><th>Cargo</th><th>Função</th><th>Departamento</th><th>Salário base</th><th>Admissão</th><th>Ações</th></tr></thead>
            <tbody>
              {employees.map((e) => (
                <tr key={e.id}>
                  <td><PersonAvatar name={e.fullName} size={36} /></td>
                  <td><strong>{e.fullName}</strong>{e.email && <div className="table-sub">{e.email}</div>}</td>
                  <td>{e.jobTitle ?? '—'}</td>
                  <td>{employeeRoleLabels[e.role]}</td>
                  <td>{e.departmentName}</td>
                  <td>{formatBrl(e.baseSalary)}</td>
                  <td>{formatBrDate(e.hireDate)}</td>
                  <td><button className="btn btn-secondary btn-sm" type="button" onClick={() => openEditEmployee(e.id)}>Editar</button></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {(activeSection === 'escalas' || activeSection === 'plantoes') && (
      <div className="card" style={{ marginBottom: 20 }}>
        <h2 style={{ marginTop: 0 }}>
          {activeSection === 'plantoes' ? 'Plantões noturnos' : 'Escalas da semana'}
          {' — '}{formatBrDate(weekRange.from)} a {formatBrDate(weekRange.to)}
        </h2>
        {activeSection === 'escalas' && (
        <form className="form-grid" onSubmit={handleCreateShift}>
          <div className="form-field">
            <label>Colaborador</label>
            <select required value={shiftForm.employeeId} onChange={(e) => setShiftForm({ ...shiftForm, employeeId: e.target.value })}>
              <option value="">Selecione</option>
              {employees.map((e) => <option key={e.id} value={e.id}>{e.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Departamento</label>
            <select required value={shiftForm.departmentId} onChange={(e) => setShiftForm({ ...shiftForm, departmentId: e.target.value })}>
              <option value="">Selecione</option>
              {departments.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Data</label>
            <input type="date" required value={shiftForm.shiftDate} onChange={(e) => setShiftForm({ ...shiftForm, shiftDate: e.target.value })} />
          </div>
          <div className="form-field">
            <label>Turno</label>
            <select value={shiftForm.shiftType} onChange={(e) => setShiftForm({ ...shiftForm, shiftType: Number(e.target.value) })}>
              {Object.entries(shiftTypeLabels).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </div>
          <div className="form-field full"><button className="btn" type="submit">Registrar escala</button></div>
        </form>
        )}
        <table className="data-table" style={{ marginTop: 16 }}>
          <thead><tr><th>Data</th><th>Colaborador</th><th>Departamento</th><th>Turno</th><th>Ações</th></tr></thead>
          <tbody>
            {shifts.map((s) => (
              <tr key={s.id}>
                <td>{formatBrDate(s.shiftDate)}</td>
                <td>{s.employeeName}</td>
                <td>{s.departmentName}</td>
                <td>{shiftTypeLabels[s.shiftType]}</td>
                <td>
                  <button className="btn btn-secondary btn-sm" type="button" onClick={() => handleDeleteShift(s.id)}>Excluir</button>
                </td>
              </tr>
            ))}
            {shifts.length === 0 && <tr><td colSpan={5}>Nenhuma escala.</td></tr>}
          </tbody>
        </table>
      </div>
      )}

      <Modal open={showEmpModal} onClose={() => setShowEmpModal(false)} title={editingId ? 'Editar colaborador' : 'Novo colaborador'} subtitle="Cadastro completo do funcionário." width="lg">
        <form className="form-grid" onSubmit={handleCreateEmployee}>
          <div className="form-field full">
            <div className="form-section-title">Foto</div>
            <PhotoCapture name={empForm.fullName || 'Colaborador'} value={empForm.photoData} onChange={(photoData) => setEmpForm({ ...empForm, photoData: photoData ?? undefined })} />
          </div>
          <div className="form-field full"><div className="form-section-title">Identificação</div></div>
          <div className="form-field"><label>Nome completo *</label><input required value={empForm.fullName} onChange={(e) => setEmpForm({ ...empForm, fullName: e.target.value })} /></div>
          <div className="form-field"><label>Nome social</label><input value={empForm.socialName} onChange={(e) => setEmpForm({ ...empForm, socialName: e.target.value })} /></div>
          <div className="form-field"><label>CPF</label><input value={empForm.cpf} onChange={(e) => setEmpForm({ ...empForm, cpf: e.target.value })} /></div>
          <div className="form-field"><label>RG</label><input value={empForm.rg} onChange={(e) => setEmpForm({ ...empForm, rg: e.target.value })} /></div>
          <div className="form-field"><label>Data de nascimento</label><input type="date" value={empForm.birthDate} onChange={(e) => setEmpForm({ ...empForm, birthDate: e.target.value })} /></div>
          <div className="form-field">
            <label>Sexo</label>
            <select value={empForm.gender} onChange={(e) => setEmpForm({ ...empForm, gender: Number(e.target.value) })}>
              <option value={0}>Não informado</option>
              <option value={1}>Masculino</option>
              <option value={2}>Feminino</option>
              <option value={3}>Outro</option>
            </select>
          </div>
          <div className="form-field full"><div className="form-section-title">Vínculo</div></div>
          <div className="form-field"><label>Cargo / função exercida</label><input value={empForm.jobTitle} onChange={(e) => setEmpForm({ ...empForm, jobTitle: e.target.value })} /></div>
          <div className="form-field">
            <label>Categoria RH *</label>
            <select value={empForm.role} onChange={(e) => setEmpForm({ ...empForm, role: Number(e.target.value) })}>
              {Object.entries(employeeRoleLabels).map(([v, l]) => <option key={v} value={v}>{l}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label>Departamento *</label>
            <select required value={empForm.departmentId} onChange={(e) => setEmpForm({ ...empForm, departmentId: e.target.value })}>
              <option value="">Selecione</option>
              {departments.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
            </select>
          </div>
          <div className="form-field"><label>Data de admissão *</label><input type="date" required value={empForm.hireDate} onChange={(e) => setEmpForm({ ...empForm, hireDate: e.target.value })} /></div>
          <div className="form-field"><label>Salário base (R$)</label><input type="number" step="0.01" min={0} value={empForm.baseSalary ?? ''} onChange={(e) => setEmpForm({ ...empForm, baseSalary: Number(e.target.value) })} /></div>
          <div className="form-field full"><div className="form-section-title">Contato</div></div>
          <div className="form-field"><label>E-mail</label><input type="email" value={empForm.email} onChange={(e) => setEmpForm({ ...empForm, email: e.target.value })} /></div>
          <div className="form-field"><label>Telefone</label><input value={empForm.phone} onChange={(e) => setEmpForm({ ...empForm, phone: e.target.value })} /></div>
          <div className="form-field"><label>Celular</label><input value={empForm.mobilePhone} onChange={(e) => setEmpForm({ ...empForm, mobilePhone: e.target.value })} /></div>
          <div className="form-field full"><div className="form-section-title">Endereço</div></div>
          <AddressFields values={empForm} onChange={(patch) => setEmpForm({ ...empForm, ...patch })} prefix="emp-" />
          <div className="form-field full"><div className="form-section-title">Emergência</div></div>
          <div className="form-field"><label>Contato</label><input value={empForm.emergencyContactName} onChange={(e) => setEmpForm({ ...empForm, emergencyContactName: e.target.value })} /></div>
          <div className="form-field"><label>Telefone</label><input value={empForm.emergencyContactPhone} onChange={(e) => setEmpForm({ ...empForm, emergencyContactPhone: e.target.value })} /></div>
          <div className="form-field full"><label>Observações</label><textarea rows={3} value={empForm.notes} onChange={(e) => setEmpForm({ ...empForm, notes: e.target.value })} /></div>
          {editingId && (
            <div className="form-field full checkbox">
              <label>
                <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
                Cadastro ativo
              </label>
            </div>
          )}
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowEmpModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Salvar colaborador</button>
          </div>
        </form>
      </Modal>
    </>
    </FeegowRhScreenLayout>
  );
}

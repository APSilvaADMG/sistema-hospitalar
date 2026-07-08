import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api, labOrderStatusLabels, type HealthInsuranceDto, type LabExamCatalogDto, type LabOrderDto,
  type PatientDto, type ProfessionalDto, type SpecialtyClinicalCatalogDto,
} from '../api/client';
import { ClinicalGuideCaptureModal } from '../components/funi/ClinicalGuideCaptureModal';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { labTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { Link, useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../utils/dateUtils';
import { SpecialtyCatalogPanel } from '../components/SpecialtyCatalogPanel';
import { useAuth } from '../auth/AuthContext';
import { printLabReport } from '../utils/printTemplates';

export function LaboratoryPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/laboratorio');
  const activeSection = section || '';

  const { hasPermission, user } = useAuth();
  const canManage = hasPermission('pep.read', 'pep.write');
  const [catalog, setCatalog] = useState<SpecialtyClinicalCatalogDto | null>(null);
  const [exams, setExams] = useState<LabExamCatalogDto[]>([]);
  const [orders, setOrders] = useState<LabOrderDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [selectedExams, setSelectedExams] = useState<string[]>([]);
  const [form, setForm] = useState({
    patientId: '',
    requestingProfessionalId: user?.professionalId ?? '',
    notes: '',
  });
  const [resultForms, setResultForms] = useState<Record<string, { value: string; isAbnormal: boolean }>>({});
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);
  const [clinicalLab, setClinicalLab] = useState<{ patientId: string; labOrderId: string; label: string } | null>(null);

  async function loadOrders() {
    setOrders(await api.getLabOrders());
  }

  async function loadCatalogForProfessional(professionalId: string) {
    if (!professionalId) {
      setCatalog(null);
      setExams(await api.getLabExams());
      return;
    }
    const data = await api.getClinicalCatalogByProfessional(professionalId);
    setCatalog(data);
    setExams(data.labExams);
    setSelectedExams([]);
  }

  async function load() {
    const [orderList, patientList, profList, insuranceList] = await Promise.all([
      api.getLabOrders(), api.getPatients(undefined, 1), api.getProfessionals(), api.getHealthInsurances(),
    ]);
    setOrders(orderList);
    setPatients(patientList.items);
    setProfessionals(profList);
    setInsurances(Array.isArray(insuranceList) ? insuranceList : []);
    if (form.requestingProfessionalId) {
      await loadCatalogForProfessional(form.requestingProfessionalId);
    } else {
      setExams(await api.getLabExams());
    }
  }

  useEffect(() => { load().catch(console.error); }, []);

  useEffect(() => {
    if (user?.professionalId && !form.requestingProfessionalId) {
      setForm((f) => ({ ...f, requestingProfessionalId: user.professionalId! }));
      loadCatalogForProfessional(user.professionalId).catch(console.error);
    }
  }, [user?.professionalId]);

  function toggleExam(id: string) {
    setSelectedExams((prev) => prev.includes(id) ? prev.filter((e) => e !== id) : [...prev, id]);
  }

  async function handleProfessionalChange(professionalId: string) {
    setForm((f) => ({ ...f, requestingProfessionalId: professionalId }));
    await loadCatalogForProfessional(professionalId);
  }

  async function handleCreateOrder(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');
    try {
      await api.createLabOrder({
        patientId: form.patientId,
        requestingProfessionalId: form.requestingProfessionalId,
        notes: form.notes,
        examCatalogIds: selectedExams,
      });
      setSuccess('Pedido de exame criado.');
      setSelectedExams([]);
      setForm((f) => ({ ...f, patientId: '', notes: '' }));
      setShowModal(false);
      await loadOrders();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar pedido.');
    }
  }

  async function handleRegisterResult(itemId: string, exam: LabExamCatalogDto) {
    const rf = resultForms[itemId] ?? { value: '', isAbnormal: false };
    if (!rf.value) return;
    setError('');
    setSuccess('');
    try {
      await api.registerLabResult({
        orderItemId: itemId, value: rf.value, unit: exam.unit,
        referenceRange: exam.referenceRange, isAbnormal: rf.isAbnormal,
      });
      setSuccess('Resultado registrado.');
      await loadOrders();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar resultado.');
    }
  }

  const pending = orders.filter((o) => o.status === 1 || o.status === 2).length;
  const completed = orders.filter((o) => o.status === 3).length;
  const urgent = orders.filter((o) => o.items.some((i) => !i.result)).length;

  const orderRows = useMemo(() => (
    orders.flatMap((order) =>
      order.items.map((item) => ({
        orderId: order.id,
        patientId: order.patientId,
        patientName: order.patientName,
        orderStatus: order.status,
        createdAt: order.createdAt,
        item,
      })),
    )
  ), [orders]);

  const filteredRows = useMemo(() => {
    return orderRows
      .filter((row) => {
        if (activeSection === 'coleta') return row.orderStatus === 1;
        if (activeSection === 'processamento') return row.orderStatus === 2;
        if (activeSection === 'resultados') return !!row.item.result;
        if (activeSection === 'laudos') return row.orderStatus === 3 && !!row.item.result;
        return true;
      })
      .filter((row) => !statusFilter || row.orderStatus === Number(statusFilter))
      .filter((row) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          row.patientName.toLowerCase().includes(term)
          || row.item.examName.toLowerCase().includes(term)
        );
      });
  }, [orderRows, statusFilter, search, activeSection]);

  const ordersById = useMemo(() => new Map(orders.map((o) => [o.id, o])), [orders]);

  function handlePrintReport(orderId: string) {
    const order = ordersById.get(orderId);
    if (order) printLabReport(order);
  }

  return (
    <>
      <PageHeader eyebrow="Diagnóstico" title={activeSection ? breadcrumb.title : 'Laboratório'} subtitle="Solicitações filtradas pela especialidade do médico.">
        {canManage && (activeSection === '' || activeSection === 'coleta') && (
          <button className="btn" type="button" onClick={() => setShowModal(true)}>
            + Solicitar exames
          </button>
        )}
      </PageHeader>

      <ModuleNav basePath="/laboratorio" tabs={labTabs} contextId="laboratory" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {activeSection === 'integracoes' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Integrações LIS / HL7</h3>
          <p>Conecte o laboratório a sistemas externos via HL7, FHIR ou integrador dedicado.</p>
          <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
            <Link to="/integracoes/laboratorio" className="btn btn-secondary">Painel de integrações</Link>
            <Link to="/integracoes/hl7" className="btn btn-secondary">HL7</Link>
          </div>
        </div>
      )}

      {activeSection === 'patologia' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Patologia</h3>
          <p>Filtro de itens de patologia dentro do hub do laboratório (sem duplicar LIS).</p>
          <div className="card-panel appt-panel" style={{ marginTop: 12 }}>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <table className="data-table">
                <thead><tr><th>Paciente</th><th>Exame</th><th>Status</th><th>Data</th></tr></thead>
                <tbody>
                  {filteredRows
                    .filter((row) => row.item.examName.toLowerCase().includes('pato') || row.item.examName.toLowerCase().includes('biops'))
                    .map((row) => (
                      <tr key={`p-${row.item.id}`}>
                        <td>{row.patientName}</td>
                        <td>{row.item.examName}</td>
                        <td>{labOrderStatusLabels[row.orderStatus]}</td>
                        <td>{formatBrDateTime(row.createdAt)}</td>
                      </tr>
                    ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {activeSection !== 'integracoes' && activeSection !== 'patologia' && (
      <>
      <div className="kpi-grid">
        <KpiCard label="Urgentes / pendentes" value={urgent} variant="danger" />
        <KpiCard label="Em processamento" value={pending} variant="info" />
        <KpiCard label="Concluídos" value={completed} variant="success" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Pedidos — {filteredRows.length} exame(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="labStatus">Status</label>
            <select id="labStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(labOrderStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="labSearch">Buscar</label>
            <input
              id="labSearch"
              placeholder="Paciente ou exame..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Paciente</th>
                <th>Exame</th>
                <th>Status</th>
                <th>Data</th>
                <th>Resultado</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filteredRows.map((row) => {
                const exam = exams.find((e) => e.id === row.item.examCatalogId) ?? exams[0];
                return (
                  <tr key={row.item.id}>
                    <td><strong>{row.patientName}</strong></td>
                    <td>{row.item.examName}</td>
                    <td><span className="badge">{labOrderStatusLabels[row.orderStatus]}</span></td>
                    <td>{formatBrDateTime(row.createdAt)}</td>
                    <td>
                      {row.item.result ? (
                        <span className={row.item.result.isAbnormal ? 'text-danger' : 'text-success'}>
                          {row.item.result.value} {row.item.result.unit}
                          {row.item.result.referenceRange && ` (ref: ${row.item.result.referenceRange})`}
                        </span>
                      ) : (
                        <span className="text-muted">Pendente</span>
                      )}
                    </td>
                    <td>
                      <div className="table-actions" style={{ flexWrap: 'wrap' }}>
                        {!row.item.result && canManage && exam ? (
                          <div className="result-inline">
                            <input
                              placeholder="Resultado"
                              value={resultForms[row.item.id]?.value ?? ''}
                              onChange={(e) => setResultForms({
                                ...resultForms,
                                [row.item.id]: {
                                  ...resultForms[row.item.id],
                                  value: e.target.value,
                                  isAbnormal: resultForms[row.item.id]?.isAbnormal ?? false,
                                },
                              })}
                            />
                            <label>
                              <input
                                type="checkbox"
                                checked={resultForms[row.item.id]?.isAbnormal ?? false}
                                onChange={(e) => setResultForms({
                                  ...resultForms,
                                  [row.item.id]: {
                                    value: resultForms[row.item.id]?.value ?? '',
                                    isAbnormal: e.target.checked,
                                  },
                                })}
                              />
                              {' '}Alterado
                            </label>
                            <button
                              type="button"
                              className="btn btn-secondary btn-sm"
                              onClick={() => handleRegisterResult(row.item.id, exam)}
                            >
                              Lançar
                            </button>
                          </div>
                        ) : row.item.result ? (
                          <button
                            type="button"
                            className="btn btn-secondary btn-sm"
                            onClick={() => handlePrintReport(row.orderId)}
                          >
                            Relatório
                          </button>
                        ) : null}
                        <button
                          type="button"
                          className="btn btn-secondary btn-sm"
                          onClick={() => setClinicalLab({
                            patientId: row.patientId,
                            labOrderId: row.orderId,
                            label: `Lab — ${row.item.examName}`,
                          })}
                        >
                          Dados TISS
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
              {filteredRows.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhum pedido de exame.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      </>
      )}

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Solicitar exames"
        subtitle="Exames filtrados pela especialidade do médico solicitante."
        width="lg"
      >
        <form onSubmit={handleCreateOrder}>
          <div className="form-grid">
            <div className="form-field">
              <label htmlFor="labPatient">Paciente</label>
              <select id="labPatient" required value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
                <option value="">Selecione</option>
                {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
            <div className="form-field">
              <label htmlFor="labProfessional">Médico solicitante</label>
              <select
                id="labProfessional"
                required
                value={form.requestingProfessionalId}
                onChange={(e) => handleProfessionalChange(e.target.value)}
              >
                <option value="">Selecione</option>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName} — {p.specialtyName}</option>
                ))}
              </select>
            </div>
            <div className="form-field full">
              <label htmlFor="labNotes">Observações</label>
              <input id="labNotes" value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} />
            </div>
          </div>

          <SpecialtyCatalogPanel
            specialtyName={catalog?.specialtyName}
            labExams={exams}
            imagingProcedures={[]}
            medications={[]}
            selectedLabIds={selectedExams}
            onLabToggle={toggleExam}
            showImaging={false}
            showMeds={false}
          />

          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit" disabled={selectedExams.length === 0}>
              Solicitar {selectedExams.length} exame(s)
            </button>
          </div>
        </form>
      </Modal>

      {clinicalLab && (
        <ClinicalGuideCaptureModal
          open
          onClose={() => setClinicalLab(null)}
          guideType={2}
          patients={patients}
          insurances={insurances}
          patientId={clinicalLab.patientId}
          clinicalContext={{
            labOrderId: clinicalLab.labOrderId,
            label: clinicalLab.label,
          }}
        />
      )}
    </>
  );
}

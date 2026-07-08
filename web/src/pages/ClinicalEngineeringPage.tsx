import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  medicalEquipmentStatusLabels,
  workOrderStatusLabels,
  type MaintenanceWorkOrderDto,
  type MedicalEquipmentDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { clinicalEngTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDate } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

export function ClinicalEngineeringPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/engenharia-clinica');
  const activeSection = section || '';

  const { hasPermission } = useAuth();
  const [equipment, setEquipment] = useState<MedicalEquipmentDto[]>([]);
  const [workOrders, setWorkOrders] = useState<MaintenanceWorkOrderDto[]>([]);
  const [equipForm, setEquipForm] = useState({
    name: '', assetTag: '', manufacturer: '', model: '', location: '', nextMaintenanceDate: '',
  });
  const [orderForm, setOrderForm] = useState({
    equipmentId: '', title: '', description: '', technicianName: '',
  });
  const [showEquipModal, setShowEquipModal] = useState(false);
  const [showOrderModal, setShowOrderModal] = useState(false);

  useEffect(() => { load(); }, []);

  async function load() {
    const [e, w] = await Promise.all([api.getMedicalEquipment(), api.getMaintenanceWorkOrders()]);
    setEquipment(e);
    setWorkOrders(w);
  }

  const stats = useMemo(() => ({
    total: equipment.length,
    operational: equipment.filter((eq) => eq.status === 'Operational').length,
    maintenance: equipment.filter((eq) => eq.status === 'Maintenance').length,
    openOrders: workOrders.filter((w) => w.status !== 'Completed' && w.status !== 'Cancelled').length,
  }), [equipment, workOrders]);

  async function handleCreateEquipment(e: FormEvent) {
    e.preventDefault();
    await api.createMedicalEquipment({
      name: equipForm.name,
      assetTag: equipForm.assetTag,
      manufacturer: equipForm.manufacturer || undefined,
      model: equipForm.model || undefined,
      location: equipForm.location || undefined,
      nextMaintenanceDate: equipForm.nextMaintenanceDate || undefined,
    });
    setEquipForm({ name: '', assetTag: '', manufacturer: '', model: '', location: '', nextMaintenanceDate: '' });
    setShowEquipModal(false);
    await load();
  }

  async function handleCreateOrder(e: FormEvent) {
    e.preventDefault();
    await api.createMaintenanceWorkOrder(orderForm);
    setOrderForm({ equipmentId: '', title: '', description: '', technicianName: '' });
    setShowOrderModal(false);
    await load();
  }

  async function completeOrder(id: string) {
    await api.updateWorkOrderStatus(id, 'Completed');
    await load();
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito à recepção.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Apoio clínico"
        title={activeSection ? breadcrumb.title : 'Engenharia Clínica'}
        subtitle="Patrimônio de equipamentos médicos e ordens de manutenção preventiva/corretiva."
      >
        {(activeSection === '' || activeSection === 'calibracoes') && (
          <button className="btn btn-secondary" type="button" onClick={() => setShowEquipModal(true)}>+ Equipamento</button>
        )}
        {(activeSection === '' || activeSection === 'manutencoes') && (
          <button className="btn" type="button" onClick={() => setShowOrderModal(true)}>+ Ordem de serviço</button>
        )}
      </PageHeader>

      <ModuleNav basePath="/engenharia-clinica" tabs={clinicalEngTabs} />

      <div className="kpi-grid">
        <KpiCard label="Equipamentos" value={stats.total} variant="primary" />
        <KpiCard label="Operacionais" value={stats.operational} variant="success" />
        <KpiCard label="Em manutenção" value={stats.maintenance} variant="warning" />
        <KpiCard label="OS abertas" value={stats.openOrders} variant="info" />
      </div>

      {activeSection === 'indicadores' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Indicadores de engenharia clínica</h3>
          <ul className="bi-progress-list">
            <li>Equipamentos operacionais: {stats.operational}/{stats.total}</li>
            <li>Em manutenção: {stats.maintenance}</li>
            <li>OS abertas: {stats.openOrders}</li>
          </ul>
        </div>
      )}

      {activeSection === 'contratos' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Contratos de manutenção</h3>
          <p>Gestão de contratos com fornecedores de equipamentos e calibração.</p>
        </div>
      )}

      {(activeSection === '' || activeSection === 'calibracoes') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">
          {activeSection === 'calibracoes' ? 'Calibrações programadas' : `Equipamentos — ${equipment.length} item(ns)`}
        </div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Tag</th><th>Nome</th><th>Local</th><th>Status</th><th>Próx. manutenção</th></tr>
            </thead>
            <tbody>
              {(activeSection === 'calibracoes' ? equipment.filter((eq) => eq.nextMaintenanceDate) : equipment).map((eq) => (
                <tr key={eq.id}>
                  <td>{eq.assetTag}</td>
                  <td>{eq.name}</td>
                  <td>{eq.location ?? '—'}</td>
                  <td><span className="badge">{medicalEquipmentStatusLabels[eq.status] ?? eq.status}</span></td>
                  <td>{eq.nextMaintenanceDate ? formatBrDate(eq.nextMaintenanceDate) : '—'}</td>
                </tr>
              ))}
              {equipment.length === 0 && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum equipamento</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {(activeSection === '' || activeSection === 'manutencoes') && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Ordens de serviço — {workOrders.length} OS</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Equipamento</th><th>Título</th><th>Técnico</th><th>Status</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {workOrders.map((w) => (
                <tr key={w.id}>
                  <td>{w.equipmentName}</td>
                  <td>{w.title}</td>
                  <td>{w.technicianName ?? '—'}</td>
                  <td><span className="badge">{workOrderStatusLabels[w.status] ?? w.status}</span></td>
                  <td>
                    {w.status !== 'Completed' && w.status !== 'Cancelled' && (
                      <button className="btn btn-secondary btn-sm" type="button" onClick={() => completeOrder(w.id)}>Concluir</button>
                    )}
                  </td>
                </tr>
              ))}
              {workOrders.length === 0 && (
                <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma OS</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      <Modal open={showEquipModal} onClose={() => setShowEquipModal(false)} title="Cadastrar equipamento" width="md">
        <form onSubmit={handleCreateEquipment} className="form-grid">
          <div className="form-field">
            <label htmlFor="eqName">Nome *</label>
            <input id="eqName" value={equipForm.name} onChange={(e) => setEquipForm({ ...equipForm, name: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="eqTag">Tag patrimonial *</label>
            <input id="eqTag" value={equipForm.assetTag} onChange={(e) => setEquipForm({ ...equipForm, assetTag: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="eqManufacturer">Fabricante</label>
            <input id="eqManufacturer" value={equipForm.manufacturer} onChange={(e) => setEquipForm({ ...equipForm, manufacturer: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="eqModel">Modelo</label>
            <input id="eqModel" value={equipForm.model} onChange={(e) => setEquipForm({ ...equipForm, model: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="eqLocation">Localização</label>
            <input id="eqLocation" value={equipForm.location} onChange={(e) => setEquipForm({ ...equipForm, location: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="eqMaintenance">Próx. manutenção</label>
            <input id="eqMaintenance" type="date" value={equipForm.nextMaintenanceDate} onChange={(e) => setEquipForm({ ...equipForm, nextMaintenanceDate: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowEquipModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Cadastrar</button>
          </div>
        </form>
      </Modal>

      <Modal open={showOrderModal} onClose={() => setShowOrderModal(false)} title="Nova ordem de serviço" width="md">
        <form onSubmit={handleCreateOrder} className="form-grid">
          <div className="form-field">
            <label htmlFor="woEquipment">Equipamento *</label>
            <select id="woEquipment" value={orderForm.equipmentId} onChange={(e) => setOrderForm({ ...orderForm, equipmentId: e.target.value })} required>
              <option value="">Selecione...</option>
              {equipment.map((eq) => <option key={eq.id} value={eq.id}>{eq.assetTag} — {eq.name}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="woTitle">Título *</label>
            <input id="woTitle" value={orderForm.title} onChange={(e) => setOrderForm({ ...orderForm, title: e.target.value })} required />
          </div>
          <div className="form-field full">
            <label htmlFor="woDesc">Descrição *</label>
            <textarea id="woDesc" rows={3} value={orderForm.description} onChange={(e) => setOrderForm({ ...orderForm, description: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="woTech">Técnico responsável</label>
            <input id="woTech" value={orderForm.technicianName} onChange={(e) => setOrderForm({ ...orderForm, technicianName: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowOrderModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Abrir OS</button>
          </div>
        </form>
      </Modal>
    </>
  );
}

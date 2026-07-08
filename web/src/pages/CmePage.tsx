import { type FormEvent, useEffect, useMemo, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  api,
  instrumentKitStatusLabels,
  sterilizationCycleStatusLabels,
  sterilizationMethodLabels,
  type InstrumentKitDto,
  type SterilizationCycleDto,
} from '../api/client';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { cmeTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrDate } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

export function CmePage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/cme');
  const activeSection = section || '';
  const canManage = hasPermission('patients.create', 'reports.read');
  const [kits, setKits] = useState<InstrumentKitDto[]>([]);
  const [cycles, setCycles] = useState<SterilizationCycleDto[]>([]);
  const [kitForm, setKitForm] = useState({ name: '', code: '', description: '' });
  const [cycleForm, setCycleForm] = useState({
    instrumentKitId: '', method: 'Steam', sterilizerName: 'Autoclave CME-01', operatorName: '',
  });
  const [showKitModal, setShowKitModal] = useState(false);
  const [showCycleModal, setShowCycleModal] = useState(false);

  useEffect(() => { load(); }, []);

  async function load() {
    const [k, c] = await Promise.all([api.getInstrumentKits(), api.getSterilizationCycles()]);
    setKits(k);
    setCycles(c);
  }

  const today = new Date().toISOString().slice(0, 10);

  const stats = useMemo(() => ({
    totalKits: kits.length,
    availableKits: kits.filter((k) => k.status === 'Available' || k.status === 'Sterile').length,
    expiredKits: kits.filter((k) => k.sterilityExpiration && k.sterilityExpiration < today).length,
    activeCycles: cycles.filter((c) => c.status === 'InProgress').length,
    pendingCycles: cycles.filter((c) => c.status === 'Pending').length,
  }), [kits, cycles, today]);

  async function handleCreateKit(e: FormEvent) {
    e.preventDefault();
    await api.createInstrumentKit(kitForm);
    setKitForm({ name: '', code: '', description: '' });
    setShowKitModal(false);
    await load();
  }

  async function handleCreateCycle(e: FormEvent) {
    e.preventDefault();
    await api.createSterilizationCycle(cycleForm);
    setShowCycleModal(false);
    await load();
  }

  async function startCycle(id: string) {
    await api.startSterilizationCycle(id);
    await load();
  }

  async function completeCycle(id: string) {
    const exp = new Date();
    exp.setDate(exp.getDate() + 30);
    await api.completeSterilizationCycle(id, exp.toISOString().slice(0, 10));
    await load();
  }

  async function rejectCycle(id: string) {
    const reason = window.prompt('Motivo da rejeição do ciclo (opcional):') ?? undefined;
    await api.rejectSterilizationCycle(id, reason);
    await load();
  }

  return (
    <>
      <PageHeader
        eyebrow="Volume 3 · CME"
        title={activeSection ? breadcrumb.title : 'CME — Central de Material Esterilizado'}
        subtitle="Rastreamento de kits, esterilização e validade — RN-021 e RN-022."
      >
        {canManage && (
          <>
            <button className="btn btn-secondary" type="button" onClick={() => setShowKitModal(true)}>+ Novo kit</button>
            <button className="btn" type="button" onClick={() => setShowCycleModal(true)}>+ Novo ciclo</button>
          </>
        )}
      </PageHeader>

      <ModuleNav basePath="/cme" tabs={cmeTabs} contextId="surgery" />

      <p className="dashboard-meta" style={{ marginBottom: 8 }}>
        Kits esterilizados vinculados ao módulo cirúrgico —{' '}
        <Link to="/centro-cirurgico">Centro Cirúrgico</Link>
      </p>

      <div className="kpi-grid">
        <KpiCard label="Kits cadastrados" value={stats.totalKits} variant="primary" />
        <KpiCard label="Kits estéreis/disponíveis" value={stats.availableKits} variant="success" />
        <KpiCard label="Kits vencidos" value={stats.expiredKits} variant="danger" />
        <KpiCard label="Ciclos em andamento" value={stats.activeCycles} variant="warning" />
        <KpiCard label="Ciclos pendentes" value={stats.pendingCycles} variant="info" />
      </div>

      {activeSection !== 'ciclos' && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Kits instrumentais — {kits.length} kit(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Código</th><th>Nome</th><th>Status</th><th>Validade</th></tr></thead>
            <tbody>
              {kits.map((k) => (
                <tr key={k.id}>
                  <td>{k.code}</td>
                  <td>{k.name}</td>
                  <td><span className="badge">{instrumentKitStatusLabels[k.status] ?? k.status}</span></td>
                  <td>{k.sterilityExpiration ? formatBrDate(k.sterilityExpiration) : '—'}</td>
                </tr>
              ))}
              {kits.length === 0 && (
                <tr><td colSpan={4} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum kit</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      {activeSection === 'ciclos' && (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Ciclos de esterilização — {cycles.length} ciclo(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead><tr><th>Kit</th><th>Método</th><th>Status</th><th>Ações</th></tr></thead>
            <tbody>
              {cycles.map((c) => (
                <tr key={c.id}>
                  <td>{c.kitCode}</td>
                  <td>{sterilizationMethodLabels[c.method] ?? c.method}</td>
                  <td><span className="badge">{sterilizationCycleStatusLabels[c.status] ?? c.status}</span></td>
                  <td>
                    <div className="table-actions">
                      {canManage && c.status === 'InProgress' && (
                        <>
                          <button className="btn btn-sm" type="button" onClick={() => completeCycle(c.id)}>Concluir</button>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => rejectCycle(c.id)}>Rejeitar</button>
                        </>
                      )}
                      {canManage && c.status === 'Pending' && (
                        <>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => startCycle(c.id)}>Iniciar</button>
                          <button className="btn btn-secondary btn-sm" type="button" onClick={() => rejectCycle(c.id)}>Rejeitar</button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {cycles.length === 0 && (
                <tr><td colSpan={4} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum ciclo</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
      )}

      <Modal open={showKitModal} onClose={() => setShowKitModal(false)} title="Novo kit instrumental" width="md">
        <form onSubmit={handleCreateKit} className="form-grid">
          <div className="form-field">
            <label htmlFor="kitName">Nome *</label>
            <input id="kitName" value={kitForm.name} onChange={(e) => setKitForm({ ...kitForm, name: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="kitCode">Código *</label>
            <input id="kitCode" value={kitForm.code} onChange={(e) => setKitForm({ ...kitForm, code: e.target.value })} required />
          </div>
          <div className="form-field full">
            <label htmlFor="kitDesc">Descrição</label>
            <input id="kitDesc" value={kitForm.description} onChange={(e) => setKitForm({ ...kitForm, description: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowKitModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Cadastrar kit</button>
          </div>
        </form>
      </Modal>

      <Modal open={showCycleModal} onClose={() => setShowCycleModal(false)} title="Novo ciclo de esterilização" width="md">
        <form onSubmit={handleCreateCycle} className="form-grid">
          <div className="form-field">
            <label htmlFor="cycleKit">Kit *</label>
            <select id="cycleKit" value={cycleForm.instrumentKitId} onChange={(e) => setCycleForm({ ...cycleForm, instrumentKitId: e.target.value })} required>
              <option value="">Selecione...</option>
              {kits.filter((k) => k.status === 'Available').map((k) => (
                <option key={k.id} value={k.id}>{k.code} — {k.name}</option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="cycleMethod">Método</label>
            <select id="cycleMethod" value={cycleForm.method} onChange={(e) => setCycleForm({ ...cycleForm, method: e.target.value })}>
              {Object.entries(sterilizationMethodLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="cycleSterilizer">Esterilizador *</label>
            <input id="cycleSterilizer" value={cycleForm.sterilizerName} onChange={(e) => setCycleForm({ ...cycleForm, sterilizerName: e.target.value })} required />
          </div>
          <div className="form-field">
            <label htmlFor="cycleOperator">Operador</label>
            <input id="cycleOperator" value={cycleForm.operatorName} onChange={(e) => setCycleForm({ ...cycleForm, operatorName: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowCycleModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Abrir ciclo</button>
          </div>
        </form>
      </Modal>
    </>
  );
}

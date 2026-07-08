import { useEffect, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { api, type AuditLogDto, type BiDashboardDto } from '../../api/client';
import { KpiCard } from '../../components/KpiCard';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { qualityTabs } from '../../navigation/moduleSections';
import { useModuleSection } from '../../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { formatBrDateTime } from '../../utils/dateUtils';

type NcRecord = { id: string; title: string; severity: string; status: string; openedAt: string };

export function QualityHubPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/qualidade');
  const activeSection = section || 'nao-conformidades';

  const [bi, setBi] = useState<BiDashboardDto | null>(null);
  const [auditLogs, setAuditLogs] = useState<AuditLogDto[]>([]);
  const [ncList, setNcList] = useState<NcRecord[]>([]);
  const [ncForm, setNcForm] = useState({ title: '', severity: 'Média', description: '' });

  useEffect(() => {
    api.getBiDashboard().then(setBi).catch(console.error);
    api.getAuditLogs(30).then(setAuditLogs).catch(console.error);
  }, []);

  function handleNcSubmit(e: FormEvent) {
    e.preventDefault();
    if (!ncForm.title.trim()) return;
    setNcList((prev) => [{
      id: crypto.randomUUID(),
      title: ncForm.title,
      severity: ncForm.severity,
      status: 'Aberta',
      openedAt: new Date().toISOString(),
    }, ...prev]);
    setNcForm({ title: '', severity: 'Média', description: '' });
  }

  return (
    <>
      <PageHeader
        eyebrow="Qualidade"
        title={breadcrumb.title}
        subtitle="Gestão da qualidade assistencial, acreditação e indicadores."
      />

      <ModuleNav basePath="/qualidade" tabs={qualityTabs} contextId="quality" />

      <div className="kpi-grid" style={{ marginTop: 16 }}>
        <KpiCard label="Ocupação" value={bi ? `${bi.bedOccupancyRate.toFixed(0)}%` : '—'} variant="primary" />
        <KpiCard label="NC abertas" value={ncList.filter((n) => n.status === 'Aberta').length} />
        <KpiCard label="Auditorias (log)" value={auditLogs.length} />
      </div>

      {activeSection === 'nao-conformidades' && (
        <>
          <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={handleNcSubmit}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Registrar Não Conformidade</h3>
            <div className="form-field"><label>Título</label><input value={ncForm.title} onChange={(e) => setNcForm({ ...ncForm, title: e.target.value })} required /></div>
            <div className="form-field"><label>Gravidade</label>
              <select value={ncForm.severity} onChange={(e) => setNcForm({ ...ncForm, severity: e.target.value })}>
                <option>Baixa</option><option>Média</option><option>Alta</option><option>Crítica</option>
              </select>
            </div>
            <div className="form-field full"><label>Descrição</label><textarea rows={3} value={ncForm.description} onChange={(e) => setNcForm({ ...ncForm, description: e.target.value })} /></div>
            <div className="form-actions"><button className="btn" type="submit">Registrar NC</button></div>
          </form>
          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Não conformidades</div>
            <table className="data-table">
              <thead><tr><th>Data</th><th>Título</th><th>Gravidade</th><th>Status</th></tr></thead>
              <tbody>
                {ncList.map((n) => (
                  <tr key={n.id}><td>{formatBrDateTime(n.openedAt)}</td><td>{n.title}</td><td>{n.severity}</td><td>{n.status}</td></tr>
                ))}
                {ncList.length === 0 && <tr><td colSpan={4} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Nenhuma NC registrada.</td></tr>}
              </tbody>
            </table>
          </div>
        </>
      )}

      {activeSection === 'protocolos' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Protocolos institucionais</h3>
          <ul>
            <li>Protocolo de sepse — atualizado 2025</li>
            <li>Protocolo de queda — escala Morse</li>
            <li>Protocolo de AVC — janela terapêutica</li>
            <li>Bundle de CVC — CCIH</li>
          </ul>
          <Link to="/ccih" className="btn btn-secondary">CCIH</Link>
        </div>
      )}

      {activeSection === 'indicadores' && bi && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Indicadores assistenciais</h3>
          <ul className="bi-progress-list">
            {bi.labOrdersByStatus.map((i) => (<li key={i.label}>{i.label}: {i.count}</li>))}
          </ul>
          <Link to="/bi" className="btn btn-secondary" style={{ marginTop: 12 }}>Painel BI completo</Link>
        </div>
      )}

      {(activeSection === 'ona' || activeSection === 'jci') && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Certificação {activeSection.toUpperCase()}</h3>
          <p>Checklist de acreditação: segurança do paciente, identificação, medicamentos, prevenção de infecção, comunicação e gestão de riscos.</p>
          <Link to="/auditoria" className="btn btn-secondary">Log de auditoria</Link>
        </div>
      )}
    </>
  );
}

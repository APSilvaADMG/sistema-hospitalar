import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  bedStatusLabel,
  isBedOccupied,
  isHospitalizationActive,
  hospitalizationStatusLabel,
  type BedDto,
  type HospitalizationDto,
  type WardDto,
} from '../../api/client';
import { KpiCard } from '../../components/KpiCard';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { regulationTabs } from '../../navigation/moduleSections';
import { useModuleSection } from '../../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';

export function RegulationHubPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/regulacao');
  const activeSection = section || 'sisreg';

  const [wards, setWards] = useState<WardDto[]>([]);
  const [beds, setBeds] = useState<BedDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);

  useEffect(() => {
    Promise.all([
      api.getWards(),
      api.getBeds(),
      api.getHospitalizations(),
    ]).then(([w, b, h]) => {
      setWards(w);
      setBeds(b);
      setHospitalizations(h);
    }).catch(console.error);
  }, []);

  const freeBeds = beds.filter((b) => !isBedOccupied(b.status));
  const occupiedBeds = beds.filter((b) => isBedOccupied(b.status));

  return (
    <>
      <PageHeader
        eyebrow="Regulação"
        title={breadcrumb.title}
        subtitle="Regulação de vagas, SISREG e autorizações do gestor."
      />

      <ModuleNav basePath="/regulacao" tabs={regulationTabs} contextId="regulatory" />

      <div className="kpi-grid" style={{ marginTop: 16 }}>
        <KpiCard label="Leitos livres" value={freeBeds.length} variant="primary" />
        <KpiCard label="Leitos ocupados" value={occupiedBeds.length} />
        <KpiCard label="Internações" value={hospitalizations.length} />
        <KpiCard label="Enfermarias" value={wards.length} />
      </div>

      {activeSection === 'sisreg' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>SISREG — Regulação ambulatorial e hospitalar</h3>
          <p className="form-hint">Integração com fila SISREG para encaminhamentos e reserva de leitos.</p>
          <Link to="/integracoes/sisreg" className="btn">Configurar integração SISREG</Link>
        </div>
      )}

      {activeSection === 'leitos' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Central de leitos</div>
          <table className="data-table">
            <thead><tr><th>Leito</th><th>Enfermaria</th><th>Status</th><th>Paciente</th></tr></thead>
            <tbody>
              {beds.map((b) => (
                <tr key={b.id}>
                  <td>{b.bedNumber}</td>
                  <td>{b.wardName ?? '—'}</td>
                  <td>{bedStatusLabel(b.status)}</td>
                  <td>{b.occupantPatientName ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {activeSection === 'autorizacoes' && (
        <div className="card" style={{ marginTop: 16 }}>
          <h3>Autorizações regulatórias</h3>
          <p>Solicitações de vaga e autorização junto ao gestor municipal/estadual.</p>
          <Link to="/faturamento-tiss/autorizacoes" className="btn btn-secondary">Autorizações convênio</Link>
        </div>
      )}

      {activeSection === 'transferencias' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Transferências reguladas</div>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>Origem</th><th>Status</th></tr></thead>
            <tbody>
              {hospitalizations.filter((h) => isHospitalizationActive(h.status)).map((h) => (
                <tr key={h.id}>
                  <td>{h.patientName}</td>
                  <td>{h.bedNumber ?? '—'}</td>
                  <td>{hospitalizationStatusLabel(h.status)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Link to="/internacao/transferencias" className="btn btn-secondary" style={{ marginTop: 12 }}>Movimentação de leitos</Link>
        </div>
      )}
    </>
  );
}

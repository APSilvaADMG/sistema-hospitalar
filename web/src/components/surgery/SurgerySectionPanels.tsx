import { Link } from 'react-router-dom';
import {
  surgeryStatusLabels,
  type SurgeryDto,
} from '../../api/client';
import { formatBrDate, formatBrTime } from '../../utils/dateUtils';

type SurgeryPanelsProps = {
  section: string;
  surgeries: SurgeryDto[];
  onUpdateChecklist: (id: string, patch: Partial<{
    consentConfirmed: boolean;
    omsSignInCompleted: boolean;
    omsTimeOutCompleted: boolean;
    omsSignOutCompleted: boolean;
  }>) => void;
  onStatusChange: (id: string, status: number) => void;
};

function OmsChecklist({
  surgery,
  onUpdate,
}: {
  surgery: SurgeryDto;
  onUpdate: SurgeryPanelsProps['onUpdateChecklist'];
}) {
  const items = [
    { key: 'consentConfirmed' as const, label: 'Consentimento assinado (RN-020)' },
    { key: 'omsSignInCompleted' as const, label: 'Sign In — identificação' },
    { key: 'omsTimeOutCompleted' as const, label: 'Time Out — equipe e procedimento' },
    { key: 'omsSignOutCompleted' as const, label: 'Sign Out — contagem final' },
  ];

  return (
    <div className="oms-checklist">
      {items.map((item) => (
        <label key={item.key} className="oms-checklist-item">
          <input
            type="checkbox"
            checked={surgery[item.key]}
            onChange={(e) => onUpdate(surgery.id, { [item.key]: e.target.checked })}
          />
          <span>{item.label}</span>
        </label>
      ))}
    </div>
  );
}

export function SurgerySectionPanels({
  section,
  surgeries,
  onUpdateChecklist,
  onStatusChange,
}: SurgeryPanelsProps) {
  if (section === 'pre-operatorio') {
    const scheduled = surgeries.filter((s) => s.status === 1);
    return (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Pré-operatório — {scheduled.length} cirurgia(s)</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Horário</th><th>Paciente</th><th>Procedimento</th><th>Checklist</th></tr>
            </thead>
            <tbody>
              {scheduled.map((s) => (
                <tr key={s.id}>
                  <td>{formatBrTime(s.scheduledAt)}</td>
                  <td>{s.patientName}</td>
                  <td>{s.procedureName}</td>
                  <td><OmsChecklist surgery={s} onUpdate={onUpdateChecklist} /></td>
                </tr>
              ))}
              {scheduled.length === 0 && (
                <tr><td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma cirurgia agendada.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    );
  }

  if (section === 'sala') {
    const active = surgeries.filter((s) => s.status === 1 || s.status === 2);
    return (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Sala cirúrgica — checklist OMS (RN-019)</div>
        <div className="card-panel-body">
          {active.map((s) => (
            <div key={s.id} className="surgery-room-card">
              <div className="surgery-room-header">
                <strong>{s.patientName}</strong>
                <span className="badge">{surgeryStatusLabels[s.status]}</span>
              </div>
              <p>{s.procedureName} · {s.operatingRoomName} · {formatBrTime(s.scheduledAt)}</p>
              <OmsChecklist surgery={s} onUpdate={onUpdateChecklist} />
              {s.status === 1 && s.consentConfirmed && s.omsSignInCompleted && s.omsTimeOutCompleted && (
                <button type="button" className="btn btn-sm" style={{ marginTop: 12 }} onClick={() => onStatusChange(s.id, 2)}>
                  Iniciar cirurgia
                </button>
              )}
              {s.status === 2 && s.omsSignOutCompleted && (
                <button type="button" className="btn btn-secondary btn-sm" style={{ marginTop: 12 }} onClick={() => onStatusChange(s.id, 3)}>
                  Concluir cirurgia
                </button>
              )}
            </div>
          ))}
          {active.length === 0 && <p style={{ color: 'var(--muted)', margin: 0 }}>Nenhuma cirurgia na sala no momento.</p>}
        </div>
      </div>
    );
  }

  if (section === 'rpa') {
    const completed = surgeries.filter((s) => s.status === 3);
    return (
      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Recuperação pós-anestésica — {completed.length} hoje</div>
        <table className="data-table">
          <thead>
            <tr><th>Paciente</th><th>Procedimento</th><th>Horário</th><th>Status</th></tr>
          </thead>
          <tbody>
            {completed.map((s) => (
              <tr key={s.id}>
                <td>{s.patientName}</td>
                <td>{s.procedureName}</td>
                <td>{formatBrDate(s.scheduledAt)} {formatBrTime(s.scheduledAt)}</td>
                <td><span className="badge badge-success">Em recuperação</span></td>
              </tr>
            ))}
            {completed.length === 0 && (
              <tr><td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>Nenhuma cirurgia concluída hoje.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    );
  }

  if (section === 'relatorios') {
    const byStatus = Object.entries(surgeryStatusLabels).map(([k, label]) => ({
      label,
      count: surgeries.filter((s) => s.status === Number(k)).length,
    }));
    return (
      <div className="grid-2" style={{ marginTop: 24 }}>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Produção cirúrgica do dia</div>
          <ul className="bi-list">
            {byStatus.map((s) => (
              <li key={s.label}><span>{s.label}</span><strong>{s.count}</strong></li>
            ))}
          </ul>
        </div>
        <div className="card-panel appt-panel">
          <div className="card-panel-header">Relatórios corporativos</div>
          <div className="card-panel-body" style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            <Link to="/relatorios" className="btn btn-secondary">Central de Relatórios</Link>
            <Link to="/bi" className="btn btn-secondary">BI — Indicadores</Link>
            <Link to="/cme" className="btn btn-secondary">CME — Esterilização</Link>
          </div>
        </div>
      </div>
    );
  }

  return null;
}

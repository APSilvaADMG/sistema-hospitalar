import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  type AppointmentDto,
  type ConsultingRoomDto,
  type ImagingStudyDto,
  type LabOrderDto,
} from '../../api/client';
import { KpiCard } from '../../components/KpiCard';
import { ModulePageChrome } from '../../components/ModulePageChrome';
import { agendaTabs } from '../../navigation/moduleSections';
import { useModuleSection } from '../../navigation/useModuleSection';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { AppointmentDayTimeline } from '../../components/AppointmentDayTimeline';

type AgendaHubPageProps = {
  embedded?: boolean;
  sectionBasePath?: string;
};

export function AgendaHubPage({ embedded = false, sectionBasePath }: AgendaHubPageProps = {}) {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const navBasePath = sectionBasePath ?? '/agenda';
  const { section } = useModuleSection(navBasePath);
  const activeSection = section || 'medica';

  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [rooms, setRooms] = useState<ConsultingRoomDto[]>([]);
  const [labOrders, setLabOrders] = useState<LabOrderDto[]>([]);
  const [imaging, setImaging] = useState<ImagingStudyDto[]>([]);

  useEffect(() => {
    api.getAppointments().then(setAppointments).catch(console.error);
    api.getConsultingRooms().then(setRooms).catch(console.error);
    api.getLabOrders().then(setLabOrders).catch(console.error);
    api.getImagingStudies().then(setImaging).catch(console.error);
  }, []);

  const today = new Date().toISOString().slice(0, 10);
  const todayAppts = appointments.filter((a) => a.scheduledAt.startsWith(today));

  return (
    <ModulePageChrome
      embedded={embedded}
      eyebrow="Agenda"
      title={breadcrumb.title}
      subtitle="Agendas médica, de salas, equipamentos e exames."
      basePath={navBasePath}
      tabs={agendaTabs}
      contextId="reception"
    >
      <div className="kpi-grid" style={{ marginTop: 16 }}>
        <KpiCard label="Consultas hoje" value={todayAppts.length} variant="primary" />
        <KpiCard label="Salas" value={rooms.length} />
        <KpiCard label="Exames lab." value={labOrders.length} />
        <KpiCard label="Imagem" value={imaging.length} />
      </div>

      {activeSection === 'medica' && (
        <>
          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Agenda médica — hoje</div>
            <div className="card-panel-body" style={{ padding: 0 }}>
              <AppointmentDayTimeline
                appointments={todayAppts}
                date={today}
                readOnly
              />
            </div>
          </div>
          <Link to="/recepcao/agendamentos" className="btn btn-secondary" style={{ marginTop: 12 }}>Agenda completa</Link>
        </>
      )}

      {activeSection === 'equipamentos' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Salas e equipamentos</div>
          <table className="data-table">
            <thead><tr><th>Sala</th><th>Local</th><th>Status</th></tr></thead>
            <tbody>
              {rooms.map((r) => (
                <tr key={r.id}><td>{r.name}</td><td>{[r.building, r.floor].filter(Boolean).join(' · ') || '—'}</td><td>{r.status}</td></tr>
              ))}
            </tbody>
          </table>
          <Link to="/ambulatorio/consultorios" className="btn btn-secondary" style={{ marginTop: 12 }}>Consultórios</Link>
        </div>
      )}

      {activeSection === 'exames' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Agenda de exames</div>
          <h4 style={{ padding: '12px 16px 0', margin: 0 }}>Laboratório</h4>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>Exame</th><th>Status</th></tr></thead>
            <tbody>
              {labOrders.slice(0, 10).map((o) => (
                <tr key={o.id}>
                  <td>{o.patientName}</td>
                  <td>{o.items?.map((i) => i.examName).join(', ') || '—'}</td>
                  <td>{o.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <h4 style={{ padding: '12px 16px 0', margin: 0 }}>Imagem</h4>
          <table className="data-table">
            <thead><tr><th>Paciente</th><th>Modalidade</th><th>Status</th></tr></thead>
            <tbody>
              {imaging.slice(0, 10).map((s) => (
                <tr key={s.id}><td>{s.patientName}</td><td>{s.modality}</td><td>{s.status}</td></tr>
              ))}
            </tbody>
          </table>
          <div style={{ padding: 16, display: 'flex', gap: 8 }}>
            <Link to="/laboratorio" className="btn btn-secondary">Laboratório</Link>
            <Link to="/imagem" className="btn btn-secondary">Imagem</Link>
          </div>
        </div>
      )}
    </ModulePageChrome>
  );
}

import { useEffect, useMemo, useState } from 'react';
import {
  api,
  formatAppointmentStatus,
  isAppointmentStatus,
  type AppointmentDto,
  type PatientDto,
} from '../../api/client';
import { WorkspaceHubOverview } from '../../components/hubs/WorkspaceHubOverview';
import { ModuleNav } from '../../components/ModuleNav';
import { PatientWorkspaceShell } from '../../components/patient-workspace/PatientWorkspaceShell';
import { ambulatoryHubTabs } from '../../navigation/patientWorkspaceConfig';
import { useModuleSection } from '../../navigation/useModuleSection';
import { AppointmentsPage } from '../AppointmentsPage';
import { ConsultingRoomsPage } from '../ConsultingRoomsPage';
import { useLocation } from 'react-router-dom';
import { useAppearance } from '../../theme/AppearanceProvider';
import { isFeegowBrand } from '../../theme/appearanceConfig';
import { isFeegowDailyAgendaRoute } from '../../utils/feegowRoutes';
import { AgendaHubPage } from './AgendaHubPage';

function formatTime(iso: string) {
  try {
    return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  } catch {
    return '—';
  }
}

export function AmbulatoryWorkspacePage() {
  const { pathname } = useLocation();
  const { section } = useModuleSection('/ambulatorio');
  const activeSection = section || '';
  const { appearance } = useAppearance();
  const feegowAgendaOnly = isFeegowBrand(appearance.brand) && isFeegowDailyAgendaRoute(pathname);

  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);

  useEffect(() => {
    const today = new Date().toISOString().slice(0, 10);
    Promise.all([
      api.getPatients('', 1).then((r) => r.items),
      api.getAppointments(today),
    ])
      .then(([p, a]) => {
        setPatients(p);
        setAppointments(a);
      })
      .catch(console.error);
  }, []);

  const stats = useMemo(() => ({
    consultas: appointments.length,
    emAtendimento: appointments.filter((a) => isAppointmentStatus(a.status, 2, 3)).length,
    concluidas: appointments.filter((a) => isAppointmentStatus(a.status, 4)).length,
  }), [appointments]);

  if (feegowAgendaOnly) {
    return <AppointmentsPage embedded sectionBasePath="/recepcao/agendamentos" />;
  }

  return (
    <>
      <ModuleNav basePath="/ambulatorio" tabs={ambulatoryHubTabs} contextId="reception" />

      <PatientWorkspaceShell moduleId="ambulatory" patients={patients}>
        {activeSection === '' && (
          <>
            <WorkspaceHubOverview
              title="Resumo do dia"
              hint="Central Ambulatorial — visão operacional"
              stats={[
                { label: 'Consultas hoje', value: stats.consultas },
                { label: 'Em atendimento', value: stats.emAtendimento, tone: 'warning' },
                { label: 'Concluídas', value: stats.concluidas, tone: 'success' },
              ]}
              footerHint="Selecione o paciente na tabela para ver consultas, evoluções, prescrições e exames na mesma tela."
            />

            <div className="tab-pane box active">
              <div className="bayanno-panel-head">
                <span className="title">
                  <i className="icon-calendar" aria-hidden />
                  {' '}
                  Consultas de hoje
                </span>
              </div>
              <div className="table-responsive-wrap">
                <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">
                  <thead>
                    <tr>
                      <th><div>#</div></th>
                      <th><div>Paciente</div></th>
                      <th><div>Horário</div></th>
                      <th><div>Profissional</div></th>
                      <th><div>Status</div></th>
                    </tr>
                  </thead>
                  <tbody>
                    {appointments.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="dataTables_empty center">
                          Nenhuma consulta para hoje.
                        </td>
                      </tr>
                    ) : (
                      appointments.map((appt, index) => (
                        <tr key={appt.id} className={index % 2 === 1 ? 'even' : undefined}>
                          <td>{index + 1}</td>
                          <td><strong>{appt.patientName}</strong></td>
                          <td>{formatTime(appt.scheduledAt)}</td>
                          <td>{appt.professionalName ?? '—'}</td>
                          <td>{formatAppointmentStatus(appt.status)}</td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </>
        )}

        {activeSection === 'agenda' && (
          <AgendaHubPage embedded sectionBasePath="/ambulatorio/agenda" />
        )}

        {activeSection === 'consultorios' && (
          <ConsultingRoomsPage embedded sectionBasePath="/ambulatorio/consultorios" />
        )}

        {activeSection === 'atendimentos' && (
          <AppointmentsPage embedded sectionBasePath="/ambulatorio/atendimentos" />
        )}
      </PatientWorkspaceShell>
    </>
  );
}

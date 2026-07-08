import { useEffect, useMemo, useState } from 'react';

import { Link, useLocation } from 'react-router-dom';

import {

  api,

  formatAppointmentStatus,

  isAppointmentStatus,

  type AppointmentDto,

  type PatientDto,

} from '../../api/client';

import { ModuleNav } from '../../components/ModuleNav';

import { PatientWorkspaceShell } from '../../components/patient-workspace/PatientWorkspaceShell';

import { receptionHubTabs } from '../../navigation/patientWorkspaceConfig';

import { useModuleSection } from '../../navigation/useModuleSection';

import { AppointmentsPage } from '../AppointmentsPage';
import { useAppearance } from '../../theme/AppearanceProvider';
import { isFeegowBrand } from '../../theme/appearanceConfig';
import { isFeegowDailyAgendaRoute, isFeegowPatientRoute, isFeegowVaccinationRoute } from '../../utils/feegowRoutes';

import { HealthPlansPage } from '../HealthPlansPage';

import { PatientsPage } from '../PatientsPage';
import { FeegowPatientWorkspacePage } from '../FeegowPatientWorkspacePage';
import { FeegowVaccinationWorkspacePage } from '../FeegowVaccinationWorkspacePage';
import { BirthRegistrationPage } from '../BirthRegistrationPage';



function formatTime(iso: string) {

  try {

    return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });

  } catch {

    return '—';

  }

}



export function ReceptionWorkspacePage() {

  const { pathname } = useLocation();
  const { section } = useModuleSection('/recepcao');

  const activeSection = section || '';
  const { appearance } = useAppearance();
  const feegowAgendaOnly = isFeegowBrand(appearance.brand) && isFeegowDailyAgendaRoute(pathname);
  const feegowPatientScreen = isFeegowBrand(appearance.brand) && isFeegowPatientRoute(pathname);
  const feegowVaccinationScreen = isFeegowBrand(appearance.brand) && isFeegowVaccinationRoute(pathname);
  const onAgendamentosSection = activeSection === 'agendamentos'
    || activeSection.startsWith('agendamentos/')
    || ['semanal', 'multipla', 'check-in', 'confirmar', 'equipamentos', 'mapa'].includes(activeSection);

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



  const todayStats = useMemo(() => ({

    total: appointments.length,

    waiting: appointments.filter((a) => isAppointmentStatus(a.status, 1)).length,

    done: appointments.filter((a) => isAppointmentStatus(a.status, 4)).length,

  }), [appointments]);

  if (feegowAgendaOnly) {
    return <AppointmentsPage embedded sectionBasePath="/recepcao/agendamentos" />;
  }

  if (feegowVaccinationScreen) {
    return <FeegowVaccinationWorkspacePage />;
  }

  if (feegowPatientScreen) {
    return <FeegowPatientWorkspacePage />;
  }

  return (

    <>

      <ModuleNav basePath="/recepcao" tabs={receptionHubTabs} contextId="reception" />



      <PatientWorkspaceShell moduleId="reception" patients={patients}>

        {activeSection === '' && (

          <div className="tab-content">

            <div className="tab-pane box active">

              <div className="bayanno-panel-head">

                <span className="title">

                  <i className="icon-home" aria-hidden />

                  {' '}

                  Resumo do dia

                </span>

                <span className="bayanno-panel-hint">Central de Recepção — visão operacional</span>

              </div>

              <table className="bayanno-stats-table">

                <thead>

                  <tr>

                    <th>Agendamentos hoje</th>

                    <th>Aguardando</th>

                    <th>Atendidos</th>

                    <th>Pacientes cadastrados</th>

                  </tr>

                </thead>

                <tbody>

                  <tr>

                    <td>{todayStats.total}</td>

                    <td className="is-warning">{todayStats.waiting}</td>

                    <td className="is-success">{todayStats.done}</td>

                    <td>{patients.length}</td>

                  </tr>

                </tbody>

              </table>

              <p className="bayanno-inline-hint">

                Selecione um paciente na tabela abaixo para ver resumo, cadastro, agendamentos e convênio sem sair desta tela.

              </p>

            </div>



            <div className="tab-pane box active">

              <div className="bayanno-panel-head">

                <div className="bayanno-panel-actions">

                  <Link className="btn btn-secondary btn-sm" to="/recepcao/agendamentos">

                    Ver agenda completa

                  </Link>

                </div>

                <span className="title">

                  <i className="icon-calendar" aria-hidden />

                  {' '}

                  Agendamentos de hoje

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

                      <th><div>Opções</div></th>

                    </tr>

                  </thead>

                  <tbody>

                    {appointments.length === 0 ? (

                      <tr>

                        <td colSpan={6} className="dataTables_empty center">

                          Nenhum agendamento para hoje.

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

                          <td className="center">

                            <Link className="btn btn-green btn-sm" to="/recepcao/check-in">

                              Check-in

                            </Link>

                          </td>

                        </tr>

                      ))

                    )}

                  </tbody>

                </table>

              </div>

            </div>

          </div>

        )}



        {activeSection === 'pacientes' && (

          <PatientsPage embedded sectionBasePath="/recepcao/pacientes" />

        )}



        {onAgendamentosSection && (

          <AppointmentsPage embedded sectionBasePath="/recepcao/agendamentos" />

        )}



        {activeSection === 'check-in' && (

          <AppointmentsPage embedded sectionBasePath="/recepcao/agendamentos" forcedSection="check-in" />

        )}



        {activeSection === 'convenios' && (

          <HealthPlansPage embedded />

        )}

        {activeSection === 'registro-nascimento' && (
          <BirthRegistrationPage />
        )}

      </PatientWorkspaceShell>

    </>

  );

}


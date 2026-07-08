import { useEffect, useState } from 'react';

import {

  api,

  appointmentStatusLabel,

  type PatientDto,

  type PatientMedicalRecordDto,

  type PatientPortalDashboardDto,

} from '../api/client';

import { KpiCard } from '../components/KpiCard';
import { PatientConsentsPanel } from '../components/PatientConsentsPanel';
import { PageHeader } from '../components/PageHeader';

import { formatBrDate, formatBrDateTime, formatBrTime } from '../utils/dateUtils';

import { useAuth } from '../auth/AuthContext';



export function PatientPortalPage() {

  const { user, hasRole } = useAuth();

  const isPatient = hasRole('Patient');

  const isAdmin = hasRole('Admin');

  const canAccess = isPatient || isAdmin;



  const [patients, setPatients] = useState<PatientDto[]>([]);

  const [selectedPatientId, setSelectedPatientId] = useState('');

  const [dashboard, setDashboard] = useState<PatientPortalDashboardDto | null>(null);

  const [record, setRecord] = useState<PatientMedicalRecordDto | null>(null);

  const [error, setError] = useState('');

  const [success, setSuccess] = useState('');

  const [loading, setLoading] = useState(false);



  useEffect(() => {

    if (!isAdmin) return;

    api.getPatients(undefined, 1)

      .then((result) => setPatients(result.items))

      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar pacientes'));

  }, [isAdmin]);



  useEffect(() => {

    if (!canAccess) return;



    const patientId = isAdmin ? selectedPatientId : user?.patientId;

    if (!patientId) {

      setDashboard(null);

      setRecord(null);

      return;

    }



    setLoading(true);

    setError('');

    Promise.all([

      api.getPatientPortalDashboard(patientId),

      api.getPatientPortalMedicalRecord(patientId),

    ])

      .then(([d, r]) => {

        setDashboard(d);

        setRecord(r);

      })

      .catch((err) => {

        setDashboard(null);

        setRecord(null);

        setError(err instanceof Error ? err.message : 'Erro ao carregar portal');

      })

      .finally(() => setLoading(false));

  }, [canAccess, isAdmin, isPatient, selectedPatientId, user?.patientId]);



  if (!canAccess) {

    return (

      <div className="card-panel">

        <div className="card-panel-body">

          <h2 style={{ marginTop: 0 }}>Portal do Paciente</h2>

          <p>Acesso exclusivo para pacientes e administradores.</p>

        </div>

      </div>

    );

  }



  if (isAdmin && !selectedPatientId) {

    return (

      <>

        <PageHeader

          eyebrow="Atendimento"

          title="Portal do Paciente"

          subtitle="Visualize o portal como administrador — selecione um paciente para conferir consultas, resultados e prontuário."

        />

        <div className="card-panel appt-panel">

          <div className="card-panel-header">Selecionar paciente</div>

          <div className="card-panel-body">

            <div className="form-field" style={{ maxWidth: 420 }}>

              <label htmlFor="portalPatient">Paciente</label>

              <select

                id="portalPatient"

                value={selectedPatientId}

                onChange={(e) => setSelectedPatientId(e.target.value)}

              >

                <option value="">Selecione o paciente</option>

                {patients.map((patient) => (

                  <option key={patient.id} value={patient.id}>

                    {patient.fullName}

                    {patient.cpf ? ` · ${patient.cpf}` : ''}

                  </option>

                ))}

              </select>

            </div>

          </div>

        </div>

      </>

    );

  }



  if (error) return <div className="alert alert-error">{error}</div>;

  if (loading || !dashboard) return <div className="card">Carregando portal...</div>;



  const abnormalResults = dashboard.recentLabResults.filter((r) => r.isAbnormal).length;

  const greetingName = isAdmin ? dashboard.patientName.split(' ')[0] : user?.fullName.split(' ')[0];

  const portalPatientId = isAdmin ? selectedPatientId : user?.patientId ?? '';



  return (

    <>

      {success && <div className="alert alert-success" style={{ marginBottom: 16 }}>{success}</div>}

      <PageHeader

        eyebrow="Atendimento"

        title={isAdmin ? `Portal — ${dashboard.patientName}` : `Olá, ${greetingName}`}

        subtitle={`PEP: ${dashboard.recordNumber ?? '—'} · Acompanhe consultas, resultados e prontuário.`}

      >

        {isAdmin && (

          <button

            type="button"

            className="btn btn-secondary"

            onClick={() => {

              setSelectedPatientId('');

              setDashboard(null);

              setRecord(null);

            }}

          >

            Trocar paciente

          </button>

        )}

      </PageHeader>



      <div className="kpi-grid">

        <KpiCard label="Próximas consultas" value={dashboard.upcomingAppointments.length} variant="primary" />

        <KpiCard label="Resultados recentes" value={dashboard.recentLabResults.length} variant="info" />

        <KpiCard label="Resultados alterados" value={abnormalResults} variant={abnormalResults > 0 ? 'danger' : 'success'} />

        <KpiCard label="Entradas no prontuário" value={record?.entries.length ?? 0} variant="neutral" />

      </div>



      <div className="card-panel appt-panel" style={{ marginBottom: 20 }}>

        <div className="card-panel-header">Próximos agendamentos</div>

        <div className="card-panel-body appt-panel-body">

          {dashboard.upcomingAppointments.length === 0 ? (

            <div className="appt-empty">

              <div className="appt-empty-icon">📅</div>

              <h3>Nenhum agendamento futuro</h3>

              <p>Quando houver consultas marcadas, elas aparecerão aqui.</p>

            </div>

          ) : (

            <div className="emergency-queue">

              {dashboard.upcomingAppointments.map((a) => (

                <article key={a.id} className="appt-card">

                  <div className="appt-card-time">

                    <span>{formatBrTime(a.scheduledAt)}</span>

                    <span className="appt-card-duration">

                      {formatBrDate(a.scheduledAt)}

                    </span>

                  </div>

                  <div className="appt-card-main">

                    <strong>{a.professionalName}</strong>

                    <div className="appt-card-meta">

                      <span>{a.specialtyName}</span>

                      <span className="appt-meta-dot">•</span>

                      <span className="appt-status status-scheduled">

                        {appointmentStatusLabel(a.status)}

                      </span>

                    </div>

                  </div>

                </article>

              ))}

            </div>

          )}

        </div>

      </div>



      <div className="card-panel appt-panel" style={{ marginBottom: 20 }}>

        <div className="card-panel-header">Resultados recentes de exames</div>

        <div className="card-panel-body" style={{ padding: 0 }}>

          {dashboard.recentLabResults.length === 0 ? (

            <div className="appt-empty">

              <p>Nenhum resultado disponível.</p>

            </div>

          ) : (

            <table className="data-table">

              <thead><tr><th>Exame</th><th>Resultado</th><th>Referência</th><th>Data</th></tr></thead>

              <tbody>

                {dashboard.recentLabResults.map((r, i) => (

                  <tr key={i} className={r.isAbnormal ? 'row-alert' : ''}>

                    <td><strong>{r.examName}</strong></td>

                    <td>{r.value}</td>

                    <td>{r.referenceRange ?? '—'}</td>

                    <td>{r.releasedAt ? formatBrDate(r.releasedAt) : '—'}</td>

                  </tr>

                ))}

              </tbody>

            </table>

          )}

        </div>

      </div>



      <div className="card-panel appt-panel" style={{ marginBottom: 20 }}>

        <div className="card-panel-header">Termos e consentimentos</div>

        <div className="card-panel-body">

          <p className="form-hint" style={{ marginTop: 0 }}>

            Leia cada termo até o final, marque a ciência e assine digitalmente para autorizar o tratamento de dados e demais finalidades.

          </p>

          <PatientConsentsPanel

            patientId={portalPatientId || undefined}

            portalMode

            onSuccess={setSuccess}

            onError={setError}

          />

        </div>

      </div>



      {record && (

        <div className="card-panel appt-panel">

          <div className="card-panel-header">Prontuário — {record.recordNumber}</div>

          <div className="card-panel-body">

            {record.entries.length === 0 ? (

              <p style={{ color: 'var(--muted)', margin: 0 }}>Nenhuma entrada no prontuário.</p>

            ) : (

              <div className="timeline">

                {record.entries.map((e, i) => (

                  <div key={i} className="timeline-item">

                    <div className="timeline-meta">

                      {formatBrDateTime(e.createdAt)} · {e.entryType}

                      {e.professionalName && ` · ${e.professionalName}`}

                    </div>

                    <p style={{ margin: 0 }}>{e.content}</p>

                  </div>

                ))}

              </div>

            )}

          </div>

        </div>

      )}

    </>

  );

}



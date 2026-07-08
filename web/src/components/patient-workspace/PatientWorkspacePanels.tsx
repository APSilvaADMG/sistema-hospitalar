import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  appointmentStatusLabel,
  isAppointmentStatus,
  entryTypeToNumber,
  formatEntryTypeLabel,
  hospitalizationStatusLabel,
  imagingModalityLabels,
  imagingStatusLabels,
  labOrderStatusLabels,
  triageUrgencyLabels,
  type AppointmentDto,
  type EmergencyVisitDto,
  type HospitalizationDto,
  type ImagingStudyDto,
  type LabOrderDto,
  type MedicalRecordEntryDto,
  type PatientDetailDto,
} from '../../api/client';
import { KpiCard } from '../KpiCard';
import { formatBrDateTime } from '../../utils/dateUtils';
import { loadPatientAppointments } from '../../utils/pepUtils';
import type { PatientWorkspaceModuleId } from '../../navigation/patientWorkspaceConfig';

type Props = {
  patient: PatientDetailDto;
  moduleId: PatientWorkspaceModuleId;
  view: string;
};

export function PatientWorkspacePanels({ patient, moduleId, view }: Props) {
  const [entries, setEntries] = useState<MedicalRecordEntryDto[]>([]);
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [labOrders, setLabOrders] = useState<LabOrderDto[]>([]);
  const [imaging, setImaging] = useState<ImagingStudyDto[]>([]);
  const [hospitalizations, setHospitalizations] = useState<HospitalizationDto[]>([]);
  const [emergencyVisits, setEmergencyVisits] = useState<EmergencyVisitDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    setLoading(true);
    Promise.all([
      api.getMedicalRecord(patient.id).then((r) => setEntries(r.entries)),
      loadPatientAppointments(patient.id, 60).catch(() => [] as AppointmentDto[]),
      api.getLabOrders().then((all) => all.filter((o) => o.patientId === patient.id)),
      api.getImagingStudies().then((all) => all.filter((s) => s.patientId === patient.id)),
      api.getHospitalizations(patient.id, 'all').catch(() => [] as HospitalizationDto[]),
      api.getEmergencyVisits().then((all) => all.filter((v) => v.patientId === patient.id)),
    ])
      .then(([, appts, labs, imgs, hosps, visits]) => {
        setAppointments(appts);
        setLabOrders(labs);
        setImaging(imgs);
        setHospitalizations(hosps);
        setEmergencyVisits(visits);
      })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [patient.id]);

  const stats = useMemo(() => ({
    upcomingAppts: appointments.filter((a) => !isAppointmentStatus(a.status, 5, 6) && new Date(a.scheduledAt) >= new Date()).length,
    pendingLabs: labOrders.filter((o) => o.status === 1 || o.status === 2).length,
    pendingImaging: imaging.filter((s) => s.status === 1 || s.status === 2).length,
    activeHosp: hospitalizations.filter((h) => h.status === 1).length,
    openEr: emergencyVisits.filter((v) => v.status === 'Waiting' || v.status === 'InCare').length,
    lastEntry: entries[0],
  }), [appointments, labOrders, imaging, hospitalizations, emergencyVisits, entries]);

  const evolutionEntries = entries.filter((e) => [1, 2, 3].includes(entryTypeToNumber(e.entryType)));
  const prescriptions = entries.filter((e) => entryTypeToNumber(e.entryType) === 4);
  const diagnoses = entries.filter((e) => entryTypeToNumber(e.entryType) === 5);
  const attachments = entries.filter((e) => entryTypeToNumber(e.entryType) === 8);

  if (loading) {
    return <p className="form-hint">Carregando dados do paciente…</p>;
  }

  if (view === 'resumo') {
    return (
      <>
        <div className="patient-panel-grid">
          <KpiCard label="Próximas consultas" value={String(stats.upcomingAppts)} variant="info" />
          <KpiCard label="Exames pendentes" value={String(stats.pendingLabs + stats.pendingImaging)} variant="warning" />
          <KpiCard label="Internação ativa" value={stats.activeHosp > 0 ? 'Sim' : 'Não'} variant={stats.activeHosp > 0 ? 'danger' : 'success'} />
          {(moduleId === 'emergency' || stats.openEr > 0) && (
            <KpiCard label="PA em aberto" value={String(stats.openEr)} variant="danger" />
          )}
        </div>
        {stats.lastEntry && (
          <div className="patient-panel-section card-panel">
            <h4>Último registro clínico</h4>
            <p className="form-hint">{formatEntryTypeLabel(stats.lastEntry.entryType)} · {formatBrDateTime(stats.lastEntry.createdAt)}</p>
            <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{stats.lastEntry.content.slice(0, 400)}{stats.lastEntry.content.length > 400 ? '…' : ''}</p>
          </div>
        )}
        {patient.notes && (
          <div className="patient-panel-section card-panel">
            <h4>Observações cadastrais</h4>
            <p style={{ margin: 0 }}>{patient.notes}</p>
          </div>
        )}
      </>
    );
  }

  if (view === 'cadastro') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>Dados cadastrais</h4>
        <div className="form-grid" style={{ padding: 0 }}>
          <div><strong>E-mail</strong><div>{patient.email || '—'}</div></div>
          <div><strong>Telefone</strong><div>{patient.phone || patient.mobilePhone || '—'}</div></div>
          <div className="full"><strong>Endereço</strong><div>{[patient.addressStreet, patient.addressNumber, patient.addressNeighborhood, patient.addressCity].filter(Boolean).join(', ') || '—'}</div></div>
          <div><strong>Mãe</strong><div>{patient.motherName || '—'}</div></div>
          <div><strong>Contato emergência</strong><div>{patient.emergencyContactName ? `${patient.emergencyContactName} (${patient.emergencyContactPhone})` : '—'}</div></div>
        </div>
        <Link className="btn btn-secondary btn-sm" style={{ marginTop: 12 }} to={`/pacientes?paciente=${patient.id}&visao=cadastro`}>
          Editar cadastro completo
        </Link>
      </div>
    );
  }

  if (view === 'convenio') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>Convênios e carteirinhas</h4>
        {patient.insurances?.length ? (
          <table className="data-table">
            <thead><tr><th>Operadora</th><th>Carteira</th><th>Plano</th><th>Principal</th></tr></thead>
            <tbody>
              {patient.insurances.map((ins) => (
                <tr key={ins.id}>
                  <td>{ins.healthInsuranceName}</td>
                  <td>{ins.cardNumber}</td>
                  <td>{ins.planName ?? '—'}</td>
                  <td>{ins.isPrimary ? 'Sim' : '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : <p className="form-hint">Nenhum convênio cadastrado.</p>}
      </div>
    );
  }

  if (view === 'agendamentos' || view === 'consultas') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>{view === 'consultas' ? 'Consultas ambulatoriais' : 'Agendamentos'}</h4>
        {appointments.length === 0 ? <p className="form-hint">Nenhum agendamento.</p> : (
          <table className="data-table">
            <thead><tr><th>Data</th><th>Profissional</th><th>Status</th><th>Motivo</th></tr></thead>
            <tbody>
              {appointments.slice(0, 20).map((a) => (
                <tr key={a.id}>
                  <td>{formatBrDateTime(a.scheduledAt)}</td>
                  <td>{a.professionalName}</td>
                  <td>{appointmentStatusLabel(a.status)}</td>
                  <td>{a.reason ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    );
  }

  if (view === 'evolucoes' || view === 'sae') {
    const list = view === 'sae' ? entries.filter((e) => e.entryType === 2) : evolutionEntries;
    return (
      <EntriesList title={view === 'sae' ? 'SAE — registros de enfermagem' : 'Evoluções clínicas'} entries={list} />
    );
  }

  if (view === 'prescricoes') {
    return <EntriesList title="Prescrições" entries={prescriptions} />;
  }

  if (view === 'diagnosticos') {
    return <EntriesList title="Diagnósticos (CID)" entries={diagnoses} />;
  }

  if (view === 'anexos' || view === 'documentos') {
    const list = view === 'documentos' ? attachments : attachments;
    return <EntriesList title={view === 'documentos' ? 'Documentos' : 'Anexos'} entries={list} emptyHint="Nenhum documento anexado." />;
  }

  if (view === 'exames') {
    return (
      <>
        <div className="card-panel patient-panel-section">
          <h4>Laboratório</h4>
          {labOrders.length === 0 ? <p className="form-hint">Sem pedidos.</p> : (
            <table className="data-table">
              <thead><tr><th>Data</th><th>Status</th><th>Exames</th></tr></thead>
              <tbody>
                {labOrders.slice(0, 10).map((o) => (
                  <tr key={o.id}>
                    <td>{formatBrDateTime(o.createdAt)}</td>
                    <td>{labOrderStatusLabels[o.status]}</td>
                    <td>{o.items.map((i) => i.examName).join(', ')}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
        <div className="card-panel patient-panel-section">
          <h4>Imagem</h4>
          {imaging.length === 0 ? <p className="form-hint">Sem estudos.</p> : (
            <table className="data-table">
              <thead><tr><th>Data</th><th>Modalidade</th><th>Exame</th><th>Status</th></tr></thead>
              <tbody>
                {imaging.slice(0, 10).map((s) => (
                  <tr key={s.id}>
                    <td>{formatBrDateTime(s.scheduledAt)}</td>
                    <td>{imagingModalityLabels[s.modality]}</td>
                    <td>{s.studyDescription}</td>
                    <td>{imagingStatusLabels[s.status]}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </>
    );
  }

  if (view === 'internacao' || view === 'alta') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>Internações</h4>
        {hospitalizations.length === 0 ? <p className="form-hint">Sem internações registradas.</p> : (
          <table className="data-table">
            <thead><tr><th>Admissão</th><th>Leito</th><th>Status</th><th>Motivo</th></tr></thead>
            <tbody>
              {hospitalizations.map((h) => (
                <tr key={h.id}>
                  <td>{formatBrDateTime(h.admittedAt)}</td>
                  <td>{h.wardName} — {h.bedNumber}</td>
                  <td>{hospitalizationStatusLabel(h.status)}</td>
                  <td>{h.reason}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    );
  }

  if (view === 'triagem' || view === 'atendimento') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>Passagens pelo Pronto Atendimento</h4>
        {emergencyVisits.length === 0 ? <p className="form-hint">Sem visitas ao PA.</p> : (
          <table className="data-table">
            <thead><tr><th>Chegada</th><th>Queixa</th><th>Urgência</th><th>Status</th></tr></thead>
            <tbody>
              {emergencyVisits.map((v) => (
                <tr key={v.id}>
                  <td>{formatBrDateTime(v.arrivedAt)}</td>
                  <td>{v.chiefComplaint}</td>
                  <td>{triageUrgencyLabels[v.urgency] ?? v.urgency}</td>
                  <td>{v.status}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    );
  }

  if (view === 'cirurgias' || view === 'pre-op') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>Cirurgias</h4>
        <p className="form-hint">Consulte a agenda do centro cirúrgico para o dia da cirurgia. Registros pré-operatórios ficam no PEP.</p>
        <Link className="btn btn-secondary btn-sm" to={`/centro-cirurgico?paciente=${patient.id}`}>
          Abrir centro cirúrgico
        </Link>
      </div>
    );
  }

  if (view === 'vigilancia' || view === 'isolamento' || view === 'infeccoes') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>CCIH — {view}</h4>
        <p className="form-hint">Registros de vigilância epidemiológica e isolamento no módulo CCIH.</p>
        <Link className="btn btn-secondary btn-sm" to={`/ccih?paciente=${patient.id}&visao=${view}`}>
          Abrir CCIH
        </Link>
      </div>
    );
  }

  if (view === 'monitorizacao' || view === 'escalas' || view === 'medicamentos' || view === 'sinais-vitais' || view === 'curativos') {
    return (
      <div className="card-panel patient-panel-section">
        <h4>Enfermagem — {view}</h4>
        <EntriesList
          title="Registros recentes"
          entries={entries.filter((e) => e.entryType === 2).slice(0, 15)}
          emptyHint="Use as abas do módulo Enfermagem para registrar novos dados."
        />
        <Link className="btn btn-secondary btn-sm" style={{ marginTop: 8 }} to={`/enfermagem/sae/evolucao?paciente=${patient.id}`}>
          Registrar em Enfermagem
        </Link>
      </div>
    );
  }

  return (
    <div className="patient-workspace-empty">
      Selecione outra aba ou use o fluxo operacional do módulo abaixo.
    </div>
  );
}

function EntriesList({
  title,
  entries,
  emptyHint = 'Nenhum registro.',
}: {
  title: string;
  entries: MedicalRecordEntryDto[];
  emptyHint?: string;
}) {
  return (
    <div className="card-panel patient-panel-section">
      <h4>{title}</h4>
      {entries.length === 0 ? <p className="form-hint">{emptyHint}</p> : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {entries.slice(0, 25).map((e) => (
            <article key={e.id} className="card" style={{ padding: 12 }}>
              <div className="form-hint" style={{ marginBottom: 4 }}>
                {formatEntryTypeLabel(e.entryType)} · {formatBrDateTime(e.createdAt)}
                {e.professionalName && ` · ${e.professionalName}`}
              </div>
              <p style={{ margin: 0, whiteSpace: 'pre-wrap' }}>{e.content}</p>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}

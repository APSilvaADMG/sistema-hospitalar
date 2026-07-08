import { useEffect, useState, type ReactNode } from 'react';

import { Link } from 'react-router-dom';

import {

  api,

  entryTypeToNumber,

  financialCategoryLabel,

  formatAppointmentStatus,

  formatEntryTypeLabel,

  imagingModalityLabels,

  imagingStatusLabels,

  isFinancialReceivable,

  labOrderStatusLabels,

  type AppointmentDto,

  type FinancialAccountDto,

  type ImagingStudyDto,

  type LabOrderDto,

  type PatientDetailDto,

  type PatientTimelineEventDto,

  type PharmacyDispensingDto,

} from '../../../api/client';

import { usePatientDigitalRecord } from '../../../hooks/usePatientDigitalRecord';

import { formatBrDate, formatBrDateTime } from '../../../utils/dateUtils';

import { FeegowPatientEntriesPanel } from './FeegowPatientEntriesPanel';
import { FeegowClinicalAlertsPanel } from './FeegowClinicalAlertsPanel';
import { FinancialStatusBadge } from '../finance/FinancialStatusBadge';



type PatientProps = {

  patientId: string;

  patient: PatientDetailDto;

};



const CERTIFICATE_TEMPLATES = [

  'Atesto para os devidos fins que o(a) paciente necessita de afastamento de suas atividades por ___ dia(s), a contar desta data.',

  'Atesto que o(a) paciente encontra-se apto(a) para exercer suas atividades habituais.',

  'Atesto que o(a) paciente compareceu a esta unidade para consulta/atendimento no período das ___ às ___ horas.',

  'Declaro que o(a) paciente está em acompanhamento médico nesta instituição.',

];



const REFERRAL_TEMPLATES = [

  'Encaminho o(a) paciente para avaliação especializada em ___.',

  'Encaminho para serviço de referência conforme protocolo institucional.',

  'Encaminho para UBS / atenção primária para continuidade do cuidado.',

  'Encaminho para urgência/emergência devido a critérios clínicos identificados.',

];



function SubsectionTitle({ children }: { children: string }) {

  return <h4 className="feegow-patient-section-subtitle">{children}</h4>;

}



function DataTable({ headers, rows, empty }: { headers: string[]; rows: ReactNode[][]; empty: string }) {

  if (rows.length === 0) {

    return <p className="feegow-patient-section-empty">{empty}</p>;

  }

  return (

    <table className="feegow-patient-section-table">

      <thead>

        <tr>{headers.map((h) => <th key={h}>{h}</th>)}</tr>

      </thead>

      <tbody>

        {rows.map((cells, index) => (

          <tr key={index}>{cells.map((cell, i) => <td key={i}>{cell}</td>)}</tr>

        ))}

      </tbody>

    </table>

  );

}



export function FeegowPatientAiSummaryPanel({ patientId, patient }: PatientProps) {

  const { digital, entries, loading } = usePatientDigitalRecord(patientId);



  const highlights = [...entries]

    .sort((a, b) => b.createdAt.localeCompare(a.createdAt))

    .slice(0, 5);



  return (

    <>

      {loading ? (

        <p className="feegow-patient-section-empty">Carregando resumo…</p>

      ) : (

        <>

          <p className="feegow-patient-section-meta">

            Prontuário nº {digital?.record.recordNumber ?? '—'} · {entries.length} registro(s) clínico(s)

          </p>

          {digital?.activeHospitalization ? (

            <p className="feegow-patient-section-meta">

              Internação ativa: leito {digital.activeHospitalization.bedNumber} — {digital.activeHospitalization.wardName}

            </p>

          ) : null}

          <SubsectionTitle>Alertas clínicos (IA)</SubsectionTitle>
          <FeegowClinicalAlertsPanel patientId={patientId} />

          <SubsectionTitle>Últimos registros</SubsectionTitle>

          <DataTable

            headers={['Data', 'Tipo', 'Resumo']}

            empty="Nenhum registro clínico para resumir."

            rows={highlights.map((e) => [

              formatBrDateTime(e.createdAt),

              formatEntryTypeLabel(e.entryType),

              e.content?.slice(0, 100) || '—',

            ])}

          />

          {digital?.tissGuides && digital.tissGuides.length > 0 ? (

            <>

              <SubsectionTitle>Guias TISS recentes</SubsectionTitle>

              <DataTable

                headers={['Data', 'Guia', 'Status']}

                empty=""

                rows={digital.tissGuides.slice(0, 5).map((g) => [

                  formatBrDate(g.createdAt),

                  g.guideNumber,

                  String(g.status),

                ])}

              />

            </>

          ) : null}

        </>

      )}

      <p className="feegow-patient-section-pep-link">

        <Link to={`/ia?paciente=${patientId}`}>Gerar resumo com IA</Link>

        {' · '}

        <Link to={`/pacientes/${patientId}/prontuario/resumo`}>PEP completo — {patient.fullName}</Link>

      </p>

    </>

  );

}



export function FeegowPatientDiagnosticsPanel(props: PatientProps) {

  return (

    <FeegowPatientEntriesPanel

      {...props}

      config={{

        createButtonLabel: '+ NOVO DIAGNÓSTICO',

        emptyMessage: 'Nenhum diagnóstico (CID-10) registrado.',

        contentPlaceholder: 'Hipótese diagnóstica, evolução ou observações clínicas…',

        filter: (entry) => Boolean(entry.cid10Code?.trim()),

        defaultEntryType: 2,

        allowedEntryTypes: [1, 2],

        pepLink: `/pacientes/${props.patientId}/prontuario/diagnosticos`,

      }}

    />

  );

}



export function FeegowPatientReferralsPanel(props: PatientProps) {

  return (

    <FeegowPatientEntriesPanel

      {...props}

      config={{

        createButtonLabel: '+ NOVO ENCAMINHAMENTO',

        emptyMessage: 'Nenhum encaminhamento registrado.',

        contentPlaceholder: 'Descreva o encaminhamento, destino e motivo clínico…',

        filter: (entry) => {

          const text = entry.content?.toLowerCase() ?? '';

          return text.includes('encaminh') || entryTypeToNumber(entry.entryType) === 5;

        },

        fixedEntryType: 5,

        showTypeColumn: false,

        contentTemplates: REFERRAL_TEMPLATES,

        pepLink: `/pacientes/${props.patientId}/prontuario/encaminhamentos`,

        extraFooterLinks: [{ label: 'Abrir regulação / SISREG', to: '/regulacao/sisreg' }],

      }}

    />

  );

}



export function FeegowPatientTextsPanel(props: PatientProps) {

  return (

    <FeegowPatientEntriesPanel

      {...props}

      config={{

        createButtonLabel: '+ NOVO TEXTO / ATESTADO',

        emptyMessage: 'Nenhum texto ou atestado registrado.',

        contentPlaceholder: 'Redija o atestado, declaração ou orientação ao paciente…',

        filter: (entry) => {

          const text = entry.content?.toLowerCase() ?? '';

          return text.includes('atest') || text.includes('declar') || text.includes('orient');

        },

        fixedEntryType: 2,

        showTypeColumn: false,

        showCid10: false,

        contentTemplates: CERTIFICATE_TEMPLATES,

        pepLink: `/pacientes/${props.patientId}/prontuario/evolucao-medica`,

      }}

    />

  );

}



export function FeegowPatientReportsPanel({ patientId }: PatientProps) {

  const [labOrders, setLabOrders] = useState<LabOrderDto[]>([]);

  const [imaging, setImaging] = useState<ImagingStudyDto[]>([]);

  const [loading, setLoading] = useState(true);



  useEffect(() => {

    setLoading(true);

    Promise.all([api.getLabOrders(), api.getImagingStudies()])

      .then(([labs, imgs]) => {

        setLabOrders(labs.filter((o) => o.patientId === patientId));

        setImaging(imgs.filter((s) => s.patientId === patientId && s.reportContent));

      })

      .catch(() => {

        setLabOrders([]);

        setImaging([]);

      })

      .finally(() => setLoading(false));

  }, [patientId]);



  const labRows = labOrders.flatMap((order) =>

    order.items

      .filter((item) => item.result)

      .map((item) => [

        formatBrDateTime(order.createdAt),

        item.examName,

        item.result?.value ?? '—',

        item.result?.referenceRange ?? '—',

        item.result?.releasedAt ? formatBrDateTime(item.result.releasedAt) : '—',

      ]),

  );



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando laudos…</p> : null}

      <SubsectionTitle>Laudos laboratoriais</SubsectionTitle>

      <DataTable

        headers={['Data', 'Exame', 'Resultado', 'Referência', 'Liberado em']}

        empty="Nenhum resultado laboratorial liberado."

        rows={labRows}

      />

      <SubsectionTitle>Laudos de imagem</SubsectionTitle>

      <DataTable

        headers={['Data', 'Modalidade', 'Descrição', 'Laudo']}

        empty="Nenhum laudo de imagem disponível."

        rows={imaging.map((study) => [

          formatBrDateTime(study.reportedAt ?? study.completedAt ?? study.scheduledAt),

          imagingModalityLabels[study.modality] ?? String(study.modality),

          study.studyDescription,

          study.reportContent?.slice(0, 80) ?? '—',

        ])}

      />

      <p className="feegow-patient-section-pep-link">

        <Link to="/laboratorio">Laboratório</Link>

        {' · '}

        <Link to="/imagem">Imagem / PACS</Link>

      </p>

    </>

  );

}



export function FeegowPatientExamOrdersPanel({ patientId, patient }: PatientProps) {

  const [labOrders, setLabOrders] = useState<LabOrderDto[]>([]);

  const [imaging, setImaging] = useState<ImagingStudyDto[]>([]);



  useEffect(() => {

    Promise.all([api.getLabOrders(), api.getImagingStudies()])

      .then(([labs, imgs]) => {

        setLabOrders(labs.filter((o) => o.patientId === patientId));

        setImaging(imgs.filter((s) => s.patientId === patientId));

      })

      .catch(() => {

        setLabOrders([]);

        setImaging([]);

      });

  }, [patientId]);



  return (

    <>

      <FeegowPatientEntriesPanel

        patientId={patientId}

        patient={patient}

        config={{

          createButtonLabel: '+ NOVO PEDIDO (PEP)',

          emptyMessage: 'Nenhum pedido de exame no prontuário.',

          contentPlaceholder: 'Descreva os exames solicitados (laboratório, imagem, etc.)…',

          filter: (entry) => entryTypeToNumber(entry.entryType) === 4,

          fixedEntryType: 4,

          showTypeColumn: false,

          pepLink: `/pacientes/${patientId}/prontuario/exames`,

        }}

      />

      <SubsectionTitle>Pedidos laboratoriais</SubsectionTitle>

      <DataTable

        headers={['Data', 'Profissional', 'Status', 'Exames']}

        empty="Nenhum pedido laboratorial."

        rows={labOrders.map((order) => [

          formatBrDateTime(order.createdAt),

          order.requestingProfessionalName,

          labOrderStatusLabels[order.status] ?? String(order.status),

          order.items.map((i) => i.examName).join(', '),

        ])}

      />

      <SubsectionTitle>Pedidos de imagem</SubsectionTitle>

      <DataTable

        headers={['Agendado', 'Modalidade', 'Descrição', 'Status']}

        empty="Nenhum exame de imagem solicitado."

        rows={imaging.map((study) => [

          formatBrDateTime(study.scheduledAt),

          imagingModalityLabels[study.modality] ?? String(study.modality),

          study.studyDescription,

          imagingStatusLabels[study.status] ?? String(study.status),

        ])}

      />

      <p className="feegow-patient-section-pep-link">

        <Link to="/laboratorio">Abrir laboratório</Link>

        {' · '}

        <Link to="/imagem">Abrir imagem</Link>

      </p>

    </>

  );

}



export function FeegowPatientProductsPanel({ patientId }: PatientProps) {

  const [items, setItems] = useState<PharmacyDispensingDto[]>([]);

  const [loading, setLoading] = useState(true);



  useEffect(() => {

    setLoading(true);

    api.getDispensings(patientId)

      .then(setItems)

      .catch(() => setItems([]))

      .finally(() => setLoading(false));

  }, [patientId]);



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando…</p> : null}

      <DataTable

        headers={['Data', 'Produto', 'Qtd.', 'Estornado', 'Profissional', 'Obs.']}

        empty="Nenhum produto dispensado para este paciente."

        rows={items.map((item) => [

          formatBrDateTime(item.dispensedAt),

          item.productName,

          String(item.quantity),

          item.reversedQuantity > 0 ? String(item.reversedQuantity) : '—',

          item.professionalName ?? '—',

          item.notes ?? '—',

        ])}

      />

      <p className="feegow-patient-section-pep-link">

        <Link to="/farmacia">Abrir farmácia</Link>

      </p>

    </>

  );

}



const timelineTypeLabels: Record<string, string> = {
  appointment: 'Consulta',
  triage: 'Triagem',
  hospitalization: 'Internação',
  prescription: 'Prescrição',
  exam_request: 'Exame',
  clinical_note: 'Registro clínico',
  lab_order: 'Laboratório',
  stock_issue: 'Material',
  vital_signs: 'Sinais vitais',
  clinical_alert: 'Alerta clínico',
};

export function FeegowPatientTimelinePanel({ patientId }: PatientProps) {
  const [events, setEvents] = useState<PatientTimelineEventDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    api.getPatientTimeline(patientId)
      .then((timeline) => {
        if (!cancelled) setEvents(timeline.events);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : 'Erro ao carregar linha do tempo');
          setEvents([]);
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [patientId]);

  return (
    <>
      {loading ? <p className="feegow-patient-section-empty">Carregando linha do tempo…</p> : null}
      {error ? <p className="feegow-patient-section-empty text-danger">{error}</p> : null}
      <ul className="feegow-patient-timeline">
        {!loading && events.length === 0 ? (
          <li className="feegow-patient-section-empty">Nenhum evento registrado.</li>
        ) : (
          events.map((entry, index) => (
            <li key={`${entry.type}-${entry.at}-${index}`} className="feegow-patient-timeline-item">
              <time>{formatBrDateTime(entry.at)}</time>
              <strong>{entry.title}</strong>
              <span className="feegow-patient-timeline-cid">{timelineTypeLabels[entry.type] ?? entry.type}</span>
              <p>{entry.description || '—'}</p>
              <span className="feegow-patient-timeline-meta">
                {entry.professionalName ?? 'Profissional não informado'}
              </span>
              {entry.link ? (
                <Link to={entry.link} className="btn btn-sm btn-link" style={{ padding: 0 }}>
                  Ver detalhes
                </Link>
              ) : null}
            </li>
          ))
        )}
      </ul>
    </>
  );
}



export function FeegowPatientImagingPanel({ patientId }: PatientProps) {

  const [studies, setStudies] = useState<ImagingStudyDto[]>([]);

  const [loading, setLoading] = useState(true);



  useEffect(() => {

    setLoading(true);

    api.getImagingStudies()

      .then((all) => setStudies(all.filter((s) => s.patientId === patientId)))

      .catch(() => setStudies([]))

      .finally(() => setLoading(false));

  }, [patientId]);



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando…</p> : null}

      <DataTable

        headers={['Agendado', 'Modalidade', 'Descrição', 'Status', 'Laudo']}

        empty="Nenhum estudo de imagem para este paciente."

        rows={studies.map((study) => [

          formatBrDateTime(study.scheduledAt),

          imagingModalityLabels[study.modality] ?? String(study.modality),

          study.studyDescription,

          imagingStatusLabels[study.status] ?? String(study.status),

          study.reportContent ? 'Sim' : 'Pendente',

        ])}

      />

      <p className="feegow-patient-section-pep-link">

        <Link to="/imagem">Abrir módulo de imagem</Link>

      </p>

    </>

  );

}



export function FeegowPatientFilesPanel(props: PatientProps) {

  return (

    <FeegowPatientEntriesPanel

      {...props}

      config={{

        createButtonLabel: '+ NOVO ANEXO / NOTA',

        emptyMessage: 'Nenhum arquivo ou anexo registrado no prontuário.',

        contentPlaceholder: 'Descreva o documento anexo ou cole referência do conteúdo…',

        filter: (entry) => {

          const text = entry.content ?? '';

          return text.includes('[anexo]') || text.length > 400;

        },

        fixedEntryType: 2,

        showTypeColumn: false,

        showCid10: false,

        pepLink: `/pacientes/${props.patientId}/prontuario/anexos`,

      }}

    />

  );

}



export function FeegowPatientAppointmentsPanel({ patientId }: PatientProps) {

  const [items, setItems] = useState<AppointmentDto[]>([]);

  const [loading, setLoading] = useState(true);



  useEffect(() => {

    setLoading(true);

    api.getAppointments()

      .then((all) => setItems(all.filter((a) => a.patientId === patientId)))

      .catch(() => setItems([]))

      .finally(() => setLoading(false));

  }, [patientId]);



  const sorted = [...items].sort((a, b) => b.scheduledAt.localeCompare(a.scheduledAt));



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando…</p> : null}

      <DataTable

        headers={['Data', 'Profissional', 'Status', 'Motivo', 'Sala']}

        empty="Nenhum agendamento encontrado."

        rows={sorted.map((appt) => [

          formatBrDateTime(appt.scheduledAt),

          appt.professionalName,

          formatAppointmentStatus(appt.status),

          appt.reason ?? '—',

          appt.room ?? '—',

        ])}

      />

      <p className="feegow-patient-section-pep-link">

        <Link to="/recepcao/agendamentos">Abrir agenda da recepção</Link>

      </p>

    </>

  );

}



export function FeegowPatientTasksPanel({ patientId }: PatientProps) {

  const { entries, loading, digital } = usePatientDigitalRecord(patientId);

  const [labOrders, setLabOrders] = useState<LabOrderDto[]>([]);

  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);



  useEffect(() => {

    Promise.all([

      api.getLabOrders(),

      api.getFinancialAccountsByPatient(patientId),

    ])

      .then(([labs, fin]) => {

        setLabOrders(labs.filter((o) => o.patientId === patientId && o.status !== 3));

        setAccounts(fin.filter((a) => isFinancialReceivable(a.direction) && a.balance > 0));

      })

      .catch(() => {

        setLabOrders([]);

        setAccounts([]);

      });

  }, [patientId]);



  const unsigned = entries.filter((e) => !e.isSigned);

  const taskRows: ReactNode[][] = [];



  unsigned.forEach((e) => {

    taskRows.push([

      'Assinatura pendente',

      formatEntryTypeLabel(e.entryType),

      formatBrDateTime(e.createdAt),

      <Link key={`sign-${e.id}`} to="/pep/assinaturas">Assinar no PEP</Link>,

    ]);

  });

  labOrders.forEach((o) => {

    taskRows.push([

      'Exame laboratorial',

      o.items.map((i) => i.examName).join(', '),

      formatBrDateTime(o.createdAt),

      <Link key={`lab-${o.id}`} to="/laboratorio">Laboratório</Link>,

    ]);

  });

  accounts.forEach((a) => {

    taskRows.push([

      'Financeiro',

      a.description,

      a.dueDate ? formatBrDate(a.dueDate) : '—',

      <Link key={`fin-${a.id}`} to="/financeiro">Financeiro</Link>,

    ]);

  });

  if (digital?.activeHospitalization) {

    taskRows.push([

      'Internação ativa',

      digital.activeHospitalization.wardName,

      formatBrDate(digital.activeHospitalization.admittedAt),

      <Link key="hosp" to="/internacao">Internação</Link>,

    ]);

  }



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando tarefas…</p> : null}

      <DataTable

        headers={['Tipo', 'Descrição', 'Data', 'Ação']}

        empty="Nenhuma tarefa pendente para este paciente."

        rows={taskRows}

      />

    </>

  );

}



function FinancialAccountsTable({

  accounts,

  empty,

}: {

  accounts: FinancialAccountDto[];

  empty: string;

}) {

  return (

    <DataTable

      headers={['Emissão', 'Descrição', 'Categoria', 'Valor', 'Pago', 'Status']}

      empty={empty}

      rows={accounts.map((a) => [

        formatBrDate(a.createdAt),

        a.description,

        financialCategoryLabel(a.category),

        a.amount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }),

        a.paidAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }),

        <FinancialStatusBadge status={a.status} />,

      ])}

    />

  );

}



export function FeegowPatientReceiptsPanel({ patientId }: PatientProps) {

  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);

  const [loading, setLoading] = useState(true);



  useEffect(() => {

    setLoading(true);

    api.getFinancialAccountsByPatient(patientId)

      .then((all) => setAccounts(

        all.filter((a) => isFinancialReceivable(a.direction) && a.paidAmount > 0),

      ))

      .catch(() => setAccounts([]))

      .finally(() => setLoading(false));

  }, [patientId]);



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando…</p> : null}

      <FinancialAccountsTable accounts={accounts} empty="Nenhum recibo / recebimento registrado." />

      <p className="feegow-patient-section-pep-link">

        <Link to="/financeiro/contas-a-receber/listar">Contas a receber</Link>

      </p>

    </>

  );

}



export function FeegowPatientProposalsPanel({ patientId }: PatientProps) {

  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);

  const [loading, setLoading] = useState(true);



  useEffect(() => {

    setLoading(true);

    api.getFinancialAccountsByPatient(patientId)

      .then((all) => setAccounts(

        all.filter((a) =>

          isFinancialReceivable(a.direction)

          && a.balance > 0

          && (a.description.toLowerCase().includes('proposta')

            || (a.notes ?? '').toLowerCase().includes('proposta')),

        ),
      ))

      .catch(() => setAccounts([]))

      .finally(() => setLoading(false));

  }, [patientId]);



  return (

    <>

      {loading ? <p className="feegow-patient-section-empty">Carregando…</p> : null}

      <FinancialAccountsTable accounts={accounts} empty="Nenhuma proposta ou orçamento em aberto." />

      <p className="feegow-patient-section-pep-link">

        <Link to="/financeiro/propostas">Propostas no financeiro</Link>

      </p>

    </>

  );

}



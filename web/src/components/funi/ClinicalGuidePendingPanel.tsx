import { useEffect, useState } from 'react';

import { Link } from 'react-router-dom';

import { api, type HealthInsuranceDto, type TissClinicalSourceDto } from '../../api/client';

import { FUNI_GUIDE_BASE } from '../../data/funiGuides/catalog';

import {

  ClinicalDocumentKind,

  generateGuideFromClinicalData,

  generateReportFromClinicalData,

  guideSlugByType,

  guideTypeLabel,

  parseClinicalFormData,

  reportSlugByCode,

  type ReportCaptureData,

} from '../../utils/clinicalDocumentWorkflow';

import { formatBrDateTime } from '../../utils/dateUtils';

import { printFuniGuide } from '../../utils/printFuniGuide';



type Props = {

  guideType?: number;

  documentKind?: number;

  reportCode?: string;

  insurances: HealthInsuranceDto[];

};



function sourceLink(source: TissClinicalSourceDto): string {

  if (source.documentKind === ClinicalDocumentKind.Report && source.reportCode) {

    const slug = reportSlugByCode[source.reportCode] ?? 'demonstrativo-analise';

    return `${FUNI_GUIDE_BASE}/${slug}?sourceId=${source.id}`;

  }

  return `${FUNI_GUIDE_BASE}/${guideSlugByType[source.guideType] ?? 'consulta'}?sourceId=${source.id}`;

}



function sourceTypeLabel(source: TissClinicalSourceDto): string {

  if (source.documentKind === ClinicalDocumentKind.Report) {

    return source.reportCode ? `Relatório ${source.reportCode}` : 'Relatório';

  }

  return guideTypeLabel(source.guideType);

}



export function ClinicalGuidePendingPanel({ guideType, documentKind, reportCode, insurances }: Props) {

  const [sources, setSources] = useState<TissClinicalSourceDto[]>([]);

  const [loading, setLoading] = useState(true);

  const [busyId, setBusyId] = useState<string | null>(null);

  const [error, setError] = useState('');

  const [success, setSuccess] = useState('');



  async function load() {

    setLoading(true);

    try {

      const list = await api.getClinicalSources({

        guideType,

        documentKind,

        reportCode,

        pendingOnly: true,

      });

      setSources(list);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar dados clínicos.');

    } finally {

      setLoading(false);

    }

  }



  useEffect(() => {

    load().catch(console.error);

  }, [guideType, documentKind, reportCode]);



  async function handleGenerate(source: TissClinicalSourceDto) {

    setError('');

    setSuccess('');

    const formData = parseClinicalFormData(source.formDataJson);

    if (!formData) {

      setError('Dados clínicos inválidos.');

      return;

    }



    const context = {

      appointmentId: source.appointmentId,

      hospitalizationId: source.hospitalizationId,

      chemotherapySessionId: source.chemotherapySessionId,

      surgeryId: source.surgeryId,

      labOrderId: source.labOrderId,

      imagingStudyId: source.imagingStudyId,

      label: source.label,

    };



    setBusyId(source.id);

    try {

      if (source.documentKind === ClinicalDocumentKind.Report) {

        const capture = formData as ReportCaptureData;

        if (!capture.reportCode) {

          setError('Código do relatório não informado.');

          return;

        }

        const { result } = await generateReportFromClinicalData(

          source.patientId,

          capture.reportCode,

          capture.reportName,

          context,

          capture,

          source.id,

        );

        setSuccess(`Relatório gerado (${result.rows.length} linha(s)).`);

      } else {

        const insuranceId = source.healthInsuranceId

          ?? insurances.find((i) => i.name === source.healthInsuranceName)?.id;

        if (!insuranceId) {

          setError(`Revise o convênio em ${sourceLink(source)} antes de gerar.`);

          return;

        }

        const { guide } = await generateGuideFromClinicalData(

          source.patientId,

          source.guideType,

          insuranceId,

          context,

          formData,

          source.id,

        );

        setSuccess(`Guia ${guide.guideNumber} gerada com preenchimento automático.`);

        if (source.guideType === 1 || source.guideType === 17 || source.guideType === 18) {

          setTimeout(() => printFuniGuide(`${guideTypeLabel(source.guideType)} — ${source.patientName}`), 300);

        }

      }

      await load();

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao gerar documento.');

    } finally {

      setBusyId(null);

    }

  }



  if (loading) {

    return (

      <div className="tab-pane box active" style={{ marginBottom: 15 }}>

        <div className="bayanno-panel-head">

          <span className="title"><i className="icon-time" aria-hidden /> Dados capturados no sistema</span>

        </div>

        <p className="bayanno-inline-hint">Carregando…</p>

      </div>

    );

  }



  if (sources.length === 0) {

    return (

      <div className="tab-pane box active" style={{ marginBottom: 15 }}>

        <div className="bayanno-panel-head">

          <span className="title"><i className="icon-file" aria-hidden /> Dados capturados no sistema</span>

          <span className="bayanno-panel-hint">Guias e relatórios gerados a partir do atendimento</span>

        </div>

        <p className="bayanno-inline-hint">

          Nenhum registro pendente. Capture os dados nos módulos assistenciais — os documentos aparecerão aqui para revisão e geração.

        </p>

      </div>

    );

  }



  return (

    <div className="tab-pane box active" style={{ marginBottom: 15 }}>

      <div className="bayanno-panel-head">

        <span className="title">

          <i className="icon-magic" aria-hidden />

          {' '}

          Gerar documento ({sources.length})

        </span>

        <span className="bayanno-panel-hint">A partir dos dados já capturados no sistema</span>

      </div>

      {error ? <p className="bayanno-inline-hint" style={{ color: '#c0392b' }}>{error}</p> : null}

      {success ? <p className="bayanno-inline-hint" style={{ color: '#3d8b3d' }}>{success}</p> : null}

      <div className="table-responsive-wrap">

        <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">

          <thead>

            <tr>

              <th><div>#</div></th>

              <th><div>Paciente</div></th>

              <th><div>Tipo</div></th>

              <th><div>Contexto</div></th>

              <th><div>Atualizado</div></th>

              <th><div>Opções</div></th>

            </tr>

          </thead>

          <tbody>

            {sources.map((s, index) => (

              <tr key={s.id} className={index % 2 === 1 ? 'even' : undefined}>

                <td>{index + 1}</td>

                <td><strong>{s.patientName}</strong></td>

                <td>{sourceTypeLabel(s)}</td>

                <td>{s.label}</td>

                <td>{formatBrDateTime(s.updatedAt ?? s.createdAt)}</td>

                <td className="center">

                  <div className="bayanno-table-actions">

                    <Link className="btn btn-blue btn-sm" to={sourceLink(s)}>

                      <i className="icon-search" aria-hidden /> Revisar

                    </Link>

                    <button

                      type="button"

                      className="btn btn-green btn-sm"

                      disabled={busyId === s.id}

                      onClick={() => handleGenerate(s)}

                    >

                      <i className="icon-play" aria-hidden />

                      {' '}

                      {busyId === s.id

                        ? 'Gerando…'

                        : s.documentKind === ClinicalDocumentKind.Report

                          ? 'Relatório'

                          : 'Guia'}

                    </button>

                  </div>

                </td>

              </tr>

            ))}

          </tbody>

        </table>

      </div>

    </div>

  );

}


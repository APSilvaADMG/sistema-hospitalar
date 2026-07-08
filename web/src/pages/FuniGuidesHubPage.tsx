import { useEffect, useState } from 'react';
import { Link, Navigate, useSearchParams } from 'react-router-dom';
import { api, type HealthInsuranceDto, type PatientDto } from '../api/client';
import { ClinicalGuidePendingPanel } from '../components/funi/ClinicalGuidePendingPanel';
import { guideTypeBySlug } from '../utils/clinicalDocumentWorkflow';
import { FuniConsultationGuideForm } from '../components/funi/FuniConsultationGuideForm';
import { FuniChemotherapyGuideForm } from '../components/funi/FuniChemotherapyGuideForm';
import { FuniRadiotherapyGuideForm } from '../components/funi/FuniRadiotherapyGuideForm';
import { TissGuideCaptureForm } from '../components/clinical/TissGuideCaptureForm';
import { ReportClinicalCaptureForm } from '../components/clinical/ReportClinicalCaptureForm';
import '../components/funi/funiGuide.css';
import { FUNI_GUIDE_BASE, FUNI_GUIDE_CATALOG, getFuniGuideBySlug, getFuniPdfUrl } from '../data/funiGuides/catalog';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { tissTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { useAuth } from '../auth/AuthContext';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';

type FuniFormProps = {
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  initialSourceId?: string;
};

export function FuniGuidesHubPage() {
  const { appearance } = useAppearance();
  const { hasPermission } = useAuth();
  const [searchParams] = useSearchParams();

  if (isFeegowBrand(appearance.brand)) {
    return <Navigate to="/faturamento-tiss" replace />;
  }
  const sourceId = searchParams.get('sourceId') ?? undefined;
  const { section } = useModuleSection(FUNI_GUIDE_BASE);
  const canAccess = hasPermission('billing.read', 'billing.write');
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [insurances, setInsurances] = useState<HealthInsuranceDto[]>([]);

  const activeGuide = section ? getFuniGuideBySlug(section) : undefined;

  useEffect(() => {
    if (!canAccess) return;
    Promise.all([
      api.getPatients('', 1).then((r) => setPatients(Array.isArray(r.items) ? r.items : [])),
      api.getHealthInsurances(),
    ]).then(([, ins]) => setInsurances(Array.isArray(ins) ? ins : [])).catch(console.error);
  }, [canAccess]);

  if (!canAccess) {
    return <div className="card">Acesso restrito ao faturamento TISS.</div>;
  }

  if (activeGuide && activeGuide.status === 'implemented') {
    const formProps: FuniFormProps = { patients, insurances, initialSourceId: sourceId };
    const guideType = activeGuide.tissGuideType ?? guideTypeBySlug[activeGuide.slug];

    return (
      <>
        <PageHeader
          eyebrow="Faturamento TISS"
          title={`${activeGuide.funiCode} — ${activeGuide.title}`}
          subtitle="Capture os dados no atendimento e gere a guia ou relatório aqui com preenchimento automático."
        />
        <ModuleNav basePath="/faturamento-tiss" tabs={tissTabs} contextId="insurance" />
        <ClinicalGuidePendingPanel
          guideType={activeGuide.captureMode !== 'report' ? guideType : undefined}
          documentKind={activeGuide.captureMode === 'report' ? 1 : 0}
          reportCode={activeGuide.reportCode}
          insurances={insurances}
        />
        {activeGuide.captureMode === 'funi-form' && activeGuide.slug === 'consulta' && (
          <FuniConsultationGuideForm {...formProps} />
        )}
        {activeGuide.captureMode === 'funi-form' && activeGuide.slug === 'quimioterapia' && (
          <FuniChemotherapyGuideForm {...formProps} />
        )}
        {activeGuide.captureMode === 'funi-form' && activeGuide.slug === 'radioterapia' && (
          <FuniRadiotherapyGuideForm {...formProps} />
        )}
        {activeGuide.captureMode === 'tiss-generic' && guideType != null && (
          <TissGuideCaptureForm
            guideType={guideType}
            guideTitle={`${activeGuide.funiCode} — ${activeGuide.title}`}
            patients={patients}
            insurances={insurances}
            initialSourceId={sourceId}
          />
        )}
        {activeGuide.captureMode === 'report' && activeGuide.reportCode && (
          <ReportClinicalCaptureForm
            reportCode={activeGuide.reportCode}
            reportName={`${activeGuide.funiCode} — ${activeGuide.title}`}
            patients={patients}
            initialSourceId={sourceId}
          />
        )}
      </>
    );
  }

  if (activeGuide) {
    const pdfUrl = getFuniPdfUrl(activeGuide.pdfFile);
    return (
      <>
        <PageHeader eyebrow="Faturamento TISS" title={activeGuide.title} subtitle={`${activeGuide.funiCode} · ${activeGuide.revision}`} />
        <ModuleNav basePath="/faturamento-tiss" tabs={tissTabs} contextId="insurance" />
        <div className="card-panel">
          <p>Esta guia está catalogada. O formulário digital será disponibilizado em breve — use o PDF oficial abaixo para referência.</p>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 12 }}>
            <a className="btn" href={pdfUrl} target="_blank" rel="noreferrer">Abrir PDF oficial</a>
            <Link className="btn btn-secondary" to={FUNI_GUIDE_BASE}>Voltar ao catálogo</Link>
          </div>
          <iframe title={`PDF ${activeGuide.funiCode}`} src={pdfUrl} style={{ width: '100%', height: '70vh', border: '1px solid var(--border)', borderRadius: 8 }} />
        </div>
      </>
    );
  }

  const implemented = FUNI_GUIDE_CATALOG.filter((g) => g.status === 'implemented').length;

  return (
    <>
      <PageHeader
        eyebrow="Faturamento TISS"
        title="Guias FUNI (formulários TISS)"
        subtitle={`${implemented} de ${FUNI_GUIDE_CATALOG.length} guias · dados capturados no sistema · geração automática no faturamento`}
      />
      <ModuleNav basePath="/faturamento-tiss" tabs={tissTabs} contextId="insurance" />
      <ClinicalGuidePendingPanel insurances={insurances} />

      <div className="tab-pane box active funi-guide-catalog-table">
        <div className="bayanno-panel-head">
          <span className="title">
            <i className="icon-file" aria-hidden />
            {' '}
            Catálogo FUNI ({FUNI_GUIDE_CATALOG.length} guias)
          </span>
          <span className="bayanno-panel-hint">
            {implemented} formulários digitais disponíveis · demais com PDF oficial
          </span>
        </div>
        <div className="table-responsive-wrap">
          <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">
            <thead>
              <tr>
                <th><div>#</div></th>
                <th><div>Código</div></th>
                <th><div>Guia</div></th>
                <th><div>Revisão</div></th>
                <th><div>Descrição</div></th>
                <th><div>Status</div></th>
                <th><div>Opções</div></th>
              </tr>
            </thead>
            <tbody>
              {FUNI_GUIDE_CATALOG.map((guide, index) => (
                <tr key={guide.id} className={index % 2 === 1 ? 'even' : undefined}>
                  <td>{index + 1}</td>
                  <td><strong>{guide.funiCode}</strong></td>
                  <td>{guide.title}</td>
                  <td>{guide.revision}</td>
                  <td>{guide.description}</td>
                  <td>
                    <span className={`bayanno-status-badge${guide.status === 'implemented' ? ' is-ready' : ' is-pending'}`}>
                      {guide.status === 'implemented' ? 'Disponível' : 'Em fila'}
                    </span>
                  </td>
                  <td className="center">
                    <div className="bayanno-table-actions">
                      {guide.status === 'implemented' ? (
                        <Link className="btn btn-green btn-sm" to={`${FUNI_GUIDE_BASE}/${guide.slug}`}>
                          <i className="icon-edit" aria-hidden /> Formulário
                        </Link>
                      ) : null}
                      <a className="btn btn-blue btn-sm" href={getFuniPdfUrl(guide.pdfFile)} target="_blank" rel="noreferrer">
                        PDF
                      </a>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

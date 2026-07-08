import { useEffect, useState, type ReactNode } from 'react';

import { Link } from 'react-router-dom';

import { api, type ReportCatalogItemDto } from '../api/client';

import {

  getContextFuniGuides,

  getContextQuickLinks,

  getContextReportsPath,

  funiGuideUrl,

  MODULE_CONTEXT,

  type ModuleContextId,

} from '../navigation/contextualModules';

import { getFuniPdfUrl } from '../data/funiGuides/catalog';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';



type Props = {

  contextId: ModuleContextId;

  /** Máximo de relatórios listados inline. */

  maxReports?: number;

};



const CONTEXT_UI: Record<ModuleContextId, {

  quick: string | null;

  quickHint: string | null;

  funi: string;

  reports: string | null;

}> = {

  reception: {

    quick: 'Guia de Consulta (FUNI)',

    quickHint: 'Documento principal da recepção e ambulatório',

    funi: 'Demais guias FUNI',

    reports: 'Relatórios de Recepção',

  },

  emergency: {

    quick: 'Guias FUNI do Pronto Atendimento',

    quickHint: 'Consulta e SP/SADT para PS',

    funi: 'Demais documentos TISS',

    reports: 'Relatórios de Pronto Atendimento',

  },

  hospitalization: {

    quick: 'Guias AIH (SUS) e Resumo de Internação',

    quickHint: 'Atalhos do fluxo de internação',

    funi: 'Demais guias FUNI de internação',

    reports: 'Relatórios de Internação',

  },

  medicalRecord: {

    quick: 'Guias FUNI do Prontuário',

    quickHint: 'Documentos TISS a partir do PEP',

    funi: 'Honorários e demais guias',

    reports: 'Relatórios do Prontuário',

  },

  nursing: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Enfermagem',

  },

  surgery: {

    quick: 'Guias FUNI do Centro Cirúrgico',

    quickHint: 'OPME e honorários cirúrgicos',

    funi: 'Outras despesas hospitalares',

    reports: 'Relatórios do Centro Cirúrgico',

  },

  pharmacy: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Farmácia',

  },

  laboratory: {

    quick: 'Guia SP/SADT (Laboratório)',

    quickHint: 'Solicitação de exames laboratoriais',

    funi: 'Demais documentos TISS',

    reports: 'Relatórios de Laboratório',

  },

  imaging: {

    quick: 'Guia SP/SADT (Imagem)',

    quickHint: 'Solicitação de exames de imagem',

    funi: 'Demais documentos TISS',

    reports: 'Relatórios de Diagnóstico por Imagem',

  },

  hemotherapy: {

    quick: 'Guia SP/SADT (Hemoterapia)',

    quickHint: 'Procedimentos e exames hemoterápicos',

    funi: 'Demais documentos TISS',

    reports: 'Relatórios de Hemoterapia',

  },

  oncology: {

    quick: 'Guias de Quimioterapia e Radioterapia',

    quickHint: 'Formulários TISS para autorização e faturamento',

    funi: 'SP/SADT e demais documentos TISS',

    reports: null,

  },

  financial: {

    quick: null,

    quickHint: null,

    funi: 'Demonstrativo de pagamento',

    reports: 'Relatórios Financeiros',

  },

  insurance: {

    quick: 'Guias FUNI e Faturamento TISS',

    quickHint: 'Catálogo e formulários digitais',

    funi: 'Catálogo completo FUNI (TISS)',

    reports: 'Relatórios de Faturamento TISS',

  },

  hospitalBilling: {

    quick: 'Faturamento SUS e Internação',

    quickHint: 'AIH, APAC e resumo de internação',

    funi: 'Demonstrativos e demais guias',

    reports: 'Relatórios de Faturamento Hospitalar',

  },

  humanResources: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de RH',

  },

  quality: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Qualidade',

  },

  infectionControl: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios da CCIH',

  },

  supply: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Suprimentos',

  },

  businessIntelligence: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios Executivos (BI)',

  },

  audit: {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Auditoria',

  },

  regulatory: {

    quick: 'Regulação e Integrações SUS',

    quickHint: 'SISREG, CNES, SIH e demais',

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios Regulatórios',

  },

  securityLgpd: {

    quick: 'Consentimentos e LGPD',

    quickHint: 'Coleta, titular e conformidade',

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Auditoria',

  },

  physicalAccess: {

    quick: 'Check-in e Portaria',

    quickHint: 'Integração com catracas e visitantes',

    funi: 'Guias TISS (FUNI)',

    reports: 'Relatórios de Recepção',

  },

};



function contextTitles(contextId: ModuleContextId, label: string) {

  const ui = CONTEXT_UI[contextId];

  if (ui) return ui;

  return {

    quick: null,

    quickHint: null,

    funi: 'Guias TISS (FUNI)',

    reports: `Relatórios · ${label}`,

  };

}



function BayannoPanel({

  title,

  hint,

  icon = 'icon-file',

  actions,

  children,

}: {

  title: string;

  hint?: string | null;

  icon?: string;

  actions?: ReactNode;

  children: ReactNode;

}) {

  return (

    <div className="tab-pane box active">

      <div className="bayanno-panel-head">

        {actions ? <div className="bayanno-panel-actions">{actions}</div> : null}

        <span className="title">

          <i className={icon} aria-hidden />

          {title}

        </span>

        {hint ? <span className="bayanno-panel-hint">{hint}</span> : null}

      </div>

      <div className="bayanno-panel-body">{children}</div>

    </div>

  );

}



export function ModuleContextTools({ contextId, maxReports = 8 }: Props) {
  const { appearance } = useAppearance();
  if (isFeegowBrand(appearance.brand)) return null;

  const config = MODULE_CONTEXT[contextId];

  const funiGuides = getContextFuniGuides(contextId);

  const quickLinks = getContextQuickLinks(contextId);

  const [reports, setReports] = useState<ReportCatalogItemDto[]>([]);

  const [loadingReports, setLoadingReports] = useState(false);



  const titles = contextTitles(contextId, config.label);



  useEffect(() => {

    if (!config.reportModule) return;

    setLoadingReports(true);

    api

      .getReportsCatalog({

        module: config.reportModule,

        implementedOnly: true,

      })

      .then((items) => setReports(Array.isArray(items) ? items.slice(0, maxReports) : []))

      .catch(console.error)

      .finally(() => setLoadingReports(false));

  }, [config.reportModule, maxReports]);



  if (funiGuides.length === 0 && !config.reportModule && quickLinks.length === 0) return null;



  return (

    <div className="bayanno-module-tools module-context-tools no-print">

      {quickLinks.length > 0 && titles.quick && (

        <BayannoPanel title={titles.quick} hint={titles.quickHint} icon="icon-star">

          <div className="table-responsive-wrap">

            <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">

              <thead>

                <tr>

                  <th><div>#</div></th>

                  <th><div>Documento</div></th>

                  <th><div>Descrição</div></th>

                  <th><div>Opções</div></th>

                </tr>

              </thead>

              <tbody>

                {quickLinks.map((link, index) => (

                  <tr key={link.to} className={index % 2 === 1 ? 'even' : undefined}>

                    <td>{index + 1}</td>

                    <td>{link.label}</td>

                    <td>{link.description ?? '—'}</td>

                    <td className="center">

                      <Link className="btn btn-green btn-sm" to={link.to}>

                        <i className="icon-folder-open" aria-hidden /> Abrir

                      </Link>

                    </td>

                  </tr>

                ))}

              </tbody>

            </table>

          </div>

        </BayannoPanel>

      )}



      {funiGuides.length > 0 && (

        <BayannoPanel title={titles.funi} hint="Leia, preencha ou abra o PDF oficial" icon="icon-file">

          <div className="table-responsive-wrap">

            <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">

              <thead>

                <tr>

                  <th><div>#</div></th>

                  <th><div>Código</div></th>

                  <th><div>Guia TISS</div></th>

                  <th><div>Descrição</div></th>

                  <th><div>Status</div></th>

                  <th><div>Opções</div></th>

                </tr>

              </thead>

              <tbody>

                {funiGuides.map((guide, index) => (

                  <tr key={guide.id} className={index % 2 === 1 ? 'even' : undefined}>

                    <td>{index + 1}</td>

                    <td><strong>{guide.funiCode}</strong></td>

                    <td>{guide.title}</td>

                    <td>{guide.description}</td>

                    <td>

                      <span className={`bayanno-status-badge${guide.status === 'implemented' ? ' is-form' : ' is-pending'}`}>

                        {guide.status === 'implemented' ? 'Formulário' : 'PDF / catálogo'}

                      </span>

                    </td>

                    <td className="center">

                      <div className="bayanno-table-actions">

                        {guide.status === 'implemented' ? (

                          <Link className="btn btn-green btn-sm" to={funiGuideUrl(guide.slug)}>

                            <i className="icon-edit" aria-hidden /> Formulário

                          </Link>

                        ) : (

                          <>

                            <Link className="btn btn-green btn-sm" to={funiGuideUrl(guide.slug)}>

                              Abrir

                            </Link>

                            <a className="btn btn-blue btn-sm" href={getFuniPdfUrl(guide.pdfFile)} target="_blank" rel="noreferrer">

                              PDF

                            </a>

                          </>

                        )}

                      </div>

                    </td>

                  </tr>

                ))}

              </tbody>

            </table>

          </div>

        </BayannoPanel>

      )}



      {config.reportModule && titles.reports && (

        <BayannoPanel

          title={titles.reports}

          hint="Relatórios disponíveis neste módulo"

          icon="icon-bar-chart"

          actions={(

            <Link className="btn btn-secondary btn-sm" to={getContextReportsPath(contextId)}>

              Ver todos

            </Link>

          )}

        >

          {loadingReports && <p className="bayanno-inline-hint">Carregando relatórios…</p>}

          {!loadingReports && reports.length === 0 && (

            <p className="bayanno-inline-hint">Nenhum relatório disponível neste módulo ainda.</p>

          )}

          {reports.length > 0 && (

            <div className="table-responsive-wrap">

              <table cellPadding={0} cellSpacing={0} className="dTable responsive dataTable">

                <thead>

                  <tr>

                    <th><div>#</div></th>

                    <th><div>Relatório</div></th>

                    <th><div>Descrição</div></th>

                    <th><div>Opções</div></th>

                  </tr>

                </thead>

                <tbody>

                  {reports.map((item, index) => (

                    <tr key={item.code} className={index % 2 === 1 ? 'even' : undefined}>

                      <td>{index + 1}</td>

                      <td>

                        {item.name}

                        {item.isEssential ? (

                          <span className="bayanno-status-badge is-form" style={{ marginLeft: 6 }}>MVP</span>

                        ) : null}

                      </td>

                      <td>{item.description}</td>

                      <td className="center">

                        <Link

                          className="btn btn-green btn-sm"

                          to={`${getContextReportsPath(contextId)}&q=${encodeURIComponent(item.code)}`}

                        >

                          <i className="icon-play" aria-hidden /> Gerar

                        </Link>

                      </td>

                    </tr>

                  ))}

                </tbody>

              </table>

            </div>

          )}

        </BayannoPanel>

      )}

    </div>

  );

}


export type FuniGuideStatus = 'implemented' | 'prototype' | 'planned';

export type FuniGuideCaptureMode = 'funi-form' | 'tiss-generic' | 'report';

export type FuniGuideDefinition = {
  id: string;
  funiCode: string;
  revision: string;
  title: string;
  slug: string;
  pdfFile: string;
  tissGuideType?: number;
  reportCode?: string;
  captureMode: FuniGuideCaptureMode;
  status: FuniGuideStatus;
  description: string;
};

/** Catálogo das guias FUNI em Diversos/guias — espelho TISS/ANS para o APSMedCore. */
export const FUNI_GUIDE_CATALOG: FuniGuideDefinition[] = [
  {
    id: 'funi-21',
    funiCode: 'FUNI 21',
    revision: 'Rev. 01',
    title: 'Guia de Consulta',
    slug: 'consulta',
    pdfFile: 'FUNI 21 - GUIA DE CONSULTA REV.01.pdf',
    tissGuideType: 1,
    captureMode: 'funi-form',
    status: 'implemented',
    description: 'Consultas eletivas, retorno e ambulatoriais (TISS guiaConsulta).',
  },
  {
    id: 'funi-13',
    funiCode: 'FUNI 13',
    revision: 'Rev. 01',
    title: 'Guia SP/SADT',
    slug: 'sp-sadt',
    pdfFile: 'FUNI 13 - GUIA DE SERVIÇO PROFISSIONAL - SERVIÇO AUXILIAR DE DIAGNÓSTICO E TERAPIA - SP SADT REV 01.pdf',
    tissGuideType: 2,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Exames, terapias e procedimentos ambulatoriais.',
  },
  {
    id: 'funi-22',
    funiCode: 'FUNI 22',
    revision: 'Rev. 02',
    title: 'Solicitação de Internação',
    slug: 'solicitacao-internacao',
    pdfFile: 'FUNI 22 - GUIA DE SOLICITAÇÃO DE INTERNAÇÃO - CURVAS - REV.02.pdf',
    tissGuideType: 6,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Autorização prévia de internação hospitalar.',
  },
  {
    id: 'funi-23',
    funiCode: 'FUNI 23',
    revision: 'Rev. 01',
    title: 'Resumo de Internação',
    slug: 'resumo-internacao',
    pdfFile: 'FUNI 23 - GUIA DE RESUMO DE INTERNAÇÃO REV.01.pdf',
    tissGuideType: 4,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Faturamento após alta hospitalar.',
  },
  {
    id: 'funi-24',
    funiCode: 'FUNI 24',
    revision: 'Rev. 01',
    title: 'Honorários Individuais',
    slug: 'honorarios',
    pdfFile: 'FUNI 24 - GUIA DE HONORÁRIOS REV.01.pdf',
    tissGuideType: 5,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Honorários médicos da equipe (CC/UTI).',
  },
  {
    id: 'funi-25',
    funiCode: 'FUNI 25',
    revision: 'Rev. 01',
    title: 'Outras Despesas',
    slug: 'outras-despesas',
    pdfFile: 'FUNI 25 - ANEXO DE OUTRAS DESPESAS REV. 01.pdf',
    tissGuideType: 7,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Materiais e despesas auxiliares (SIMPRO/Brasíndice).',
  },
  {
    id: 'funi-39',
    funiCode: 'FUNI 39',
    revision: 'Rev. 02',
    title: 'Prorrogação de Internação',
    slug: 'prorrogacao',
    pdfFile: 'FUNI 39 - GUIA DE SOLICITAÇÃO DE PRORROGAÇÃO DE INTERNAÇÃO OU COMPLETAÇÃO DO TRATAMENTO REV. 02.pdf',
    tissGuideType: 9,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Prorrogação ou complementação de tratamento.',
  },
  {
    id: 'funi-54',
    funiCode: 'FUNI 54',
    revision: 'Rev. 01',
    title: 'Anexo OPME',
    slug: 'opme',
    pdfFile: 'FUNI 54 - GUIA DE ANEXO DE SOLICITAÇÃO DE ORTESES, PROTESES E MATERIAIS ESPECIAIS - OPME REV. 01.pdf',
    tissGuideType: 16,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Órteses, próteses e materiais especiais.',
  },
  {
    id: 'funi-55',
    funiCode: 'FUNI 55',
    revision: 'Rev. 00',
    title: 'Anexo Quimioterapia',
    slug: 'quimioterapia',
    pdfFile: 'FUNI 55 - GUIA ANEXO DE SOLICITAÇÃO DE QUIMIOTERAPIA - REV. 00.pdf',
    tissGuideType: 17,
    captureMode: 'funi-form',
    status: 'implemented',
    description: 'Solicitação de quimioterapia.',
  },
  {
    id: 'funi-56',
    funiCode: 'FUNI 56',
    revision: 'Rev. 00',
    title: 'Anexo Radioterapia',
    slug: 'radioterapia',
    pdfFile: 'FUNI 56 - GUIA DE ANEXO SOLICITAÇÃO DE RADIOTERAPIA - REV. 00.pdf',
    tissGuideType: 18,
    captureMode: 'funi-form',
    status: 'implemented',
    description: 'Solicitação de radioterapia.',
  },
  {
    id: 'funi-57',
    funiCode: 'FUNI 57',
    revision: 'Rev. 00',
    title: 'Demonstrativo de Análise de Conta',
    slug: 'demonstrativo-analise',
    pdfFile: 'FUNI 57 - GUIA DE DEMONSTRATIVO DE ANÁLISE DE CONTA - REV. 00.pdf',
    reportCode: 'ins.glosas.by-reason',
    captureMode: 'report',
    status: 'implemented',
    description: 'Retorno da operadora com glosas e análise.',
  },
  {
    id: 'funi-58',
    funiCode: 'FUNI 58',
    revision: 'Rev. 00',
    title: 'Demonstrativo de Pagamento',
    slug: 'demonstrativo-pagamento',
    pdfFile: 'FUNI 58 - GUIA DE DEMONSTRATIVO DE PAGAMENTO - REV. 00.pdf',
    tissGuideType: 11,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Pagamentos efetuados pela operadora.',
  },
  {
    id: 'funi-59',
    funiCode: 'FUNI 59',
    revision: 'Rev. 00',
    title: 'Recurso de Glosas',
    slug: 'recurso-glosas',
    pdfFile: 'FUNI 59 - GUIA DE RECURSO DE GLOSAS - REV. 00.pdf',
    tissGuideType: 10,
    captureMode: 'tiss-generic',
    status: 'implemented',
    description: 'Contestação de glosas TISS.',
  },
];

export function getFuniGuideBySlug(slug: string): FuniGuideDefinition | undefined {
  return FUNI_GUIDE_CATALOG.find((g) => g.slug === slug);
}

export const FUNI_GUIDE_BASE = '/faturamento-tiss/guias-funi';

/** URL pública do PDF oficial (copiado de Diversos/guias para web/public/guias). */
export function getFuniPdfUrl(pdfFile: string): string {
  return `/guias/${encodeURIComponent(pdfFile)}`;
}

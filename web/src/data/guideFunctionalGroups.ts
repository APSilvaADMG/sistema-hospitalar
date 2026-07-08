/** Grupos funcionais do hub de Gestão de Guias. */

export type GuideQuickLink = {
  label: string;
  path: string;
  description?: string;
  susGuideType?: number;
};

export type GuideFunctionalGroup = {
  id: string;
  slug: string;
  label: string;
  description: string;
  guideTypes?: number[];
  statusFilter?: number[];
  quickLinks?: GuideQuickLink[];
};

export const GUIDE_FUNCTIONAL_GROUPS: GuideFunctionalGroup[] = [
  {
    id: 'consultas',
    slug: 'consultas',
    label: 'Guias de Consulta',
    description: 'Emissão e acompanhamento de consultas eletivas, retorno, urgência e convênio',
    guideTypes: [1],
    quickLinks: [
      { label: 'Nova consulta TISS', path: '/faturamento-tiss', description: 'Guia de consulta convênio' },
      { label: 'Guia FUNI — Consulta', path: '/faturamento-tiss/guias-funi/consulta' },
    ],
  },
  {
    id: 'exames',
    slug: 'exames',
    label: 'Guias de Exames',
    description: 'Laboratorial, imagem e exames especializados com autorização e execução',
    guideTypes: [2],
    quickLinks: [
      { label: 'SP/SADT TISS', path: '/faturamento-tiss' },
      { label: 'Guia FUNI — SP/SADT', path: '/faturamento-tiss/guias-funi/sp-sadt' },
      { label: 'Laboratório', path: '/laboratorio' },
      { label: 'Imagem', path: '/imagem' },
    ],
  },
  {
    id: 'procedimentos',
    slug: 'procedimentos',
    label: 'Guias de Procedimentos',
    description: 'Ambulatoriais, hospitalares, cirúrgicos e alta complexidade',
    guideTypes: [2, 3, 5, 7],
    quickLinks: [
      { label: 'Centro cirúrgico', path: '/centro-cirurgico' },
      { label: 'Honorários', path: '/faturamento-tiss/guias-funi/honorarios' },
    ],
  },
  {
    id: 'internacao',
    slug: 'internacao',
    label: 'Guias de Internação',
    description: 'Solicitação, eletiva, urgência, prorrogação, alta e transferência',
    guideTypes: [3, 4, 6, 9],
    quickLinks: [
      { label: 'Internação', path: '/internacao' },
      { label: 'Solicitação de internação', path: '/faturamento-tiss/guias-funi/solicitacao-internacao' },
      { label: 'Resumo de internação', path: '/faturamento-tiss/guias-funi/resumo-internacao' },
    ],
  },
  {
    id: 'tiss',
    slug: 'tiss',
    label: 'Guias TISS',
    description: 'Padrão ANS: SP/SADT, consulta, internação, honorários e anexos',
    quickLinks: [
      { label: 'Faturamento TISS', path: '/faturamento-tiss' },
      { label: 'Catálogo FUNI', path: '/faturamento-tiss/guias-funi' },
      { label: 'Lotes', path: '/faturamento-tiss/lotes' },
    ],
  },
  {
    id: 'sus',
    slug: 'sus',
    label: 'Guias SUS',
    description: 'APAC, BPA, AIH, laudos e autorizações do SUS',
    quickLinks: [
      { label: 'Nova BPA', path: '/guias/sus', susGuideType: 1, description: 'Boletim de Produção Ambulatorial' },
      { label: 'Nova APAC', path: '/guias/sus', susGuideType: 2, description: 'Alta complexidade ambulatorial' },
      { label: 'Nova AIH', path: '/guias/sus', susGuideType: 3, description: 'Autorização de internação hospitalar' },
      { label: 'Painel SUS', path: '/faturamento' },
      { label: 'Integrações SIH/SIA', path: '/integracoes-gov/sih' },
    ],
  },
  {
    id: 'autorizacoes',
    slug: 'autorizacoes',
    label: 'Autorizações',
    description: 'Solicitação, aprovação, pendências, reenvio e histórico',
    statusFilter: [1, 2],
    quickLinks: [
      { label: 'Autorizações convênio', path: '/faturamento-tiss/autorizacoes' },
      { label: 'Regulação', path: '/regulacao/autorizacoes' },
    ],
  },
  {
    id: 'faturamento',
    slug: 'faturamento',
    label: 'Faturamento de Guias',
    description: 'Emitidas, autorizadas, faturadas, glosadas e repasses',
    statusFilter: [2, 3, 4],
    quickLinks: [
      { label: 'Fechamento TISS', path: '/faturamento-tiss/fechamento' },
      { label: 'Glosas', path: '/faturamento-tiss/glosas' },
      { label: 'Financeiro', path: '/financeiro' },
    ],
  },
  {
    id: 'auditoria',
    slug: 'auditoria',
    label: 'Auditoria',
    description: 'Histórico de alterações, usuário responsável e logs de movimentação',
    quickLinks: [
      { label: 'Auditoria do sistema', path: '/auditoria' },
      { label: 'Segurança e LGPD', path: '/seguranca-lgpd/auditoria' },
    ],
  },
];

const GROUP_BY_SLUG = Object.fromEntries(
  GUIDE_FUNCTIONAL_GROUPS.map((g) => [g.slug, g]),
) as Record<string, GuideFunctionalGroup>;

export function getGuideGroupBySlug(slug: string | undefined): GuideFunctionalGroup | undefined {
  if (!slug) return undefined;
  return GROUP_BY_SLUG[slug];
}

export function getGuideGroupById(id: string | null | undefined): GuideFunctionalGroup | undefined {
  if (!id) return undefined;
  return GUIDE_FUNCTIONAL_GROUPS.find((g) => g.id === id);
}

export const GUIDE_STATUS_OPTIONS = [
  { value: '', label: 'Todos' },
  { value: '1', label: 'Rascunho' },
  { value: '2', label: 'Enviada' },
  { value: '3', label: 'Paga' },
  { value: '4', label: 'Glosa' },
  { value: '5', label: 'Cancelada' },
] as const;

export const SERVICE_UNIT_OPTIONS = [
  { value: '', label: 'Todas' },
  { value: 'Unidade Principal', label: 'Unidade Principal' },
] as const;

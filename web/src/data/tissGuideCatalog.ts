import type { TissGuideTypeCatalogDto } from '../api/client';

export const tissGuideCategoryLabels: Record<string, string> = {
  billing: 'Cobrança',
  authorization: 'Autorização',
  annex: 'Anexo',
  administrative: 'Administrativo',
  dental: 'Odontologia',
};

/** Catálogo local espelhando o padrão ANS — 19 tipos de guias/documentos TISS. */
export const tissGuideCatalogFallback: TissGuideTypeCatalogDto[] = [
  {
    code: 1,
    slug: 'consulta',
    name: 'Guia de Consulta',
    shortName: 'Consulta',
    category: 'billing',
    categoryLabel: 'Cobrança',
    description: 'Formulário mais simples do padrão TISS.',
    whenToUse: 'Consultas eletivas em consultório, ambulatório ou clínica — sem internação.',
    isCreatable: true,
    isImplemented: true,
    linkedTab: 'guides',
    ansManualUrl: 'https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss',
  },
  {
    code: 2,
    slug: 'sp-sadt',
    name: 'Guia SP/SADT',
    shortName: 'SP/SADT',
    category: 'billing',
    categoryLabel: 'Cobrança',
    description: 'Serviços Profissionais e Serviços Auxiliares de Diagnóstico e Terapia.',
    whenToUse: 'Exames, terapias, procedimentos ambulatoriais, autorizações e faturamento SP/SADT.',
    isCreatable: true,
    isImplemented: true,
    linkedTab: 'guides',
    ansManualUrl: 'https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss',
  },
  {
    code: 6,
    slug: 'solicitacao-internacao',
    name: 'Guia de Solicitação de Internação',
    shortName: 'Solic. internação',
    category: 'authorization',
    categoryLabel: 'Autorização',
    description: 'Pedido de autorização para internação hospitalar.',
    whenToUse: 'Quando há necessidade de internar o paciente e a operadora exige autorização prévia.',
    isCreatable: true,
    isImplemented: true,
    linkedTab: 'authorizations',
    ansManualUrl: 'https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss',
  },
  {
    code: 4,
    slug: 'resumo-internacao',
    name: 'Guia de Resumo de Internação',
    shortName: 'Resumo internação',
    category: 'billing',
    categoryLabel: 'Cobrança',
    description: 'Faturamento após alta hospitalar.',
    whenToUse: 'Após a alta: procedimentos, diárias, taxas, materiais e medicamentos da internação.',
    isCreatable: true,
    isImplemented: true,
    linkedTab: 'guides',
    ansManualUrl: 'https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss',
  },
  {
    code: 5,
    slug: 'honorarios',
    name: 'Guia de Honorário Individual',
    shortName: 'Honorários',
    category: 'billing',
    categoryLabel: 'Cobrança',
    description: 'Honorários de profissionais específicos da equipe.',
    whenToUse: 'Profissionais que atuaram em internação, centro cirúrgico ou equipe multiprofissional.',
    isCreatable: true,
    isImplemented: true,
    linkedTab: 'guides',
    ansManualUrl: 'https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss',
  },
  {
    code: 12,
    slug: 'gto',
    name: 'Guia de Tratamento Odontológico (GTO)',
    shortName: 'GTO',
    category: 'dental',
    categoryLabel: 'Odontologia',
    description: 'Faturamento odontológico conforme rol ANS.',
    whenToUse: 'Atendimentos e procedimentos em consultório odontológico.',
    isCreatable: true,
    isImplemented: true,
    linkedTab: 'guides',
    ansManualUrl: 'https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss',
  },
];

export function getGuideCatalogEntry(code: number, catalog: TissGuideTypeCatalogDto[]): TissGuideTypeCatalogDto | undefined {
  const fromCatalog = catalog.find((g) => g.code === code);
  if (fromCatalog) return fromCatalog;
  return tissGuideCatalogFallback.find((g) => g.code === code);
}

/** Grupos funcionais do hub Financeiro (scaffold). */

export type FinancialFunctionalGroup = {
  id: string;
  slug: string;
  label: string;
  description: string;
};

export const FINANCIAL_FUNCTIONAL_GROUPS: FinancialFunctionalGroup[] = [
  { id: 'receber', slug: 'receber', label: 'Contas a receber', description: 'Convênios, SUS e particular' },
  { id: 'pagar', slug: 'pagar', label: 'Contas a pagar', description: 'Fornecedores, despesas e impostos' },
  { id: 'tesouraria', slug: 'tesouraria', label: 'Tesouraria', description: 'Caixa, bancos e conciliação' },
  { id: 'fiscal', slug: 'fiscal', label: 'Fiscal', description: 'Notas fiscais e obrigações' },
  { id: 'cobrancas', slug: 'cobrancas', label: 'Cobranças', description: 'Inadimplência e negociação' },
  { id: 'recibos-diversos', slug: 'recibos-diversos', label: 'Recibos Diversos', description: 'Comprovantes e recibos avulsos' },
  { id: 'boletos', slug: 'boletos', label: 'Boletos', description: 'Emissão e retorno bancário' },
];

const GROUP_BY_SLUG = Object.fromEntries(
  FINANCIAL_FUNCTIONAL_GROUPS.map((g) => [g.slug, g]),
) as Record<string, FinancialFunctionalGroup>;

export function getFinancialGroupBySlug(slug: string | undefined): FinancialFunctionalGroup | undefined {
  if (!slug) return undefined;
  return GROUP_BY_SLUG[slug];
}

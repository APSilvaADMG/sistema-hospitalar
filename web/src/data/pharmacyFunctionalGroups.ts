/** Grupos funcionais do hub de Farmácia / Estoque (scaffold). */

export type PharmacyFunctionalGroup = {
  id: string;
  slug: string;
  label: string;
  description: string;
};

export const PHARMACY_FUNCTIONAL_GROUPS: PharmacyFunctionalGroup[] = [
  { id: 'dispensacao', slug: 'dispensacao', label: 'Dispensação', description: 'Medicamentos dispensados por prescrição' },
  { id: 'solicitacoes', slug: 'solicitacoes', label: 'Solicitações', description: 'Pedidos de enfermagem e setores' },
  { id: 'estoque', slug: 'estoque', label: 'Estoque', description: 'Saldo, mínimos e movimentações' },
  { id: 'lotes', slug: 'lotes', label: 'Lotes', description: 'Rastreabilidade e validades' },
  { id: 'validades', slug: 'validades', label: 'Validades', description: 'Produtos próximos do vencimento' },
  { id: 'inventario', slug: 'inventario', label: 'Inventário', description: 'Contagens e ajustes' },
  { id: 'transferencias', slug: 'transferencias', label: 'Transferências', description: 'Entre farmácia central e alas' },
  { id: 'devolucoes', slug: 'devolucoes', label: 'Devoluções', description: 'Estornos e devoluções de paciente' },
];

const GROUP_BY_SLUG = Object.fromEntries(
  PHARMACY_FUNCTIONAL_GROUPS.map((g) => [g.slug, g]),
) as Record<string, PharmacyFunctionalGroup>;

export function getPharmacyGroupBySlug(slug: string | undefined): PharmacyFunctionalGroup | undefined {
  if (!slug) return undefined;
  return GROUP_BY_SLUG[slug];
}

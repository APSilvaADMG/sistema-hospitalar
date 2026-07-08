import { GUIDE_FUNCTIONAL_GROUPS } from '../data/guideFunctionalGroups';
import { FUNI_GUIDE_BASE, FUNI_GUIDE_CATALOG } from '../data/funiGuides/catalog';

export type GuiasMenuLeaf = {
  id: string;
  label: string;
  path: string;
  end?: boolean;
};

export type GuiasMenuGroup = {
  id: string;
  label: string;
  items: GuiasMenuLeaf[];
};

const HUB_GROUPS = GUIDE_FUNCTIONAL_GROUPS.filter((g) =>
  ['consultas', 'exames', 'procedimentos', 'internacao'].includes(g.slug),
);

const FUNI_LEAVES: GuiasMenuLeaf[] = FUNI_GUIDE_CATALOG.map((guide) => ({
  id: `funi-${guide.slug}`,
  label: `${guide.funiCode} — ${guide.title}`,
  path: `${FUNI_GUIDE_BASE}/${guide.slug}`,
}));

/** Grupos do menu Guias — hub, TISS, FUNI, SUS e fluxos relacionados. */
export const GUIAS_MENU_GROUPS: GuiasMenuGroup[] = [
  {
    id: 'gestao',
    label: 'Gestão',
    items: [
      { id: 'guias-hub', label: 'Gestão de Guias', path: '/guias', end: true },
      ...HUB_GROUPS.map((g) => ({
        id: `guias-${g.slug}`,
        label: g.label,
        path: `/guias/${g.slug}`,
      })),
    ],
  },
  {
    id: 'tiss',
    label: 'Convênios TISS',
    items: [
      { id: 'fat-tiss', label: 'Faturamento TISS', path: '/faturamento-tiss', end: true },
      { id: 'fat-funi-hub', label: 'Catálogo FUNI', path: '/faturamento-tiss/guias-funi', end: true },
      { id: 'guias-tiss', label: 'Grupo TISS no hub', path: '/guias/tiss' },
    ],
  },
  {
    id: 'funi',
    label: 'Formulários FUNI',
    items: FUNI_LEAVES,
  },
  {
    id: 'sus',
    label: 'SUS',
    items: [
      { id: 'guias-sus', label: 'Guias SUS', path: '/guias/sus' },
      { id: 'fat-aih', label: 'AIH (SUS)', path: '/faturamento/sus/aih' },
      { id: 'fat-apac', label: 'APAC (SUS)', path: '/faturamento/sus/apac' },
      { id: 'fat-bpa', label: 'BPA (SUS)', path: '/faturamento/sus/bpa' },
      { id: 'fat-prod-amb', label: 'Produção Ambulatorial', path: '/faturamento/sus/producao-ambulatorial' },
    ],
  },
  {
    id: 'fluxos',
    label: 'Autorizações e faturamento',
    items: [
      { id: 'guias-aut', label: 'Autorizações', path: '/guias/autorizacoes' },
      { id: 'guias-fat', label: 'Faturamento de Guias', path: '/guias/faturamento' },
      { id: 'guias-aud', label: 'Auditoria', path: '/guias/auditoria' },
      { id: 'tiss-aut', label: 'Autorizações TISS', path: '/faturamento-tiss/autorizacoes' },
      { id: 'tiss-lotes', label: 'Lotes TISS', path: '/faturamento-tiss/lotes' },
    ],
  },
];

export function flattenGuiasMenuLeaves(): GuiasMenuLeaf[] {
  return GUIAS_MENU_GROUPS.flatMap((group) => group.items);
}

export function buildFeegowGuiasChildren(): { label: string; path: string }[] {
  return flattenGuiasMenuLeaves().map(({ label, path }) => ({ label, path }));
}

export function buildFeegowGuiasSideItems(): {
  id: string;
  label: string;
  path?: string;
  children?: { label: string; path: string }[];
}[] {
  return GUIAS_MENU_GROUPS.map((group) => {
    if (group.items.length === 1) {
      const item = group.items[0];
      return { id: group.id, label: item.label, path: item.path };
    }
    return {
      id: group.id,
      label: group.label,
      children: group.items.map((item) => ({ label: item.label, path: item.path })),
    };
  });
}

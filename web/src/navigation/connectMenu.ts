import { connectTabs } from './moduleSections';

export const CONNECT_BASE = '/connect';

export type ConnectMenuLeaf = {
  id: string;
  label: string;
  path: string;
  end?: boolean;
};

export type ConnectMenuGroup = {
  id: string;
  label: string;
  items: ConnectMenuLeaf[];
};

function connectPath(slug: string): string {
  return slug ? `${CONNECT_BASE}/${slug}` : CONNECT_BASE;
}

function tabLeaves(slugs: string[]): ConnectMenuLeaf[] {
  return connectTabs
    .filter((t) => slugs.includes(t.slug))
    .map((t) => ({
      id: `conn-${t.slug || 'inbox'}`,
      label: t.label,
      path: connectPath(t.slug),
      end: !t.slug,
    }));
}

/** Grupos do menu Comunicação — espelha as abas do APSMed Connect. */
export const CONNECT_MENU_GROUPS: ConnectMenuGroup[] = [
  {
    id: 'correio',
    label: 'Correio',
    items: tabLeaves(['', 'enviadas', 'rascunhos']),
  },
  {
    id: 'colaboracao',
    label: 'Colaboração',
    items: [
      ...tabLeaves(['chat', 'notificacoes', 'mural']),
      { id: 'conn-tv', label: 'TV Corporativa', path: '/connect/tv-corporativa', end: true },
    ],
  },
  {
    id: 'fluxos',
    label: 'Fluxos',
    items: tabLeaves(['chamados', 'tarefas', 'aprovacoes', 'agenda', 'assistente']),
  },
  {
    id: 'canais',
    label: 'Canais',
    items: [
      { id: 'conn-whatsapp', label: 'WhatsApp Connect', path: '/connect/whatsapp', end: true },
    ],
  },
];

export function flattenConnectMenuLeaves(): ConnectMenuLeaf[] {
  return CONNECT_MENU_GROUPS.flatMap((group) => group.items);
}

export function buildFeegowConnectChildren(): { label: string; path: string }[] {
  return flattenConnectMenuLeaves().map(({ label, path }) => ({ label, path }));
}

export function buildFeegowConnectSideItems(): {
  id: string;
  label: string;
  path?: string;
  children?: { label: string; path: string }[];
}[] {
  return CONNECT_MENU_GROUPS.map((group) => {
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

import type { ModuleVisibility } from '../config/moduleVisibility';
import { isPathModuleEnabled } from '../config/moduleVisibility';
import { defaultModuleVisibility } from '../config/clinicOnDoctorProfile';
import type { MenuBranch, MenuLeaf, MenuMegaGroupId, MenuNode, MenuRoles, MenuSection } from './types';
import { flattenMenuLeaves, isMenuBranch } from './types';
import { MENU_MEGA_GROUPS, SECTION_PERMISSIONS } from './menuMegaGroups';
import { CONNECT_MENU_GROUPS } from './connectMenu';
import { GUIAS_MENU_GROUPS } from './guiasMenu';

function leaf(
  id: string,
  label: string,
  path: string,
  roles?: MenuRoles[],
  end?: boolean,
  badge?: number,
  permissions?: string[],
): MenuLeaf {
  return { id, label, path, roles, end, badge, permissions };
}

function buildGuiasMenuNodes(): MenuNode[] {
  return GUIAS_MENU_GROUPS.map((group) => {
    const children = group.items.map((item) =>
      leaf(item.id, item.label, item.path, undefined, item.end),
    );
    if (children.length === 1) return children[0];
    return { id: group.id, label: group.label, children } satisfies MenuBranch;
  });
}

function buildConnectMenuNodes(): MenuNode[] {
  return CONNECT_MENU_GROUPS.map((group) => {
    const children = group.items.map((item) =>
      leaf(item.id, item.label, item.path, undefined, item.end, undefined, ['connect.read']),
    );
    if (children.length === 1) return children[0];
    return { id: group.id, label: group.label, children } satisfies MenuBranch;
  });
}

export type MenuBuildOptions = {
  isStaff: boolean;
  isAdminOrReception: boolean;
  isAdmin: boolean;
  hasSecurityLgpd?: boolean;
  unreadCount: number;
  hasPermission?: (...permissions: string[]) => boolean;
  modules?: ModuleVisibility;
};

/**
 * Menu enxuto: uma entrada por módulo operacional.
 * Subtelas ficam nas abas internas (ModuleNav) — evita duplicata e links “Em desenvolvimento”.
 */
export function buildMenuSections(options: MenuBuildOptions): MenuSection[] {
  const {
    isStaff,
    isAdminOrReception,
    isAdmin,
    hasSecurityLgpd = false,
    unreadCount,
    hasPermission = () => true,
    modules = defaultModuleVisibility,
  } = options;

  const sections: MenuSection[] = [
    {
      id: 'dashboard',
      megaGroup: 'management',
      title: 'Painel',
      nodes: [
        leaf('dash-visao', 'Visão Geral', '/', undefined, true),
        leaf('dash-command', 'Centro de Comando', '/dashboard/command-center'),
        leaf('dash-assist', 'Painel Assistencial', '/dashboard/assistencial'),
        leaf('dash-pend', 'Centro de Pendências', '/pendencias'),
      ],
    },
    {
      id: 'gestao',
      megaGroup: 'management',
      title: 'Gestão',
      roles: ['adminOrReception'],
      nodes: [
        leaf('rel-central', 'Relatórios', '/relatorios'),
        leaf('dash-indicadores', 'Indicadores (BI)', '/bi', ['adminOrReception']),
        leaf('qual-hub', 'Qualidade', '/qualidade'),
        leaf('qual-aud', 'Auditorias', '/auditoria'),
      ],
    },

    // ─── Jornada do paciente (1 entrada = 1 módulo + abas internas) ───
    {
      id: 'entrada',
      megaGroup: 'clinical',
      title: '1 · Entrada e Recepção',
      nodes: [
        leaf('rec-central', 'Central de Recepção', '/recepcao', ['adminOrReception']),
        leaf('acf-portaria', 'Portaria e Visitantes', '/seguranca'),
        leaf('reg-hub', 'Regulação e Leitos', '/regulacao', ['adminOrReception']),
      ],
    },
    {
      id: 'ambulatorio',
      megaGroup: 'clinical',
      title: '2 · Ambulatório',
      nodes: [
        leaf('amb-central', 'Central Ambulatorial', '/ambulatorio'),
      ],
    },
    {
      id: 'pronto-atendimento',
      megaGroup: 'clinical',
      title: '3 · Pronto Atendimento',
      nodes: [
        leaf('ps-hub', 'Pronto Socorro', '/emergencia'),
      ],
    },
    {
      id: 'internacao',
      megaGroup: 'clinical',
      title: '4 · Internação e UTI',
      nodes: [
        leaf('int-hub', 'Internação', '/internacao'),
        leaf('int-aih', 'AIH (SUS)', '/faturamento/sus/aih'),
        leaf('uti-hub', 'UTI', '/uti'),
      ],
    },
    {
      id: 'cirurgia',
      megaGroup: 'clinical',
      title: '5 · Centro Cirúrgico',
      nodes: [
        leaf('cc-hub', 'Centro Cirúrgico', '/centro-cirurgico'),
        leaf('cc-cme', 'CME — Esterilização', '/cme'),
      ],
    },
    {
      id: 'pep',
      megaGroup: 'clinical',
      title: '6 · Prontuário (PEP)',
      roles: ['staff'],
      permissions: ['pep.read'],
      nodes: [
        leaf('pep-hub', 'Prontuário Eletrônico', '/pep'),
      ],
    },
    {
      id: 'enfermagem',
      megaGroup: 'clinical',
      title: '7 · Enfermagem',
      roles: ['staff'],
      nodes: [
        leaf('enf-hub', 'Enfermagem e SAE', '/enfermagem'),
      ],
    },
    {
      id: 'ccih',
      megaGroup: 'clinical',
      title: '8 · CCIH',
      roles: ['staff'],
      nodes: [
        leaf('ccih-hub', 'Controle de Infecção', '/ccih'),
      ],
    },

    // ─── Apoio ───
    {
      id: 'laboratorio',
      megaGroup: 'diagnostic',
      title: 'Laboratório',
      nodes: [
        leaf('lab-hub', 'Laboratório', '/laboratorio'),
      ],
    },
    {
      id: 'imagem',
      megaGroup: 'diagnostic',
      title: 'Diagnóstico por Imagem',
      nodes: [
        leaf('img-hub', 'Imagem e PACS', '/imagem'),
      ],
    },
    {
      id: 'farmacia',
      megaGroup: 'diagnostic',
      title: 'Farmácia',
      nodes: [
        leaf('far-hub', 'Farmácia', '/farmacia'),
      ],
    },
    {
      id: 'sangue',
      megaGroup: 'diagnostic',
      title: 'Hemoterapia',
      nodes: [
        leaf('bs-hub', 'Banco de Sangue', '/hemoterapia'),
      ],
    },
    {
      id: 'nutricao',
      megaGroup: 'diagnostic',
      title: 'Nutrição',
      nodes: [leaf('nut-hub', 'Nutrição', '/nutricao')],
    },
    {
      id: 'oncologia',
      megaGroup: 'diagnostic',
      title: 'Oncologia',
      nodes: [
        leaf('onc-hub', 'Oncologia', '/oncologia'),
      ],
    },
    {
      id: 'operacional',
      megaGroup: 'diagnostic',
      title: 'Logística Interna',
      roles: ['adminOrReception', 'staff'],
      nodes: [
        leaf('op-tr-hub', 'Transportes', '/transportes'),
        leaf('op-h-hub', 'Hotelaria Hospitalar', '/hotelaria'),
        leaf('op-dialise', 'Diálise', '/dialise'),
        leaf('op-fisio', 'Fisioterapia', '/fisioterapia'),
        leaf('op-lav', 'Lavanderia', '/lavanderia'),
        leaf('op-res', 'Resíduos hospitalares', '/residuos'),
        leaf('op-amb', 'Ambulâncias', '/ambulancias'),
      ],
    },

    // ─── Fechamento ───
    {
      id: 'fechamento',
      megaGroup: 'administrative',
      title: '9 · Faturamento',
      permissions: ['billing.read', 'billing.write'],
      nodes: [
        leaf('fat-dash', 'Painel de Faturamento', '/faturamento', undefined, true),
        leaf('fat-tiss', 'Faturamento TISS', '/faturamento-tiss'),
        leaf('fat-tiss-consulta', 'Nova guia — Consulta', '/faturamento-tiss/inserir/consulta'),
        leaf('fat-tiss-lotes', 'Lotes TISS', '/faturamento-tiss/lotes'),
        leaf('fat-tiss-fech', 'Fechar lote', '/faturamento-tiss/fechamento'),
        leaf('fat-tiss-glosas', 'Glosas TISS', '/faturamento-tiss/glosas'),
        leaf('fat-tiss-recursos', 'Recursos de glosa', '/faturamento-tiss/recursos-glosa'),
        leaf('fat-tiss-aut', 'Autorizações TISS', '/faturamento-tiss/autorizacoes'),
        leaf('fat-sus-aih', 'AIH (SUS)', '/faturamento/sus/aih'),
        leaf('fat-sus-apac', 'APAC (SUS)', '/faturamento/sus/apac'),
        leaf('fat-sus-bpa', 'BPA (SUS)', '/faturamento/sus/bpa'),
        leaf('fat-tpa', 'TPA', '/convenios/tpa'),
        leaf('fat-convenios', 'Convênios', '/convenios'),
      ],
    },
    {
      id: 'saida',
      megaGroup: 'administrative',
      title: '10 · Saída e Portaria',
      roles: ['adminOrReception'],
      permissions: ['billing.read', 'billing.write'],
      nodes: [
        leaf('saida-hub', 'Liberação e Acesso Físico', '/acesso-fisico'),
        leaf('saida-agenda', 'Agendamentos e check-in', '/recepcao/check-in'),
      ],
    },
    {
      id: 'financeiro',
      megaGroup: 'administrative',
      title: 'Financeiro',
      roles: ['adminOrReception'],
      nodes: [leaf('fin-hub', 'Financeiro', '/financeiro')],
    },
    {
      id: 'suprimentos',
      megaGroup: 'administrative',
      title: 'Suprimentos',
      roles: ['adminOrReception'],
      nodes: [
        leaf('alm-hub', 'Almoxarifado', '/estoque'),
        leaf('alm-kits', 'Kits de Produtos', '/estoque/kits'),
        leaf('alm-med-conv', 'Medicamento por Convênio', '/estoque/config/medicamento-convenio'),
        leaf('comp-hub', 'Compras', '/compras'),
      ],
    },
    {
      id: 'rh',
      megaGroup: 'administrative',
      title: 'Pessoas',
      roles: ['adminOrReception'],
      nodes: [
        leaf('rh-hub', 'Recursos Humanos', '/rh'),
        leaf('rh-folha', 'Folha de pagamento', '/rh/folha'),
        leaf('rh-escalas', 'Escalas', '/rh/escalas'),
        leaf('rh-plantoes', 'Plantões', '/rh/plantoes'),
        leaf('rh-ferias', 'Férias', '/rh/ferias'),
        leaf('rh-treinamentos', 'Treinamentos', '/rh/treinamentos'),
        leaf('rh-avaliacoes', 'Avaliações', '/rh/avaliacoes'),
        leaf('rh-prof', 'Profissionais de Saúde', '/profissionais'),
        ...(isAdmin ? [leaf('rh-users', 'Usuários do Sistema', '/usuarios', ['admin'])] : []),
      ],
    },
    {
      id: 'eng-clinica',
      megaGroup: 'administrative',
      title: 'Engenharia Clínica',
      roles: ['adminOrReception'],
      nodes: [leaf('ec-hub', 'Equipamentos e Manutenção', '/engenharia-clinica')],
    },

    // ─── Sistema ───
    {
      id: 'seguranca-lgpd',
      megaGroup: 'security',
      title: 'LGPD e Segurança',
      roles: ['securityLgpd'],
      nodes: [leaf('seg-hub', 'Segurança e LGPD', '/seguranca-lgpd')],
    },
    {
      id: 'seguranca-lgpd-links',
      megaGroup: 'security',
      title: 'LGPD — Atalhos',
      roles: ['securityLgpd'],
      nodes: [
        leaf('seg-consent', 'Consentimentos LGPD', '/seguranca-lgpd/consentimentos'),
        leaf('seg-pac-consent', 'Coletar assinatura', '/recepcao/pacientes/consentimentos'),
        leaf('seg-titular', 'Direitos do titular', '/seguranca-lgpd/titular'),
      ],
    },
    {
      id: 'integracoes-gov',
      megaGroup: 'security',
      title: 'Integrações SUS',
      roles: ['adminOrReception'],
      nodes: [leaf('gov-hub', 'Painel Governamental', '/integracoes-gov')],
    },
    {
      id: 'integracoes',
      megaGroup: 'security',
      title: 'Integrações Clínicas',
      roles: ['staff'],
      nodes: [
        leaf('int-hub', 'Integrações', '/integracoes'),
        ...(isStaff ? [leaf('int-ia', 'IA Assistencial', '/ia')] : []),
        leaf('int-tele', 'Telemedicina', '/telemedicina', ['staff']),
      ],
    },
    {
      id: 'automacao',
      megaGroup: 'security',
      title: 'Automação',
      nodes: [leaf('auto-hub', 'Automação', '/automacao')],
    },
    {
      id: 'guias',
      megaGroup: 'security',
      title: 'Guias',
      permissions: ['billing.read'],
      nodes: buildGuiasMenuNodes(),
    },
    {
      id: 'comunicacao',
      megaGroup: 'security',
      title: 'Comunicação',
      permissions: ['connect.read'],
      nodes: buildConnectMenuNodes(),
    },
    {
      id: 'configuracoes',
      megaGroup: 'security',
      title: 'Configurações',
      roles: ['admin'],
      nodes: [leaf('cfg-hub', 'Parâmetros e Cadastros', '/configuracoes')],
    },
  ];

  return sections
    .map((section) => ({
      ...section,
      permissions: section.permissions ?? SECTION_PERMISSIONS[section.id],
    }))
    .filter((section) => filterByRoles(section.roles, isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd))
    .filter((section) => filterByPermissions(section.permissions, hasPermission))
    .map((section) => ({
      ...section,
      nodes: filterNodes(section.nodes, isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd, unreadCount, hasPermission, modules),
    }))
    .filter((section) => section.nodes.length > 0);
}

export function groupSectionsByMegaGroup(sections: MenuSection[]) {
  const grouped = new Map<MenuMegaGroupId, MenuSection[]>();
  for (const meta of MENU_MEGA_GROUPS) {
    grouped.set(meta.id, []);
  }
  for (const section of sections) {
    grouped.get(section.megaGroup)?.push(section);
  }
  return MENU_MEGA_GROUPS
    .map((meta) => ({ meta, sections: grouped.get(meta.id) ?? [] }))
    .filter((g) => g.sections.length > 0);
}

function filterByPermissions(
  permissions: string[] | undefined,
  hasPermission: (...permissions: string[]) => boolean,
): boolean {
  if (!permissions?.length) return true;
  return hasPermission(...permissions);
}

function filterByRoles(
  roles: MenuRoles[] | undefined,
  isStaff: boolean,
  isAdminOrReception: boolean,
  isAdmin: boolean,
  hasSecurityLgpd: boolean,
): boolean {
  if (!roles?.length) return true;
  return roles.some((role) => {
    if (role === 'staff') return isStaff;
    if (role === 'adminOrReception') return isAdminOrReception;
    if (role === 'admin') return isAdmin;
    if (role === 'securityLgpd') return hasSecurityLgpd;
    return true;
  });
}

function filterNodes(
  nodes: MenuNode[],
  isStaff: boolean,
  isAdminOrReception: boolean,
  isAdmin: boolean,
  hasSecurityLgpd: boolean,
  unreadCount: number,
  hasPermission: (...permissions: string[]) => boolean,
  modules: ModuleVisibility,
): MenuNode[] {
  return nodes
    .map((node) => {
      if (isMenuBranch(node)) {
        const children = filterNodes(
          node.children, isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd, unreadCount, hasPermission, modules,
        );
        if (!children.length) return null;
        if (!filterByRoles(node.roles, isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd)) return null;
        return { ...node, children };
      }

      if (!filterByRoles(node.roles, isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd)) return null;
      if (!filterByPermissions(node.permissions, hasPermission)) return null;
      if (!isPathModuleEnabled(node.path, modules)) return null;

      if (node.id === 'dash-alertas' && unreadCount > 0) {
        return { ...node, badge: unreadCount };
      }
      return node;
    })
    .filter((node): node is MenuNode => node !== null);
}

export function getAllMenuPaths(sections: MenuSection[]): string[] {
  const paths = new Set<string>();
  for (const section of sections) {
    for (const leafNode of flattenMenuLeaves(section.nodes)) {
      paths.add(leafNode.path);
    }
  }
  return [...paths];
}

export function findMenuBreadcrumb(pathname: string): {
  title: string;
  section: string | null;
  parents: string[];
} {
  const sections = buildMenuSections({
    isStaff: true,
    isAdminOrReception: true,
    isAdmin: true,
    hasSecurityLgpd: true,
    unreadCount: 0,
  });

  for (const section of sections) {
    const trail = findInNodes(section.nodes, pathname, []);
    if (trail) {
      return {
        title: trail.leaf.label,
        section: section.title,
        parents: [...trail.parents, trail.leaf.label],
      };
    }
  }

  return {
    title: 'Módulo',
    section: null,
    parents: [],
  };
}

function findInNodes(
  nodes: MenuNode[],
  pathname: string,
  parents: string[],
): { leaf: MenuLeaf; parents: string[] } | null {
  for (const node of nodes) {
    if (isMenuBranch(node)) {
      const found = findInNodes(node.children, pathname, [...parents, node.label]);
      if (found) return found;
    } else if (isPathMatch(pathname, node.path, node.end)) {
      return { leaf: node, parents };
    }
  }
  return null;
}

function isPathMatch(pathname: string, to: string, end?: boolean) {
  if (end || to === '/') return pathname === to;
  return pathname === to || pathname.startsWith(`${to}/`);
}

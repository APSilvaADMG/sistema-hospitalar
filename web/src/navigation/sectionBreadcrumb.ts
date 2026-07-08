import type { ModuleTab } from './useModuleSection';
import {
  agendaTabs,
  aiTabs,
  appointmentTabs,
  automacaoTabs,
  biTabs,
  ccihTabs,
  clinicalEngTabs,
  cmeTabs,
  configTabs,
  connectTabs,
  connectWhatsAppTabs,
  consultingRoomsTabs,
  emergencyTabs,
  financialTabs,
  govIntegrationTabs,
  hemotherapyTabs,
  hospitalizationTabs,
  hotelariaTabs,
  hrTabs,
  icuTabs,
  imagingTabs,
  integrationTabs,
  inventoryTabs,
  labTabs,
  nursingTabs,
  nutritionTabs,
  patientTabs,
  pepTabs,
  pharmacyTabs,
  physicalAccessTabs,
  purchasingTabs,
  qualityTabs,
  regulationTabs,
  reportsTabs,
  securityLgpdTabs,
  securityPortariaTabs,
  surgeryTabs,
  susBillingTabs,
  tissTabs,
  guidesTabs,
  transportTabs,
} from './moduleSections';
import { findMenuBreadcrumb } from './sidebarMenu';

type SectionRegistry = { basePath: string; tabs: ModuleTab[] };

const SECTION_REGISTRY: SectionRegistry[] = [
  { basePath: '/emergencia', tabs: emergencyTabs },
  { basePath: '/internacao', tabs: hospitalizationTabs },
  { basePath: '/centro-cirurgico', tabs: surgeryTabs },
  { basePath: '/uti', tabs: icuTabs },
  { basePath: '/agendamentos', tabs: appointmentTabs },
  { basePath: '/pacientes', tabs: patientTabs },
  { basePath: '/configuracoes', tabs: configTabs },
  { basePath: '/automacao', tabs: automacaoTabs },
  { basePath: '/pep', tabs: pepTabs },
  { basePath: '/agenda', tabs: agendaTabs },
  { basePath: '/enfermagem', tabs: nursingTabs },
  { basePath: '/farmacia', tabs: pharmacyTabs },
  { basePath: '/estoque', tabs: inventoryTabs },
  { basePath: '/laboratorio', tabs: labTabs },
  { basePath: '/imagem', tabs: imagingTabs },
  { basePath: '/hemoterapia', tabs: hemotherapyTabs },
  { basePath: '/nutricao', tabs: nutritionTabs },
  { basePath: '/faturamento', tabs: susBillingTabs },
  { basePath: '/financeiro', tabs: financialTabs },
  { basePath: '/faturamento-tiss', tabs: tissTabs },
  { basePath: '/guias', tabs: guidesTabs },
  { basePath: '/compras', tabs: purchasingTabs },
  { basePath: '/rh', tabs: hrTabs },
  { basePath: '/ccih', tabs: ccihTabs },
  { basePath: '/seguranca-lgpd', tabs: securityLgpdTabs },
  { basePath: '/qualidade', tabs: qualityTabs },
  { basePath: '/regulacao', tabs: regulationTabs },
  { basePath: '/engenharia-clinica', tabs: clinicalEngTabs },
  { basePath: '/bi', tabs: biTabs },
  { basePath: '/hotelaria', tabs: hotelariaTabs },
  { basePath: '/transportes', tabs: transportTabs },
  { basePath: '/acesso-fisico', tabs: physicalAccessTabs },
  { basePath: '/integracoes-gov', tabs: govIntegrationTabs },
  { basePath: '/relatorios', tabs: reportsTabs },
  { basePath: '/connect', tabs: connectTabs },
  { basePath: '/connect/whatsapp', tabs: connectWhatsAppTabs },
  { basePath: '/consultorios', tabs: consultingRoomsTabs },
  { basePath: '/cme', tabs: cmeTabs },
  { basePath: '/seguranca', tabs: securityPortariaTabs },
  { basePath: '/ia', tabs: aiTabs },
  { basePath: '/integracoes', tabs: integrationTabs },
];

function tabPath(basePath: string, tab: ModuleTab): string {
  if (tab.to) return tab.to;
  const base = basePath.replace(/\/$/, '');
  return tab.slug ? `${base}/${tab.slug}` : base;
}

function normalizePath(pathname: string): string {
  return pathname.replace(/\/$/, '') || '/';
}

/** Rótulo da sub-aba interna (ModuleNav), quando existir. */
export function findSectionTabLabel(pathname: string): string | null {
  const normalized = normalizePath(pathname);

  for (const { basePath, tabs } of SECTION_REGISTRY) {
    const base = basePath.replace(/\/$/, '');
    if (normalized !== base && !normalized.startsWith(`${base}/`)) continue;

    for (const tab of tabs) {
      const tabRoute = normalizePath(tabPath(basePath, tab));
      if (tabRoute === normalized) return tab.label;
    }
  }

  return null;
}

/** Título amigável: menu lateral ou aba interna do módulo. */
export function resolvePageTitle(pathname: string): string {
  const tabLabel = findSectionTabLabel(pathname);
  if (tabLabel) return tabLabel;

  const menu = findMenuBreadcrumb(pathname);
  if (menu.title !== 'Módulo') return menu.title;

  const segment = pathname.split('/').filter(Boolean).pop();
  if (!segment) return 'Visão Geral';
  return segment.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
}

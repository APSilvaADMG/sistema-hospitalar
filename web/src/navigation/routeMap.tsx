import { ModulePlaceholderPage } from '../pages/ModulePlaceholderPage';
import type { ComponentType } from 'react';
import { lazyPage } from './lazyPage';
import { AuditPage } from '../pages/AuditPage';
import { DashboardPage } from '../pages/DashboardPage';
import { EmergencyPage } from '../pages/EmergencyPage';
import { HealthPlansPage } from '../pages/HealthPlansPage';
import { HemotherapyPage } from '../pages/HemotherapyPage';
import { HospitalizationHubPage } from '../pages/HospitalizationHubPage';
import { HrPage } from '../pages/HrPage';
import { IcuPage } from '../pages/IcuPage';
import { ImagingPage } from '../pages/ImagingPage';
import { IntegrationsPage } from '../pages/IntegrationsPage';
import { InventoryPage } from '../pages/InventoryPage';
import { LaboratoryPage } from '../pages/LaboratoryPage';
import { NotificationsPage } from '../pages/NotificationsPage';
import { PendenciesPage } from '../pages/PendenciesPage';
import { NutritionPage } from '../pages/NutritionPage';
import { PharmacyHubPage } from '../pages/hubs/PharmacyHubPage';
import { AmbulancePage } from '../pages/AmbulancePage';
import { DialysisPage } from '../pages/DialysisPage';
import { LaundryPage } from '../pages/LaundryPage';
import { WasteManagementPage } from '../pages/WasteManagementPage';
import { PhysiotherapyPage } from '../pages/PhysiotherapyPage';
import { ProfessionalsPage } from '../pages/ProfessionalsPage';
import { PurchasingPage } from '../pages/PurchasingPage';
import { ReportsPage } from '../pages/ReportsPage';
import { GuidesHubPage } from '../pages/GuidesHubPage';
import { SurgeryPage } from '../pages/SurgeryPage';
import { TissPage } from '../pages/TissPage';
import { UsersPage } from '../pages/UsersPage';
import { AiPage } from '../pages/AiPage';
import { TelemedicinePage } from '../pages/TelemedicinePage';
import { ClinicalEngineeringPage } from '../pages/ClinicalEngineeringPage';
import { InfectionControlPage } from '../pages/InfectionControlPage';
import { MimicResearchPage } from '../pages/MimicResearchPage';
import { DashboardTasksPage } from '../pages/DashboardTasksPage';
import { DashboardAssistencialPage } from '../pages/DashboardAssistencialPage';
import { CommandCenterPage } from '../pages/CommandCenterPage';
import { BusinessRulesPage } from '../pages/BusinessRulesPage';
import { OfficialUpdatesPage } from '../pages/OfficialUpdatesPage';
import { PepHubPage } from '../pages/hubs/PepHubPage';
import { NursingHubPage } from '../pages/hubs/NursingHubPage';
import { ReceptionWorkspacePage } from '../pages/hubs/ReceptionWorkspacePage';
import { AmbulatoryWorkspacePage } from '../pages/hubs/AmbulatoryWorkspacePage';
import { SusBillingHubPage } from '../pages/hubs/SusBillingHubPage';
import { QualityHubPage } from '../pages/hubs/QualityHubPage';
import { RegulationHubPage } from '../pages/hubs/RegulationHubPage';
import { PhysicalAccessHubPage } from '../pages/hubs/PhysicalAccessHubPage';
import { GovIntegrationsHubPage } from '../pages/hubs/GovIntegrationsHubPage';
import { TransportHubPage } from '../pages/hubs/TransportHubPage';
import { HotelariaHubPage } from '../pages/hubs/HotelariaHubPage';
import { SecurityLgpdHubPage } from '../pages/hubs/SecurityLgpdHubPage';
import { CmePage } from '../pages/CmePage';
import { SecurityPage } from '../pages/SecurityPage';
import { HospitalReferenceCatalogPage } from '../pages/HospitalReferenceCatalogPage';
import { ConfigHubPage } from '../pages/hubs/ConfigHubPage';
import { AutomacaoHubPage } from '../pages/hubs/AutomacaoHubPage';
import { AdministrationRoutesCatalogPage } from '../pages/AdministrationRoutesCatalogPage';
import { FuniGuidesHubPage } from '../pages/FuniGuidesHubPage';
import { OncologyPage } from '../pages/OncologyPage';
import { FeegowSghcWorkspacePage } from '../pages/FeegowSghcWorkspacePage';
import { LegacyRedirectPage } from '../pages/LegacyRedirectPage';
import { FUNI_GUIDE_BASE, FUNI_GUIDE_CATALOG } from '../data/funiGuides/catalog';
import { TpaPage } from '../pages/TpaPage';
import { PayrollPage } from '../pages/PayrollPage';
import { PharmacyBillingPage } from '../pages/PharmacyBillingPage';
import { HelpHubPage } from '../pages/HelpHubPage';
import { DownloadsCenterPage } from '../pages/DownloadsCenterPage';
import { BirthRegistrationPage } from '../pages/BirthRegistrationPage';

const BiPage = lazyPage(() => import('../pages/BiPage').then((m) => ({ default: m.BiPage })));
const FinancialHubPage = lazyPage(() => import('../pages/hubs/FinancialHubPage').then((m) => ({ default: m.FinancialHubPage })));
const ConnectHubPage = lazyPage(() => import('../pages/ConnectHubPage').then((m) => ({ default: m.ConnectHubPage })));
const ConnectPage = lazyPage(() => import('../pages/ConnectPage').then((m) => ({ default: m.ConnectPage })));
const TvSignageAdminPage = lazyPage(() => import('../pages/tv/TvSignageAdminPage').then((m) => ({ default: m.TvSignageAdminPage })));

/** Rotas que reutilizam páginas já implementadas (inclui aliases do novo menu). */
export const implementedRoutes: Record<string, ComponentType> = {
  '/': DashboardPage,
  '/bi': BiPage,
  '/pesquisa/mimic': MimicResearchPage,
  '/notificacoes': NotificationsPage,
  '/pendencias': PendenciesPage,
  '/recepcao': ReceptionWorkspacePage,
  '/ambulatorio': AmbulatoryWorkspacePage,
  '/agendamentos': LegacyRedirectPage,
  '/emergencia': EmergencyPage,
  '/internacao': HospitalizationHubPage,
  '/centro-cirurgico': SurgeryPage,
  '/uti': IcuPage,
  '/pacientes': LegacyRedirectPage,
  '/convenios': HealthPlansPage,
  '/convenios/tpa': TpaPage,
  '/consultorios': LegacyRedirectPage,
  '/farmacia': PharmacyHubPage,
  '/farmacia/faturamento': PharmacyBillingPage,
  '/estoque': InventoryPage,
  '/compras': PurchasingPage,
  '/laboratorio': LaboratoryPage,
  '/imagem': ImagingPage,
  '/hemoterapia': HemotherapyPage,
  '/nutricao': NutritionPage,
  '/oncologia': OncologyPage,
  '/faturamento-tiss': TissPage,
  '/faturamento-tiss/guias-funi': FuniGuidesHubPage,
  '/financeiro': FinancialHubPage,
  '/rh': HrPage,
  '/rh/folha': PayrollPage,
  '/profissionais': ProfessionalsPage,
  '/ccih': InfectionControlPage,
  '/auditoria': AuditPage,
  '/engenharia-clinica': ClinicalEngineeringPage,
  '/relatorios': ReportsPage,
  '/guias': GuidesHubPage,
  '/integracoes': IntegrationsPage,
  '/connect': ConnectHubPage,
  '/connect/tv-corporativa': TvSignageAdminPage,
  '/connect/whatsapp': ConnectPage,
  '/ia': AiPage,
  '/telemedicina': TelemedicinePage,
  '/usuarios': UsersPage,
  '/dashboard/tarefas': DashboardTasksPage,
  '/dashboard/assistencial': DashboardAssistencialPage,
  '/dashboard/command-center': CommandCenterPage,
  '/configuracoes/regras-negocio': BusinessRulesPage,
  '/configuracoes/catalogo-hospitalar': HospitalReferenceCatalogPage,
  '/configuracoes/atualizacoes-oficiais': OfficialUpdatesPage,
  '/pep': PepHubPage,
  '/pep/vias-administracao': AdministrationRoutesCatalogPage,
  '/enfermagem': NursingHubPage,
  '/faturamento': SusBillingHubPage,
  '/qualidade': QualityHubPage,
  '/regulacao': RegulationHubPage,
  '/agenda': LegacyRedirectPage,
  '/acesso-fisico': PhysicalAccessHubPage,
  '/integracoes-gov': GovIntegrationsHubPage,
  '/transportes': TransportHubPage,
  '/hotelaria': HotelariaHubPage,
  '/dialise': DialysisPage,
  '/fisioterapia': PhysiotherapyPage,
  '/lavanderia': LaundryPage,
  '/residuos': WasteManagementPage,
  '/ambulancias': AmbulancePage,
  '/seguranca-lgpd': SecurityLgpdHubPage,
  '/cme': CmePage,
  '/seguranca': SecurityPage,
  '/configuracoes': ConfigHubPage,
  '/configuracoes/downloads': DownloadsCenterPage,
  '/ajuda': HelpHubPage,
  '/relatorios/downloads': DownloadsCenterPage,
  '/recepcao/registro-nascimento': BirthRegistrationPage,
  '/automacao': AutomacaoHubPage,
  '/sghc': FeegowSghcWorkspacePage,
};

for (const guide of FUNI_GUIDE_CATALOG) {
  implementedRoutes[`${FUNI_GUIDE_BASE}/${guide.slug}`] = FuniGuidesHubPage;
}

const implementedPrefixes = Object.keys(implementedRoutes)
  .filter((key) => key !== '/')
  .sort((a, b) => b.length - a.length);

export function resolvePageComponent(path: string): ComponentType {
  if (implementedRoutes[path]) return implementedRoutes[path];

  const parent = implementedPrefixes.find(
    (key) => path.startsWith(`${key}/`) || path === key,
  );
  if (parent) return implementedRoutes[parent];

  return ModulePlaceholderPage;
}

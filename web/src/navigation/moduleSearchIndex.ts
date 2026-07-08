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
  dashboardTabs,
  emergencyTabs,
  financialTabs,
  govIntegrationTabs,
  hemotherapyTabs,
  hospitalizationTabs,
  hrTabs,
  icuTabs,
  imagingTabs,
  integrationTabs,
  inventoryTabs,
  labTabs,
  nursingTabs,
  nutritionTabs,
  oncologyTabs,
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
  hotelariaTabs,
  dialysisTabs,
  physiotherapyTabs,
  laundryTabs,
  ambulanceTabs,
} from './moduleSections';
import type { ModuleTab } from './useModuleSection';
import { ambulatoryHubTabs, receptionHubTabs } from './patientWorkspaceConfig';
import { buildMenuSections, type MenuBuildOptions } from './sidebarMenu';
import { flattenMenuLeaves } from './types';
import { MENU_MEGA_GROUPS } from './menuMegaGroups';
import { BAYANNO_ROLE_LABELS, BAYANNO_SCREENS } from '../data/bayanno';

export type ModuleSearchItem = {
  id: string;
  label: string;
  path: string;
  section: string;
  megaGroup: string;
  kind: 'module' | 'tab';
  parentLabel?: string;
};

const MODULE_TABS: Record<string, ModuleTab[]> = {
  '/recepcao': receptionHubTabs,
  '/recepcao/pacientes': patientTabs,
  '/recepcao/agendamentos': appointmentTabs,
  '/ambulatorio': ambulatoryHubTabs,
  '/ambulatorio/agenda': agendaTabs,
  '/ambulatorio/consultorios': consultingRoomsTabs,
  '/agendamentos': appointmentTabs,
  '/emergencia': emergencyTabs,
  '/internacao': hospitalizationTabs,
  '/centro-cirurgico': surgeryTabs,
  '/uti': icuTabs,
  '/pacientes': patientTabs,
  '/pep': pepTabs,
  '/enfermagem': nursingTabs,
  '/laboratorio': labTabs,
  '/imagem': imagingTabs,
  '/farmacia': pharmacyTabs,
  '/estoque': inventoryTabs,
  '/compras': purchasingTabs,
  '/hemoterapia': hemotherapyTabs,
  '/nutricao': nutritionTabs,
  '/oncologia': oncologyTabs,
  '/faturamento': susBillingTabs,
  '/faturamento-tiss': tissTabs,
  '/guias': guidesTabs,
  '/financeiro': financialTabs,
  '/rh': hrTabs,
  '/ccih': ccihTabs,
  '/relatorios': reportsTabs,
  '/bi': biTabs,
  '/qualidade': qualityTabs,
  '/regulacao': regulationTabs,
  '/agenda': agendaTabs,
  '/consultorios': consultingRoomsTabs,
  '/cme': cmeTabs,
  '/seguranca': securityPortariaTabs,
  '/seguranca-lgpd': securityLgpdTabs,
  '/configuracoes': configTabs,
  '/automacao': automacaoTabs,
  '/engenharia-clinica': clinicalEngTabs,
  '/integracoes': integrationTabs,
  '/integracoes-gov': govIntegrationTabs,
  '/acesso-fisico': physicalAccessTabs,
  '/transportes': transportTabs,
  '/hotelaria': hotelariaTabs,
  '/dialise': dialysisTabs,
  '/fisioterapia': physiotherapyTabs,
  '/lavanderia': laundryTabs,
  '/ambulancias': ambulanceTabs,
  '/connect': connectTabs,
  '/connect/whatsapp': connectWhatsAppTabs,
  '/ia': aiTabs,
  '/dashboard': dashboardTabs,
};

/** Palavras-chave extras para busca em português (sinônimos hospitalares). */
const PATH_SYNONYMS: Record<string, string> = {
  '/recepcao': 'entrada check-in cadastro agenda recepção pacientes agendamentos convênios',
  '/ambulatorio': 'consulta ambulatorial agenda médica consultórios atendimentos',
  '/recepcao/pacientes': 'cadastro paciente responsável documentos',
  '/recepcao/agendamentos': 'agenda consulta retorno encaminhamento',
  '/emergencia': 'pronto atendimento pa ps urgência manchester triagem',
  '/pep': 'prontuário eletrônico anamnese evolução prescrição solicitação exames cid vias administração oral intravenosa',
  '/pep/vias-administracao': 'vias administração medicamento oral intravenosa intramuscular catálogo madre',
  '/pacientes': 'cadastro paciente responsável documentos',
  '/internacao': 'leito admissão alta transferência uti',
  '/uti': 'terapia intensiva monitorização',
  '/centro-cirurgico': 'cirurgia cc sala operatório',
  '/faturamento-tiss': 'tiss guia funi convênio glosa consulta sp-sadt honorários lote autorização fechamento',
  '/faturamento-tiss/inserir/consulta': 'nova guia consulta tiss inserir',
  '/faturamento-tiss/inserir/spsadt': 'nova guia sp-sadt exame procedimento tiss inserir',
  '/faturamento-tiss/inserir/honorarios': 'nova guia honorários médicos tiss inserir',
  '/faturamento-tiss/lotes': 'lotes tiss xml administrar envio',
  '/faturamento-tiss/fechamento': 'fechar lote fechamento tiss',
  '/faturamento-tiss/glosas': 'glosa convênio recurso negativa',
  '/faturamento-tiss/autorizacoes': 'autorização convênio senha guia',
  '/faturamento': 'faturamento sus aih apac bpa painel hospitalar',
  '/convenios/tpa': 'tpa convênio operadora pagamento',
  '/faturamento-tiss/guias-funi': 'funi formulário impressão pdf catálogo ans tiss',
  '/guias': 'guias consulta exame procedimento internação tiss sus autorização faturamento impressão',
  '/guias/sus': 'sus bpa apac aih boletim produção ambulatorial internação',
  '/guias/consultas': 'guia consulta convênio funi impressão',
  '/guias/exames': 'guia exame sp-sadt laboratório imagem',
  '/guias/internacao': 'guia internação solicitação resumo alta',
  '/laboratorio': 'exame lab resultado laudo',
  '/imagem': 'raio-x tomografia ressonância ultrassom pacs',
  '/farmacia': 'medicamento dispensação',
  '/ccih': 'infecção vigilância isolamento',
  '/enfermagem': 'sae curativo sinais vitais leito pulseira scan',
  '/relatorios': 'relatório indicador',
  '/convenios': 'operadora plano saúde',
  '/configuracoes': 'parâmetros cadastro integração aparência tema',
  '/configuracoes/aparencia': 'tema claro escuro clínica hospital densidade visual',
  '/configuracoes/atualizacoes-oficiais': 'tuss tiss sigtap ans datasus atualização oficial catálogo',
  '/financeiro/caixas': 'caixa tesouraria sessão abertura fechamento financeiro',
  '/financeiro/recibos-diversos': 'recibo diverso avulso pagamento impressão financeiro',
  '/recepcao/vacinacao': 'vacina vacinação imunização calendário paciente',
  '/estoque/farmacia-ala': 'farmácia ala estoque medicamento enfermaria ward',
  '/dialise': 'hemodiálise diálise renal sessão',
  '/fisioterapia': 'fisioterapia reabilitação motora respiratória',
  '/lavanderia': 'roupa hospitalar lavanderia higienização têxtil',
  '/residuos': 'resíduos lixo coleta infectante perfurocortante hospitalar waste',
  '/estoque/kits': 'kit produto cirurgia emergência curativo pacote',
  '/estoque/config/medicamento-convenio': 'medicamento convênio plano saúde mapeamento genérico',
  '/rh': 'recursos humanos colaboradores folha escala plantão férias treinamento avaliação pessoas',
  '/rh/folha': 'folha pagamento holerite salário encargos INSS IRRF',
  '/rh/escalas': 'escala plantão turno enfermagem',
  '/rh/plantoes': 'plantão sobreaviso escala médica',
  '/hotelaria': 'hotelaria higienização leito limpeza noc housekeeping',
  '/financeiro/honorarios': 'honorários médicos repasse profissional',
  '/financeiro/propostas': 'proposta orçamento paciente',
  '/financeiro/tef': 'tef cartão débito crédito transação eletrônica',
  '/financeiro/cheques': 'cheque recebimento pagamento',
  '/ambulancias': 'ambulância remoção urgência samu transporte',
};

const FEEGOW_EXTRA_SEARCH_ITEMS: Omit<ModuleSearchItem, 'megaGroup'>[] = [
  {
    id: 'feegow-caixas',
    label: 'Caixas',
    path: '/financeiro/caixas',
    section: 'Financeiro',
    kind: 'module',
  },
  {
    id: 'feegow-vacinacao',
    label: 'Vacinação',
    path: '/recepcao/vacinacao',
    section: 'Pacientes',
    kind: 'module',
  },
  {
    id: 'feegow-farmacia-ala',
    label: 'Farmácia por Ala',
    path: '/estoque/farmacia-ala',
    section: 'Estoque',
    kind: 'module',
  },
  {
    id: 'feegow-vias-administracao',
    label: 'Vias de administração',
    path: '/pep/vias-administracao',
    section: 'PEP',
    kind: 'module',
  },
  {
    id: 'feegow-sghc-hub',
    label: 'SGHC Bayanno (73 telas)',
    path: '/sghc',
    section: 'SGHC',
    kind: 'module',
  },
  {
    id: 'feegow-kits',
    label: 'Kits de Produtos',
    path: '/estoque/kits',
    section: 'Estoque',
    kind: 'module',
  },
  {
    id: 'feegow-med-convenio',
    label: 'Medicamento por Convênio',
    path: '/estoque/config/medicamento-convenio',
    section: 'Estoque',
    kind: 'module',
  },
  {
    id: 'feegow-hotelaria',
    label: 'Hotelaria (NOC)',
    path: '/hotelaria',
    section: 'Operações',
    kind: 'module',
  },
  {
    id: 'feegow-residuos',
    label: 'Resíduos e Coleta',
    path: '/residuos',
    section: 'Operações',
    kind: 'module',
  },
  {
    id: 'feegow-rh-folha',
    label: 'Folha de pagamento',
    path: '/rh/folha',
    section: 'Recursos Humanos',
    kind: 'module',
  },
  {
    id: 'feegow-fat-tiss',
    label: 'Faturamento TISS',
    path: '/faturamento-tiss',
    section: 'Faturamento',
    kind: 'module',
  },
  {
    id: 'feegow-fat-tiss-consulta',
    label: 'Nova guia TISS — Consulta',
    path: '/faturamento-tiss/inserir/consulta',
    section: 'Faturamento',
    kind: 'module',
  },
  {
    id: 'feegow-fat-tiss-lotes',
    label: 'Lotes TISS',
    path: '/faturamento-tiss/lotes',
    section: 'Faturamento',
    kind: 'module',
  },
  {
    id: 'feegow-fat-tiss-fechamento',
    label: 'Fechar lote TISS',
    path: '/faturamento-tiss/fechamento',
    section: 'Faturamento',
    kind: 'module',
  },
  {
    id: 'feegow-fat-sus',
    label: 'Painel Faturamento SUS',
    path: '/faturamento',
    section: 'Faturamento',
    kind: 'module',
  },
  {
    id: 'feegow-fat-tpa',
    label: 'TPA',
    path: '/convenios/tpa',
    section: 'Faturamento',
    kind: 'module',
  },
];

function megaGroupLabel(id: string): string {
  return MENU_MEGA_GROUPS.find((g) => g.id === id)?.title ?? id;
}

function normalize(text: string): string {
  return text
    .normalize('NFD')
    .replace(/\p{Diacritic}/gu, '')
    .toLowerCase();
}

function tabPath(base: string, slug: string): string {
  return slug ? `${base.replace(/\/$/, '')}/${slug}` : base;
}

export function buildModuleSearchIndex(options: MenuBuildOptions): ModuleSearchItem[] {
  const sections = buildMenuSections(options);
  const items: ModuleSearchItem[] = [];
  const seen = new Set<string>();

  function push(item: ModuleSearchItem) {
    const key = `${item.path}|${item.label}`;
    if (seen.has(key)) return;
    seen.add(key);
    items.push(item);
  }

  for (const section of sections) {
    const mega = megaGroupLabel(section.megaGroup);
    for (const leaf of flattenMenuLeaves(section.nodes)) {
      const base = leaf.path.split('?')[0];
      push({
        id: `menu-${leaf.id}`,
        label: leaf.label,
        path: leaf.path,
        section: section.title,
        megaGroup: mega,
        kind: 'module',
      });

      const tabs = MODULE_TABS[base];
      if (tabs) {
        for (const tab of tabs) {
          if (!tab.slug && tab.label === leaf.label) continue;
          push({
            id: `tab-${leaf.id}-${tab.slug || 'root'}`,
            label: tab.label,
            path: tabPath(base, tab.slug),
            section: section.title,
            megaGroup: mega,
            kind: 'tab',
            parentLabel: leaf.label,
          });
        }
      }
    }
  }

  for (const extra of FEEGOW_EXTRA_SEARCH_ITEMS) {
    push({
      ...extra,
      megaGroup: extra.section,
    });
  }

  for (const screen of BAYANNO_SCREENS) {
    if (screen.kind === 'layout') continue;
    push({
      id: `sghc-${screen.id}`,
      label: `${screen.title} (SGHC)`,
      path: screen.path,
      section: BAYANNO_ROLE_LABELS[screen.role] ?? screen.role,
      megaGroup: 'SGHC',
      kind: 'module',
      parentLabel: 'Bayanno',
    });
  }

  return items;
}

export function searchModules(items: ModuleSearchItem[], query: string, limit = 12): ModuleSearchItem[] {
  const q = normalize(query.trim());
  if (!q) {
    return items.filter((i) => i.kind === 'module').slice(0, limit);
  }

  const tokens = q.split(/\s+/).filter(Boolean);

  return items
    .map((item) => {
      const synonyms = PATH_SYNONYMS[item.path.split('?')[0]] ?? '';
      const haystack = normalize(
        `${item.label} ${item.parentLabel ?? ''} ${item.section} ${item.megaGroup} ${item.path} ${synonyms}`,
      );

      let score = 0;
      if (normalize(item.label) === q) score = 200;
      else if (normalize(item.label).startsWith(q)) score = 150;
      else if (haystack.includes(q)) score = 120;
      else if (tokens.every((t) => haystack.includes(t))) score = 100;
      else if (tokens.some((t) => normalize(item.label).includes(t))) score = 60;

      if (item.kind === 'module') score += 5;
      return { item, score };
    })
    .filter((r) => r.score > 0)
    .sort((a, b) => b.score - a.score || a.item.label.localeCompare(b.item.label, 'pt-BR'))
    .slice(0, limit)
    .map((r) => r.item);
}

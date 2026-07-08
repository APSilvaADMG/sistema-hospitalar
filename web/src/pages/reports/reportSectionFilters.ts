/** Mapeia rotas /relatorios/:secao para grupos funcionais (legado + deep links). */

export const REPORT_SECTION_FILTERS: Record<string, { module?: string; search?: string; groupId?: string }> = {

  pacientes: { groupId: 'pacientes' },

  agenda: { groupId: 'agenda' },

  'estoque-farmacia': { groupId: 'estoque-farmacia' },

  financeiro: { groupId: 'financeiro', search: 'financeiro' },

  faturamento: { groupId: 'faturamento' },

  internacao: { groupId: 'internacao' },

  'rh-gestao': { groupId: 'rh-gestao' },

  indicadores: { groupId: 'indicadores' },

  atendimento: { module: 'Reception' },

  uti: { module: 'Hospitalization', search: 'UTI' },

  'centro-cirurgico': { module: 'Surgery' },

  producao: { search: 'produção' },

  ocupacao: { search: 'ocupação' },

  utilizacao: { search: 'utilização' },

  receitas: { search: 'receita' },

  despesas: { search: 'despesa' },

  'fluxo-caixa': { search: 'fluxo caixa' },

  'convenios/producao': { module: 'Insurance', search: 'produção' },

  'convenios/glosas': { module: 'Insurance', search: 'glosa' },

  'convenios/autorizacoes': { module: 'Insurance', search: 'autorização' },

  'farmacia/consumo': { module: 'Pharmacy', search: 'consumo' },

  'farmacia/estoque': { module: 'Pharmacy', search: 'estoque' },

  'farmacia/validades': { module: 'Pharmacy', search: 'validade' },

  'almoxarifado/entradas': { module: 'Supply', search: 'entrada' },

  'almoxarifado/saidas': { module: 'Supply', search: 'saída' },

  'almoxarifado/inventario': { module: 'Supply', search: 'inventário' },

  'rh/escalas': { module: 'HumanResources', search: 'escala' },

  'rh/plantoes': { module: 'HumanResources', search: 'plantão' },

  'rh/produtividade': { module: 'HumanResources', search: 'produtividade' },

  'qualidade/indicadores': { module: 'Quality', search: 'indicador' },

  'qualidade/nao-conformidades': { module: 'Quality', search: 'não conformidade' },

  'qualidade/auditorias': { module: 'Quality', search: 'auditoria' },

  ccih: { module: 'InfectionControl' },

  epidemiologia: { module: 'InfectionControl', search: 'epidêm' },

  'ccih/surtos': { module: 'InfectionControl', search: 'surto' },

  'ccih/mortalidade': { module: 'InfectionControl', search: 'mortalidade' },

  'ccih/vacinacao': { module: 'InfectionControl', search: 'vacinal' },

  'financeiro/dre': { module: 'Financial', search: 'DRE' },

  'regulatorio/bpa': { module: 'Regulatory', search: 'BPA' },

  'regulatorio/aih': { module: 'Regulatory', search: 'AIH' },

  'regulatorio/sus': { module: 'Regulatory', search: 'SUS' },

};



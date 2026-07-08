export type HealthCampaignColor =
  | 'white'
  | 'purple'
  | 'orange'
  | 'lilac'
  | 'navy'
  | 'blue'
  | 'green'
  | 'yellow'
  | 'red'
  | 'gold'
  | 'pink';

export type HealthCampaignItem = {
  color: HealthCampaignColor;
  colorLabel: string;
  topic: string;
};

export type MonthlyHealthCampaigns = {
  month: number;
  monthName: string;
  campaigns: HealthCampaignItem[];
  examsHint?: string;
};

export const healthCampaignCalendar: MonthlyHealthCampaigns[] = [
  {
    month: 1,
    monthName: 'Janeiro',
    campaigns: [
      { color: 'white', colorLabel: 'Branco', topic: 'Saúde mental e emocional' },
      { color: 'purple', colorLabel: 'Roxo', topic: 'Combate e prevenção à Hanseníase' },
    ],
    examsHint: 'Avaliação de saúde mental; exame dermatológico em suspeita de hanseníase.',
  },
  {
    month: 2,
    monthName: 'Fevereiro',
    campaigns: [
      { color: 'purple', colorLabel: 'Roxo', topic: 'Conscientização sobre Lúpus, Alzheimer e Fibromialgia' },
      { color: 'orange', colorLabel: 'Laranja', topic: 'Alerta e combate à Leucemia' },
    ],
    examsHint: 'Hemograma e investigação conforme sintomas; acompanhamento neurológico para memória.',
  },
  {
    month: 3,
    monthName: 'Março',
    campaigns: [
      { color: 'lilac', colorLabel: 'Lilás', topic: 'Prevenção do câncer do colo do útero' },
      { color: 'navy', colorLabel: 'Azul-marinho', topic: 'Conscientização sobre o câncer colorretal' },
    ],
    examsHint: 'Papanicolau / HPV; colonoscopia ou sangue oculto nas fezes conforme idade e risco.',
  },
  {
    month: 4,
    monthName: 'Abril',
    campaigns: [
      { color: 'blue', colorLabel: 'Azul', topic: 'Conscientização sobre o Autismo' },
      { color: 'green', colorLabel: 'Verde', topic: 'Prevenção e segurança no ambiente de trabalho' },
    ],
    examsHint: 'Triagem do desenvolvimento infantil; avaliação de saúde ocupacional.',
  },
  {
    month: 5,
    monthName: 'Maio',
    campaigns: [
      { color: 'yellow', colorLabel: 'Amarelo', topic: 'Segurança no trânsito e prevenção de acidentes' },
      { color: 'purple', colorLabel: 'Roxo', topic: 'Doenças Inflamatórias Intestinais (Crohn e Retocolite)' },
    ],
    examsHint: 'Orientação preventiva; investigação digestiva se sintomas persistentes.',
  },
  {
    month: 6,
    monthName: 'Junho',
    campaigns: [
      { color: 'red', colorLabel: 'Vermelho', topic: 'Estímulo à doação de sangue' },
      { color: 'orange', colorLabel: 'Laranja', topic: 'Prevenção e combate à Anemia e Leucemia' },
    ],
    examsHint: 'Hemograma; tipagem sanguínea para doação quando elegível.',
  },
  {
    month: 7,
    monthName: 'Julho',
    campaigns: [
      { color: 'yellow', colorLabel: 'Amarelo', topic: 'Prevenção e tratamento das Hepatites Virais' },
      { color: 'green', colorLabel: 'Verde', topic: 'Combate ao câncer de cabeça e pescoço' },
    ],
    examsHint: 'Sorologias para hepatites; avaliação ORL em fatores de risco (tabagismo, etilismo).',
  },
  {
    month: 8,
    monthName: 'Agosto',
    campaigns: [
      { color: 'gold', colorLabel: 'Dourado', topic: 'Estímulo e conscientização sobre o aleitamento materno' },
      { color: 'green', colorLabel: 'Verde', topic: 'Combate ao linfoma' },
    ],
    examsHint: 'Acompanhamento pré-natal e puericultura; investigação de linfonodos se indicado.',
  },
  {
    month: 9,
    monthName: 'Setembro',
    campaigns: [
      { color: 'yellow', colorLabel: 'Amarelo', topic: 'Prevenção ao suicídio e valorização da vida' },
      { color: 'green', colorLabel: 'Verde', topic: 'Conscientização sobre a doação de órgãos' },
    ],
    examsHint: 'Rastreio de saúde mental; orientação sobre doação e fila de transplante.',
  },
  {
    month: 10,
    monthName: 'Outubro',
    campaigns: [
      { color: 'pink', colorLabel: 'Rosa', topic: 'Prevenção e diagnóstico precoce do câncer de mama' },
    ],
    examsHint: 'Mamografia conforme idade; autoexame e consulta ginecológica.',
  },
  {
    month: 11,
    monthName: 'Novembro',
    campaigns: [
      { color: 'blue', colorLabel: 'Azul', topic: 'Saúde integral do homem e prevenção ao câncer de próstata' },
    ],
    examsHint: 'PSA e toque retal conforme idade e orientação médica; check-up masculino.',
  },
  {
    month: 12,
    monthName: 'Dezembro',
    campaigns: [
      { color: 'orange', colorLabel: 'Laranja', topic: 'Conscientização sobre o câncer de pele' },
      { color: 'red', colorLabel: 'Vermelho', topic: 'Prevenção e combate ao HIV/AIDS e outras ISTs' },
    ],
    examsHint: 'Dermatoscopia em lesões suspeitas; testes rápidos para HIV e outras ISTs.',
  },
];

export function getCurrentMonthCampaigns(date = new Date()): MonthlyHealthCampaigns {
  const month = date.getMonth() + 1;
  return healthCampaignCalendar.find((m) => m.month === month) ?? healthCampaignCalendar[0];
}

import type { TvNewsDto } from '../../api/client';

/** Item de notícia com campos obrigatórios preenchidos — evita erros de tipo em fallbacks do player. */
export function tvNewsItem(partial: Partial<TvNewsDto> & Pick<TvNewsDto, 'id' | 'title'>): TvNewsDto {
  return {
    publishedAt: new Date().toISOString(),
    ...partial,
  };
}

export const TV_DEFAULT_NEWS: TvNewsDto = tvNewsItem({
  id: 'default',
  title: 'Bem-vindo',
  summary: 'Cuide da sua saúde. Em caso de dúvida, procure nossa equipe na recepção.',
});

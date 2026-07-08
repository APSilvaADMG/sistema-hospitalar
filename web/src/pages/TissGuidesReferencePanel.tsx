import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import type { TissGuideTypeCatalogDto } from '../api/client';
import { tissGuideCategoryLabels } from '../data/tissGuideCatalog';

type Props = {
  catalog: TissGuideTypeCatalogDto[];
  onCreateGuide?: (guideType: number) => void;
};

export function TissGuidesReferencePanel({ catalog, onCreateGuide }: Props) {
  const [category, setCategory] = useState('');
  const [expanded, setExpanded] = useState<number | null>(null);

  const filtered = useMemo(() => {
    if (!category) return catalog;
    return catalog.filter((g) => g.category === category);
  }, [catalog, category]);

  const categories = useMemo(
    () => [...new Set(catalog.map((g) => g.category))],
    [catalog],
  );

  return (
    <div className="tiss-reference">
      <div className="card-panel tiss-reference-intro">
        <h3>Padrão TISS — Guias ANS</h3>
        <p>
          As <strong>Guias TISS</strong> (Troca de Informações na Saúde Suplementar) são formulários padronizados
          pela Agência Nacional de Saúde Suplementar (ANS) para comunicação entre hospitais/clínicas e operadoras.
          O preenchimento correto reduz <strong>glosas</strong> (recusas de pagamento).
        </p>
        <p className="form-hint">
          Consulte os{' '}
          <a href="https://www.gov.br/ans/pt-br/assuntos/prestadores/padrao-para-troca-de-informacao-de-saude-suplementar-2013-tiss" target="_blank" rel="noreferrer">
            Manuais de Preenchimento TISS
          </a>{' '}
          no portal oficial do Governo Federal.
        </p>
      </div>

      <div className="tiss-reference-filters">
        <button
          type="button"
          className={`btn btn-sm ${category === '' ? '' : 'btn-secondary'}`}
          onClick={() => setCategory('')}
        >
          Todos ({catalog.length})
        </button>
        {categories.map((cat) => (
          <button
            key={cat}
            type="button"
            className={`btn btn-sm ${category === cat ? '' : 'btn-secondary'}`}
            onClick={() => setCategory(cat)}
          >
            {tissGuideCategoryLabels[cat] ?? cat} ({catalog.filter((g) => g.category === cat).length})
          </button>
        ))}
      </div>

      <div className="tiss-reference-grid">
        {filtered.map((guide) => (
          <article
            key={guide.code}
            className={`tiss-reference-card${expanded === guide.code ? ' expanded' : ''}`}
          >
            <header>
              <div className="tiss-reference-card-badges">
                <span className="badge badge-neutral">{guide.categoryLabel}</span>
                {guide.isImplemented ? (
                  <span className="badge badge-success">No sistema</span>
                ) : (
                  <span className="badge badge-neutral">Em breve</span>
                )}
              </div>
              <h4>{guide.name}</h4>
              <p>{guide.description}</p>
            </header>

            <button
              type="button"
              className="btn btn-secondary btn-sm"
              onClick={() => setExpanded(expanded === guide.code ? null : guide.code)}
            >
              {expanded === guide.code ? 'Ocultar detalhes' : 'Quando usar?'}
            </button>

            {expanded === guide.code && (
              <div className="tiss-reference-detail">
                <p><strong>Quando utilizar:</strong> {guide.whenToUse}</p>
                {guide.linkedTab && (
                  <p className="form-hint">
                    No APSMedCore: aba <strong>{guide.linkedTab}</strong>
                    {guide.isCreatable && guide.isImplemented && onCreateGuide && (
                      <>
                        {' · '}
                        <button type="button" className="link-btn" onClick={() => onCreateGuide(guide.code)}>
                          Criar guia deste tipo
                        </button>
                      </>
                    )}
                  </p>
                )}
              </div>
            )}
          </article>
        ))}
      </div>

      <div className="card-panel tiss-reference-tips">
        <h4>Principais guias no dia a dia hospitalar</h4>
        <ul>
          <li><strong>Consulta</strong> — ambulatório e consultórios eletivos.</li>
          <li><strong>SP/SADT</strong> — laboratório, imagem, terapias e procedimentos ambulatoriais.</li>
          <li><strong>Solicitação de internação</strong> — autorização antes da admissão.</li>
          <li><strong>Resumo de internação</strong> — faturamento após alta.</li>
          <li><strong>Honorários</strong> — equipe médica em internação/cirurgia.</li>
          <li><strong>GTO</strong> — odontologia.</li>
        </ul>
        <p className="form-hint">
          Dúvidas sobre glosa? Use a aba <Link to="/faturamento-tiss">Guias TISS</Link> para registrar recurso
          ou consulte o módulo de <Link to="/relatorios">Relatórios → Convênios</Link>.
        </p>
      </div>
    </div>
  );
}

import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import {
  api,
  type HelpArticleListItemDto,
  type HelpCategoryDto,
  type HelpSummaryDto,
} from '../api/client';
import { useAuth } from '../auth/AuthContext';
import { TicketsPanel } from '../components/connect/TicketsPanel';
import {
  HelpArticleDetailView,
  HelpArticleList,
  HelpAssistantChat,
  HelpSearchBar,
  HelpSuggestionsPanel,
  useHelpSearch,
} from '../components/help/HelpArticlePanels';
import { KpiCard } from '../components/KpiCard';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { helpTabs } from '../navigation/helpSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import '../styles/feegow-help.css';

export function HelpHubPage() {
  const { hasPermission } = useAuth();
  const { section, goToSection } = useModuleSection('/ajuda');
  const activeSection = section || '';
  const breadcrumb = findMenuBreadcrumb('/ajuda');
  const [searchParams] = useSearchParams();

  const canConnectWrite = hasPermission('connect.write');

  const [summary, setSummary] = useState<HelpSummaryDto | null>(null);
  const [categories, setCategories] = useState<HelpCategoryDto[]>([]);
  const [articles, setArticles] = useState<HelpArticleListItemDto[]>([]);
  const [selectedSlug, setSelectedSlug] = useState<string | null>(searchParams.get('artigo'));
  const [categoryFilter, setCategoryFilter] = useState('');
  const { query, setQuery, results, searched, search } = useHelpSearch();

  const loadSummary = useCallback(() => {
    api.getHelpSummary().then(setSummary).catch(console.error);
    api.getHelpCategories().then(setCategories).catch(console.error);
  }, []);

  const loadArticles = useCallback(async () => {
    const type = sectionType(activeSection);
    const list = await api.getHelpArticles({
      type: type ?? undefined,
      category: categoryFilter || undefined,
    });
    setArticles(list);
  }, [activeSection, categoryFilter]);

  useEffect(() => {
    loadSummary();
  }, [loadSummary]);

  useEffect(() => {
    if (['faq', 'base', 'videos', 'treinamentos', 'downloads', 'sobre'].includes(activeSection)) {
      loadArticles().catch(console.error);
    }
  }, [activeSection, loadArticles]);

  useEffect(() => {
    const art = searchParams.get('artigo');
    if (art) setSelectedSlug(art);
  }, [searchParams]);

  const displayedArticles = useMemo(() => {
    if (activeSection === 'sobre') {
      return articles.filter((a) => a.categoryCode === 'geral');
    }
    if (activeSection === 'downloads') {
      return articles.filter((a) => a.type === 'Manual');
    }
    return articles;
  }, [articles, activeSection]);

  function openCategory(code: string) {
    setCategoryFilter(code);
    goToSection('faq');
  }

  return (
    <>
      <PageHeader
        eyebrow="Central de Ajuda"
        title={breadcrumb.title ?? 'Ajuda'}
        subtitle="Busca, FAQ, base de conhecimento, treinamentos, suporte e sugestões."
      />

      <ModuleNav basePath="/ajuda" tabs={helpTabs} />

      <HelpSearchBar
        value={query}
        onChange={setQuery}
        onSearch={() => search().catch(console.error)}
      />

      {searched ? (
        <div style={{ marginBottom: 20 }}>
          <h3 style={{ margin: '0 0 8px', fontSize: 16 }}>Resultados da busca</h3>
          {selectedSlug ? (
            <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} />
          ) : (
            <HelpArticleList items={results} onSelect={setSelectedSlug} emptyLabel="Nenhum resultado." />
          )}
        </div>
      ) : null}

      {(activeSection === '' || activeSection === 'faq') && !searched ? (
        <>
          {summary ? (
            <div className="help-kpi-grid">
              <KpiCard label="Artigos" value={summary.totalArticles} />
              <KpiCard label="FAQs" value={summary.totalFaqs} />
              <KpiCard label="Vídeos" value={summary.totalVideos} />
              <KpiCard label="Treinamentos" value={summary.totalTrainings} />
              <KpiCard label="Chamados abertos" value={summary.openTickets} />
              <KpiCard label="Meus chamados" value={summary.myOpenTickets} />
            </div>
          ) : null}

          {activeSection === '' ? (
            <>
              <h3 style={{ margin: '0 0 4px', fontSize: 16 }}>Categorias</h3>
              <p style={{ margin: '0 0 8px', fontSize: 13, color: 'var(--muted)' }}>
                Conteúdo por módulo do sistema
              </p>
              <div className="help-category-grid">
                {categories.map((cat) => (
                  <button
                    key={cat.id}
                    type="button"
                    className="help-category-card"
                    onClick={() => openCategory(cat.code)}
                  >
                    <strong>{cat.icon ? `${cat.icon} ` : ''}{cat.name}</strong>
                    <span>{cat.articleCount} artigo(s)</span>
                  </button>
                ))}
              </div>

              <div className="card-panel appt-panel" style={{ marginTop: 20 }}>
                <div className="card-panel-header">Assistente rápido</div>
                <div className="card-panel-body">
                  <HelpAssistantChat />
                </div>
              </div>
            </>
          ) : null}

          {activeSection === 'faq' ? (
            <>
              {categoryFilter ? (
                <p style={{ fontSize: 13, marginBottom: 8 }}>
                  Categoria: <strong>{categories.find((c) => c.code === categoryFilter)?.name ?? categoryFilter}</strong>
                  {' '}
                  <button type="button" className="btn btn-secondary btn-sm" onClick={() => setCategoryFilter('')}>
                    Limpar
                  </button>
                </p>
              ) : null}
              {selectedSlug ? (
                <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} onTrainingComplete={loadSummary} />
              ) : (
                <div className="help-faq">
                  {displayedArticles.filter((a) => a.type === 'Faq').map((faq) => (
                    <details key={faq.id}>
                      <summary>{faq.title}</summary>
                      <p style={{ margin: '8px 0 0', fontSize: 14 }}>{faq.summary}</p>
                      <button type="button" className="btn btn-secondary btn-sm" style={{ marginTop: 8 }} onClick={() => setSelectedSlug(faq.slug)}>
                        Ver completo
                      </button>
                    </details>
                  ))}
                </div>
              )}
            </>
          ) : null}
        </>
      ) : null}

      {activeSection === 'base' && !searched ? (
        selectedSlug ? (
          <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} />
        ) : (
          <HelpArticleList
            items={displayedArticles.filter((a) => a.type === 'Article')}
            onSelect={setSelectedSlug}
          />
        )
      ) : null}

      {activeSection === 'videos' && !searched ? (
        selectedSlug ? (
          <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} />
        ) : (
          <HelpArticleList
            items={displayedArticles.filter((a) => a.type === 'Video')}
            onSelect={setSelectedSlug}
          />
        )
      ) : null}

      {activeSection === 'treinamentos' && !searched ? (
        selectedSlug ? (
          <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} onTrainingComplete={loadSummary} />
        ) : (
          <HelpArticleList
            items={displayedArticles.filter((a) => a.type === 'Training')}
            onSelect={setSelectedSlug}
          />
        )
      ) : null}

      {activeSection === 'downloads' && !searched ? (
        selectedSlug ? (
          <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} />
        ) : (
          <>
            <HelpArticleList
              items={displayedArticles}
              onSelect={setSelectedSlug}
              emptyLabel="Nenhum manual cadastrado."
            />
            <p style={{ marginTop: 12, fontSize: 13 }}>
              <Link to="/relatorios/downloads">Central de downloads do sistema</Link>
            </p>
          </>
        )
      ) : null}

      {activeSection === 'chamados' ? (
        <div>
          <p style={{ fontSize: 14, color: 'var(--muted)', marginBottom: 12 }}>
            Abra chamados de suporte técnico e acompanhe protocolos pelo Connect.
          </p>
          <TicketsPanel canWrite={canConnectWrite} initialMyRequests={false} />
        </div>
      ) : null}

      {activeSection === 'sugestoes' ? <HelpSuggestionsPanel /> : null}

      {activeSection === 'sobre' && !searched ? (
        selectedSlug ? (
          <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} />
        ) : (
          <HelpArticleList items={displayedArticles} onSelect={setSelectedSlug} />
        )
      ) : null}
    </>
  );
}

function sectionType(section: string): HelpArticleListItemDto['type'] | null {
  switch (section) {
    case 'faq': return 'Faq';
    case 'base': return 'Article';
    case 'videos': return 'Video';
    case 'treinamentos': return 'Training';
    case 'downloads': return 'Manual';
    case 'sobre': return null;
    default: return null;
  }
}

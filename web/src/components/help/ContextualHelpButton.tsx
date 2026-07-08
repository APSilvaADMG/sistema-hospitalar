import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, type HelpContextDto } from '../../api/client';
import { Modal } from '../Modal';
import { HelpArticleDetailView, HelpArticleList } from './HelpArticlePanels';

type ContextualHelpButtonProps = {
  route?: string;
};

export function ContextualHelpButton({ route }: ContextualHelpButtonProps) {
  const [open, setOpen] = useState(false);
  const [context, setContext] = useState<HelpContextDto | null>(null);
  const [selectedSlug, setSelectedSlug] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open || !route) return;
    setLoading(true);
    setSelectedSlug(null);
    api.getHelpContext(route)
      .then(setContext)
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [open, route]);

  return (
    <>
      <button
        type="button"
        className="help-context-btn"
        onClick={() => setOpen(true)}
        title="Ajuda desta tela"
      >
        <span aria-hidden>?</span> Ajuda
      </button>
      <Modal
        open={open}
        onClose={() => setOpen(false)}
        title={context?.moduleLabel ? `Ajuda — ${context.moduleLabel}` : 'Ajuda contextual'}
        width="lg"
      >
        {loading ? <p style={{ color: 'var(--muted)' }}>Carregando…</p> : null}
        {selectedSlug ? (
          <HelpArticleDetailView slug={selectedSlug} onBack={() => setSelectedSlug(null)} />
        ) : (
          <>
            {!loading && context && context.faqs.length === 0 && context.articles.length === 0 ? (
              <p style={{ color: 'var(--muted)' }}>
                Não há artigos específicos para esta tela. Consulte a{' '}
                <Link to="/ajuda" onClick={() => setOpen(false)}>Central de Ajuda</Link>.
              </p>
            ) : null}
            {context?.faqs.length ? (
              <div className="help-faq" style={{ marginBottom: 16 }}>
                <h4 style={{ margin: '0 0 8px' }}>Perguntas frequentes</h4>
                {context.faqs.map((faq) => (
                  <details key={faq.id}>
                    <summary>{faq.title}</summary>
                    <p style={{ margin: '8px 0 0', fontSize: 14 }}>{faq.summary}</p>
                    <button
                      type="button"
                      className="btn btn-secondary btn-sm"
                      style={{ marginTop: 8 }}
                      onClick={() => setSelectedSlug(faq.slug)}
                    >
                      Ler mais
                    </button>
                  </details>
                ))}
              </div>
            ) : null}
            {context?.articles.length ? (
              <>
                <h4 style={{ margin: '0 0 8px' }}>Artigos relacionados</h4>
                <HelpArticleList items={context.articles} onSelect={setSelectedSlug} />
              </>
            ) : null}
            <p style={{ marginTop: 16, fontSize: 13 }}>
              <Link to="/ajuda/chamados" onClick={() => setOpen(false)}>Abrir chamado de suporte</Link>
            </p>
          </>
        )}
      </Modal>
    </>
  );
}

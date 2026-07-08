import { Link, useLocation } from 'react-router-dom';
import { PageHeader } from '../components/PageHeader';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { resolvePageTitle } from '../navigation/sectionBreadcrumb';
import { getMegaGroupMeta } from '../navigation/menuMegaGroups';

const RELATED_MODULES: Record<string, { label: string; path: string }[]> = {
  clinical: [
    { label: 'Pacientes', path: '/pacientes' },
    { label: 'Prontuário (PEP)', path: '/pep' },
    { label: 'Internação', path: '/internacao' },
    { label: 'Emergência', path: '/emergencia' },
  ],
  diagnostic: [
    { label: 'Farmácia', path: '/farmacia' },
    { label: 'Laboratório', path: '/laboratorio' },
    { label: 'Imagem', path: '/imagem' },
  ],
  administrative: [
    { label: 'Financeiro', path: '/financeiro' },
    { label: 'Faturamento TISS', path: '/faturamento-tiss' },
    { label: 'Estoque', path: '/estoque' },
  ],
  management: [
    { label: 'Dashboard', path: '/' },
    { label: 'BI', path: '/bi' },
    { label: 'Relatórios', path: '/relatorios' },
  ],
  security: [
    { label: 'Segurança e LGPD', path: '/seguranca-lgpd' },
    { label: 'Acesso Físico', path: '/acesso-fisico' },
    { label: 'Integrações', path: '/integracoes' },
  ],
};

function findMegaGroupForPath(pathname: string) {
  if (pathname.startsWith('/faturamento') || pathname.startsWith('/financeiro')) return 'administrative' as const;
  if (pathname.startsWith('/integracoes') || pathname.startsWith('/seguranca-lgpd') || pathname.startsWith('/configuracoes')) return 'security' as const;
  if (pathname.startsWith('/bi') || pathname.startsWith('/relatorios') || pathname.startsWith('/dashboard')) return 'management' as const;
  if (pathname.startsWith('/farmacia') || pathname.startsWith('/laboratorio') || pathname.startsWith('/imagem')) return 'diagnostic' as const;
  return 'clinical' as const;
}

export function ModulePlaceholderPage() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const title = resolvePageTitle(pathname);
  const megaId = findMegaGroupForPath(pathname);
  const mega = getMegaGroupMeta(megaId);
  const related = RELATED_MODULES[megaId] ?? RELATED_MODULES.clinical;

  return (
    <>
      <PageHeader
        eyebrow={breadcrumb.section ?? mega.title}
        title={title}
        subtitle="Esta rota não possui tela dedicada. Use o módulo correspondente no menu ou os atalhos abaixo."
      />

      <div className="card module-placeholder">
        <div
          className="module-placeholder-badge"
          style={{ background: mega.accent, color: mega.color, borderColor: mega.color }}
        >
          {mega.title}
        </div>
        <div className="module-placeholder-icon" aria-hidden>🚧</div>
        <h2>Em desenvolvimento</h2>
        <p>
          A rota <code>{pathname}</code> ainda não foi implementada. Se você chegou aqui por um link antigo,
          volte ao módulo principal — as subtelas ficam nas abas internas de cada área.
        </p>
        {breadcrumb.parents.length > 0 && (
          <p className="module-placeholder-trail">
            {breadcrumb.parents.join(' › ')}
          </p>
        )}
        <div className="module-placeholder-related">
          <strong>Módulos já disponíveis:</strong>
          <div className="module-placeholder-links">
            {related.map((item) => (
              <Link key={item.path} to={item.path} className="btn btn-secondary btn-sm">
                {item.label}
              </Link>
            ))}
          </div>
        </div>
        <div className="module-placeholder-actions">
          <Link to="/" className="btn">Visão Geral</Link>
          <Link to="/relatorios" className="btn btn-secondary">Relatórios</Link>
        </div>
      </div>
    </>
  );
}

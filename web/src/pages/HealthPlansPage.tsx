import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { api, type HealthInsuranceDto } from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { InsuranceLogo } from '../components/InsuranceLogo';
import { KpiCard } from '../components/KpiCard';
import { ModulePageChrome } from '../components/ModulePageChrome';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';

function formatCnpj(value?: string | null): string {
  if (!value) return '—';
  const digits = value.replace(/\D/g, '');
  if (digits.length !== 14) return value;
  return digits.replace(/^(\d{2})(\d{3})(\d{3})(\d{4})(\d{2})$/, '$1.$2.$3/$4-$5');
}

function formatAns(value?: string | null): string {
  if (!value) return '—';
  return value.padStart(6, '0');
}

type HealthPlansPageProps = { embedded?: boolean };

export function HealthPlansPage({ embedded = false }: HealthPlansPageProps = {}) {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [plans, setPlans] = useState<HealthInsuranceDto[]>([]);
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true);
    api
      .getHealthInsurances()
      .then(setPlans)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar convênios.'))
      .finally(() => setLoading(false));
  }, []);

  const filtered = useMemo(() => {
    const term = query.trim().toLowerCase();
    if (!term) return plans;
    return plans.filter(
      (p) =>
        p.name.toLowerCase().includes(term) ||
        (p.ansRegistration ?? '').includes(term) ||
        (p.cnpj ?? '').includes(term.replace(/\D/g, '')),
    );
  }, [plans, query]);

  const withAns = plans.filter((p) => p.ansRegistration).length;
  const privateCount = plans.filter((p) => p.name !== 'SUS' && p.name !== 'Particular').length;

  function handleSearch(event: FormEvent) {
    event.preventDefault();
    setQuery(search);
  }

  return (
    <ModulePageChrome
      embedded={embedded}
      eyebrow="Cadastros"
      title={breadcrumb.title || 'Planos de Saúde'}
      subtitle="Catálogo completo de operadoras e convênios médicos com registro ANS, CNPJ e identidade visual."
    >
      <div className="kpi-grid kpi-grid-6 health-plans-kpi">
        <KpiCard label="Total cadastrado" value={String(plans.length)} variant="primary" />
        <KpiCard label="Operadoras ANS" value={String(withAns)} variant="info" />
        <KpiCard label="Planos privados" value={String(privateCount)} variant="success" />
        <KpiCard label="Exibindo" value={String(filtered.length)} variant="neutral" />
      </div>

      <FilterBar>
        <form className="filter-form" onSubmit={handleSearch}>
          <input
            type="search"
            placeholder="Buscar por nome, ANS ou CNPJ..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button type="submit" className="btn btn-primary">
            Buscar
          </button>
          {query && (
            <button type="button" className="btn btn-secondary" onClick={() => { setSearch(''); setQuery(''); }}>
              Limpar
            </button>
          )}
        </form>
      </FilterBar>

      {error && <p className="form-hint form-hint-error">{error}</p>}
      {loading && <p className="form-hint">Carregando convênios...</p>}

      {!loading && filtered.length === 0 && (
        <p className="form-hint">Nenhum convênio encontrado para &quot;{query}&quot;.</p>
      )}

      <div className="health-plans-grid">
        {filtered.map((plan) => (
          <article key={plan.id} className="health-plan-card">
            <div className="health-plan-card-logo">
              <InsuranceLogo name={plan.name} logoUrl={plan.logoUrl} size={56} />
            </div>
            <div className="health-plan-card-body">
              <h3>{plan.name}</h3>
              <dl className="health-plan-meta">
                <div>
                  <dt>Registro ANS</dt>
                  <dd>{formatAns(plan.ansRegistration)}</dd>
                </div>
                <div>
                  <dt>CNPJ</dt>
                  <dd>{formatCnpj(plan.cnpj)}</dd>
                </div>
              </dl>
              {plan.websiteUrl && (
                <a href={plan.websiteUrl} target="_blank" rel="noreferrer noopener" className="health-plan-link">
                  Site da operadora
                </a>
              )}
            </div>
          </article>
        ))}
      </div>
    </ModulePageChrome>
  );
}

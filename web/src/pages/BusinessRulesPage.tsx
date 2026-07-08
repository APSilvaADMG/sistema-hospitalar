import { useEffect, useMemo, useState } from 'react';
import { api, type BusinessRuleDto } from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { configTabs } from '../navigation/moduleSections';
import { resolvePageTitle } from '../navigation/sectionBreadcrumb';
import { useLocation } from 'react-router-dom';

export function BusinessRulesPage() {
  const { pathname } = useLocation();
  const [rules, setRules] = useState<BusinessRuleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [moduleFilter, setModuleFilter] = useState('');
  const [implementedOnly, setImplementedOnly] = useState(false);
  const [search, setSearch] = useState('');

  useEffect(() => {
    api.getBusinessRules()
      .then(setRules)
      .catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar regras'))
      .finally(() => setLoading(false));
  }, []);

  const modules = useMemo(
    () => [...new Set(rules.map((r) => r.module))].sort(),
    [rules],
  );

  const filtered = useMemo(() => {
    return rules.filter((rule) => {
      if (implementedOnly && !rule.implemented) return false;
      if (moduleFilter && rule.module !== moduleFilter) return false;
      if (!search.trim()) return true;
      const term = search.toLowerCase();
      return (
        rule.code.toLowerCase().includes(term)
        || rule.title.toLowerCase().includes(term)
        || rule.description.toLowerCase().includes(term)
        || (rule.brReference?.toLowerCase().includes(term) ?? false)
        || (rule.layer?.toLowerCase().includes(term) ?? false)
      );
    });
  }, [rules, implementedOnly, moduleFilter, search]);

  const stats = useMemo(() => ({
    total: rules.length,
    implemented: rules.filter((r) => r.implemented).length,
    pending: rules.filter((r) => !r.implemented).length,
  }), [rules]);

  return (
    <>
      <PageHeader
        eyebrow="Configurações · Governança"
        title={resolvePageTitle(pathname) || 'Catálogo de Regras de Negócio'}
        subtitle="Espinha dorsal do APSMedCore — regras documentadas e status de implementação no sistema."
      />

      <ModuleNav basePath="/configuracoes" tabs={configTabs} />

      {error && <div className="alert alert-error">{error}</div>}

      <div className="kpi-grid" style={{ marginBottom: 20 }}>
        <div className="card kpi-card-neutral" style={{ padding: 16 }}>
          <div className="kpi-label">Total catalogadas</div>
          <div className="kpi-value">{stats.total}</div>
        </div>
        <div className="card" style={{ padding: 16, borderLeft: '4px solid #2e7d32' }}>
          <div className="kpi-label">Implementadas</div>
          <div className="kpi-value">{stats.implemented}</div>
        </div>
        <div className="card" style={{ padding: 16, borderLeft: '4px solid #ef6c00' }}>
          <div className="kpi-label">Pendentes</div>
          <div className="kpi-value">{stats.pending}</div>
        </div>
      </div>

      <div className="card-panel appt-panel">
        <div className="card-panel-header">Filtros</div>
        <FilterBar>
          <div className="filter-field w-lg">
            <label htmlFor="br-module">Módulo</label>
            <select id="br-module" value={moduleFilter} onChange={(e) => setModuleFilter(e.target.value)}>
              <option value="">Todos</option>
              {modules.map((m) => <option key={m} value={m}>{m}</option>)}
            </select>
          </div>
          <div className="filter-field grow-sm">
            <label htmlFor="br-search">Buscar</label>
            <input
              id="br-search"
              placeholder="Código, título ou descrição..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="filter-field checkbox align-end">
            <label>
              <input
                type="checkbox"
                checked={implementedOnly}
                onChange={(e) => setImplementedOnly(e.target.checked)}
              />
              Somente implementadas
            </label>
          </div>
        </FilterBar>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 20 }}>
        <div className="card-panel-header">
          Regras — {filtered.length} de {rules.length}
        </div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          {loading ? (
            <p style={{ padding: 24, color: 'var(--muted)' }}>Carregando catálogo...</p>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Ref. BRD</th>
                  <th>Módulo</th>
                  <th>Título</th>
                  <th>Descrição</th>
                  <th>Camada</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((rule) => (
                  <tr key={rule.code}>
                    <td><code>{rule.code}</code></td>
                    <td>{rule.brReference ? <code>{rule.brReference}</code> : '—'}</td>
                    <td>{rule.module}</td>
                    <td><strong>{rule.title}</strong></td>
                    <td style={{ maxWidth: 360 }}>{rule.description}</td>
                    <td style={{ fontSize: '0.85rem', color: 'var(--muted)' }}>{rule.layer ?? '—'}</td>
                    <td>
                      <span className={`badge ${rule.implemented ? 'badge-success' : 'badge-warning'}`}>
                        {rule.implemented ? 'Implementada' : 'Pendente'}
                      </span>
                    </td>
                  </tr>
                ))}
                {filtered.length === 0 && (
                  <tr>
                    <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                      Nenhuma regra encontrada.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </>
  );
}

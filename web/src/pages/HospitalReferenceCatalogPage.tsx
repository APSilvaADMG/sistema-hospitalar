import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  api,
  type HospitalReferenceCatalogItemDto,
  type HospitalReferenceCatalogSummaryDto,
  type HospitalReferenceCatalogType,
  hospitalReferenceCatalogTypeLabels,
} from '../api/client';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { configTabs } from '../navigation/moduleSections';
import { useAuth } from '../auth/AuthContext';

export function HospitalReferenceCatalogPage() {
  const { hasPermission } = useAuth();
  const canView = hasPermission('users.manage', 'security.manage', 'integrations.manage');

  const [summary, setSummary] = useState<HospitalReferenceCatalogSummaryDto[]>([]);
  const [activeType, setActiveType] = useState<HospitalReferenceCatalogType>(1);
  const [items, setItems] = useState<HospitalReferenceCatalogItemDto[]>([]);
  const [groupFilter, setGroupFilter] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!canView) return;
    api.getHospitalCatalogSummary()
      .then(setSummary)
      .catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar resumo'))
      .finally(() => setLoading(false));
  }, [canView]);

  useEffect(() => {
    if (!canView) return;
    setLoading(true);
    api.getHospitalCatalogItems(activeType, {
      group: groupFilter || undefined,
      search: search.trim() || undefined,
    })
      .then(setItems)
      .catch((e) => setError(e instanceof Error ? e.message : 'Erro ao carregar itens'))
      .finally(() => setLoading(false));
  }, [canView, activeType, groupFilter, search]);

  const groups = useMemo(() => {
    const set = new Set<string>();
    for (const item of items) {
      if (item.parentGroup) set.add(item.parentGroup);
    }
    return [...set].sort((a, b) => a.localeCompare(b, 'pt-BR'));
  }, [items]);

  const activeSummary = summary.find((s) => s.catalogType === activeType);
  const groupedItems = useMemo(() => {
    const map = new Map<string, HospitalReferenceCatalogItemDto[]>();
    for (const item of items) {
      const key = item.parentGroup ?? '(Sem grupo)';
      const list = map.get(key) ?? [];
      list.push(item);
      map.set(key, list);
    }
    return [...map.entries()].sort(([a], [b]) => a.localeCompare(b, 'pt-BR'));
  }, [items]);

  if (!canView) {
    return (
      <div className="connect-page">
        <PageHeader
          eyebrow="Configurações"
          title="Catálogo Hospitalar"
          subtitle="Referência ERP — tipos de usuário, setores, exames, menu e perfis."
        />
        <div className="alert alert-error" style={{ marginTop: 16 }}>
          Você não tem permissão para visualizar o catálogo hospitalar.
        </div>
      </div>
    );
  }

  return (
    <div className="connect-page">
      <PageHeader
        eyebrow="Configurações"
        title="Catálogo Hospitalar"
        subtitle="Referência ERP — tipos de usuário, setores, alas, exames, menu, permissões e perfis prontos."
      />

      <ModuleNav basePath="/configuracoes" tabs={configTabs} />

      {error ? <div className="alert alert-error" style={{ marginTop: 12 }}>{error}</div> : null}

      <div style={{ display: 'grid', gridTemplateColumns: 'minmax(220px, 280px) 1fr', gap: 16, marginTop: 16 }}>
        <aside className="card-panel appt-panel" style={{ alignSelf: 'start' }}>
          <div className="card-panel-header">Tipos de catálogo</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <ul className="bi-progress-list" style={{ listStyle: 'none', margin: 0, padding: 0 }}>
              {summary.map((row) => (
                <li key={row.catalogType}>
                  <button
                    type="button"
                    className={`btn btn-sm ${activeType === row.catalogType ? '' : 'btn-secondary'}`}
                    style={{ width: '100%', textAlign: 'left', marginBottom: 6, justifyContent: 'space-between', display: 'flex' }}
                    onClick={() => { setActiveType(row.catalogType); setGroupFilter(''); }}
                  >
                    <span>{row.label}</span>
                    <span style={{ opacity: 0.7 }}>{row.itemCount}</span>
                  </button>
                </li>
              ))}
            </ul>
          </div>
        </aside>

        <div className="card-panel appt-panel">
          <div className="card-panel-header">
            {hospitalReferenceCatalogTypeLabels[activeType] ?? 'Catálogo'}
            {activeSummary ? (
              <span style={{ fontWeight: 400, marginLeft: 8, color: 'var(--muted)' }}>
                ({activeSummary.itemCount} itens · {activeSummary.groupCount} grupos)
              </span>
            ) : null}
          </div>
          <div className="card-panel-body">
            <div style={{ display: 'flex', flexWrap: 'wrap', gap: 12, marginBottom: 16 }}>
              <div className="form-field" style={{ flex: '1 1 240px', margin: 0 }}>
                <label>Buscar</label>
                <input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="Nome, código ou grupo…"
                />
              </div>
              {groups.length > 0 ? (
                <div className="form-field" style={{ flex: '0 1 220px', margin: 0 }}>
                  <label>Grupo</label>
                  <select value={groupFilter} onChange={(e) => setGroupFilter(e.target.value)}>
                    <option value="">Todos</option>
                    {groups.map((g) => (
                      <option key={g} value={g}>{g}</option>
                    ))}
                  </select>
                </div>
              ) : null}
            </div>

            {loading ? (
              <p style={{ color: 'var(--muted)' }}>Carregando catálogo…</p>
            ) : groupedItems.length === 0 ? (
              <p style={{ color: 'var(--muted)' }}>Nenhum item encontrado.</p>
            ) : (
              groupedItems.map(([group, groupItems]) => (
                <div key={group} style={{ marginBottom: 20 }}>
                  <h4 style={{ margin: '0 0 8px', fontSize: 14, color: 'var(--muted)' }}>{group}</h4>
                  <table className="data-table">
                    <thead>
                      <tr>
                        <th style={{ width: 120 }}>Código</th>
                        <th>Nome</th>
                        <th style={{ width: 80 }}>Ordem</th>
                      </tr>
                    </thead>
                    <tbody>
                      {groupItems.map((item) => (
                        <tr key={item.code}>
                          <td><code>{item.code}</code></td>
                          <td>
                            {item.name}
                            {item.description ? (
                              <div style={{ fontSize: 12, color: 'var(--muted)', marginTop: 2 }}>{item.description}</div>
                            ) : null}
                          </td>
                          <td>{item.displayOrder}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ))
            )}

            <p className="form-hint" style={{ marginTop: 16 }}>
              Catálogo de referência para parametrização do ERP. Dados sincronizados na inicialização do sistema.
              {' · '}
              <Link to="/configuracoes/cadastros">Cadastros auxiliares</Link>
              {' · '}
              <Link to="/usuarios">Usuários e perfis</Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

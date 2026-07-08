import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { PageHeader } from '../components/PageHeader';
import { useAdministrationRoutes } from '../hooks/useAdministrationRoutes';

type Props = {
  embedded?: boolean;
};

export function AdministrationRoutesCatalogPage({ embedded }: Props) {
  const { routes, loading, error } = useAdministrationRoutes();
  const [search, setSearch] = useState('');

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return routes;
    return routes.filter((route) =>
      route.code.includes(q)
      || route.name.toLowerCase().includes(q)
      || route.abbreviation?.toLowerCase().includes(q),
    );
  }, [routes, search]);

  const body = (
    <>
      {!embedded ? (
        <PageHeader
          eyebrow="Catálogo clínico"
          title="Vias de administração"
          subtitle="Catálogo MADRE com 33 vias padronizadas para prescrição, dispensação e faturamento."
        />
      ) : null}

      {error ? <div className="alert alert-error">{error}</div> : null}

      <div className={embedded ? '' : 'card-panel appt-panel'} style={{ marginTop: embedded ? 0 : 16 }}>
        {!embedded ? (
          <div className="card-panel-header">Referência de vias</div>
        ) : null}
        <div className="card-panel-body">
          <div className="form-field" style={{ maxWidth: 420, marginBottom: 16 }}>
            <label>Buscar</label>
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Código, nome ou sigla (ex.: IV, oral)…"
            />
          </div>

          {loading ? (
            <p style={{ color: 'var(--muted)' }}>Carregando catálogo…</p>
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Via</th>
                  <th>Sigla</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((route) => (
                  <tr key={route.code}>
                    <td>{route.code}</td>
                    <td>{route.name}</td>
                    <td><strong>{route.abbreviation ?? '—'}</strong></td>
                  </tr>
                ))}
                {filtered.length === 0 ? (
                  <tr>
                    <td colSpan={3} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                      Nenhuma via encontrada.
                    </td>
                  </tr>
                ) : null}
              </tbody>
            </table>
          )}

          <p className="form-hint" style={{ marginTop: 16 }}>
            Utilizado em prescrições do PEP e no cadastro Feegow do paciente.
            {' '}
            <Link to="/pep/prescricao">Ir para prescrição</Link>
            {' · '}
            <Link to="/configuracoes/cadastros">Cadastros auxiliares</Link>
          </p>
        </div>
      </div>
    </>
  );

  if (embedded) return body;
  return <div className="connect-page">{body}</div>;
}

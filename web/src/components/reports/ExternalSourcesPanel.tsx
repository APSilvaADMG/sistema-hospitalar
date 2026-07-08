import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  EXTERNAL_SOURCES,
  countByStatus,
  type ExternalSourceStatus,
} from '../../data/externalSourcesCatalog';
import { getFunctionalLabelForCode } from '../../data/reportFunctionalGroups';

const STATUS_LABEL: Record<ExternalSourceStatus, string> = {
  integrated: 'Integrado',
  partial: 'Parcial',
  reference: 'Referência',
  planned: 'Planejado',
};

const STATUS_CLASS: Record<ExternalSourceStatus, string> = {
  integrated: 'badge-success',
  partial: 'badge-warning',
  reference: 'badge-info',
  planned: 'badge-muted',
};

export function ExternalSourcesPanel({ reportNames }: { reportNames?: Record<string, string> }) {
  const [expanded, setExpanded] = useState(false);
  const [filter, setFilter] = useState('');

  const stats = useMemo(
    () => ({
      integrated: countByStatus('integrated'),
      partial: countByStatus('partial'),
      reference: countByStatus('reference'),
      planned: countByStatus('planned'),
    }),
    [],
  );

  function reportLabel(code: string) {
    return reportNames?.[code] ?? getFunctionalLabelForCode(code) ?? code;
  }

  const filtered = useMemo(() => {
    const term = filter.trim().toLowerCase();
    if (!term) return EXTERNAL_SOURCES;
    return EXTERNAL_SOURCES.filter(
      (s) =>
        s.name.toLowerCase().includes(term) ||
        s.description.toLowerCase().includes(term) ||
        s.features.some(
          (f) =>
            f.external.toLowerCase().includes(term) ||
            f.localModule.toLowerCase().includes(term),
        ),
    );
  }, [filter]);

  return (
    <section className="external-sources-panel">
      <button
        type="button"
        className="external-sources-toggle"
        onClick={() => setExpanded((v) => !v)}
        aria-expanded={expanded}
      >
        <i className={`icon-${expanded ? 'chevron-up' : 'chevron-down'}`} aria-hidden />
        {' '}
        Integrações open source (HospitalRun, sitrep, EpiModel…)
        {' '}
        <span className="external-sources-stats">
          {stats.integrated} integradas · {stats.partial} parciais · {stats.planned} planejadas
        </span>
      </button>

      {expanded ? (
        <div className="external-sources-body">
          <p className="external-sources-intro">
            Features inspiradas em repositórios externos, reimplementadas na stack .NET + React.
            Clone as referências com{' '}
            <code>Diversos/external-repos/sync-external-repos.ps1</code>.
          </p>
          <input
            type="search"
            className="external-sources-search"
            placeholder="Filtrar fonte ou módulo…"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          />
          <div className="external-sources-grid">
            {filtered.map((source) => (
              <article key={source.id} className="external-source-card">
                <header>
                  <h3>
                    <a href={source.url} target="_blank" rel="noreferrer">
                      {source.name}
                    </a>
                  </h3>
                  <span className="external-source-meta">
                    {source.license} · {source.stack}
                  </span>
                </header>
                <p>{source.description}</p>
                <ul className="external-feature-list">
                  {source.features.map((f) => (
                    <li key={`${source.id}-${f.external}`}>
                      <span className={`badge ${STATUS_CLASS[f.status]}`}>
                        {STATUS_LABEL[f.status]}
                      </span>
                      <strong>{f.external}</strong>
                      {' → '}
                      {f.localPath ? (
                        <Link to={f.localPath}>{f.localModule}</Link>
                      ) : (
                        f.localModule
                      )}
                      {f.reportCodes?.length ? (
                        <span className="external-report-links">
                          {' · '}
                          {f.reportCodes.map((code, i) => (
                            <span key={code}>
                              {i > 0 ? ', ' : ''}
                              <Link to={`/relatorios?q=${encodeURIComponent(reportLabel(code))}`}>
                                {reportLabel(code)}
                              </Link>
                            </span>
                          ))}
                        </span>
                      ) : null}
                      {f.notes ? <em className="external-feature-note">{f.notes}</em> : null}
                    </li>
                  ))}
                </ul>
              </article>
            ))}
          </div>
        </div>
      ) : null}
    </section>
  );
}

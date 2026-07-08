import type { ReactNode } from 'react';

export type WorkspaceStat = {
  label: string;
  value: string | number;
  tone?: 'default' | 'warning' | 'success' | 'danger';
};

type Props = {
  title: string;
  hint?: string;
  stats: WorkspaceStat[];
  footerHint?: string;
  children?: ReactNode;
};

function toneClass(tone?: WorkspaceStat['tone']) {
  if (tone === 'warning') return 'is-warning';
  if (tone === 'success') return 'is-success';
  if (tone === 'danger') return 'is-danger';
  return '';
}

/**
 * Resumo operacional padrão Bayanno para hubs com workspace de paciente.
 */
export function WorkspaceHubOverview({ title, hint, stats, footerHint, children }: Props) {
  return (
    <div className="tab-content">
      <div className="tab-pane box active">
        <div className="bayanno-panel-head">
          <span className="title">
            <i className="icon-home" aria-hidden />
            {' '}
            {title}
          </span>
          {hint ? <span className="bayanno-panel-hint">{hint}</span> : null}
        </div>
        <table className="bayanno-stats-table">
          <thead>
            <tr>
              {stats.map((s) => (
                <th key={s.label}>{s.label}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            <tr>
              {stats.map((s) => (
                <td key={s.label} className={toneClass(s.tone)}>
                  {s.value}
                </td>
              ))}
            </tr>
          </tbody>
        </table>
        {footerHint ? <p className="bayanno-inline-hint">{footerHint}</p> : null}
        {children}
      </div>
    </div>
  );
}

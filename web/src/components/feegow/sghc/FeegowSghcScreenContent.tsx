import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import type { BayannoScreen } from '../../../data/bayanno';
import { bayannoPhrase } from '../../../data/bayanno';
import { useSghcColumns, useSghcScreenData } from '../../../hooks/useSghcScreenData';
import { FeegowSghcDashboard } from './FeegowSghcDashboard';
import { FeegowSghcDataTable } from './FeegowSghcDataTable';
import { FeegowSghcScreenForm } from './FeegowSghcScreenForm';
import { parseSghcPath } from './feegowSghcNav';
import { resolveSghcDataModule } from './sghcScreenData';
import { resolveSghcFormMode } from './sghcScreenForms';

type Props = {
  screen: BayannoScreen;
  pathname: string;
};

function inferActiveTab(screen: BayannoScreen, pathname: string): string {
  const parsed = parseSghcPath(pathname);
  const sub = parsed?.subAction?.toLowerCase() ?? '';

  if (sub.includes('operation') || sub.includes('birth') || sub.includes('death')) {
    return 'list';
  }

  if (screen.tabs.length === 0) return 'list';

  const listTab = screen.tabs.find((tab) => tab.id === 'list');
  return listTab?.id ?? screen.tabs[0].id;
}

function tabKind(tabId: string): 'list' | 'form' {
  if (tabId === 'list' || tabId.includes('list')) return 'list';
  return 'form';
}

export function FeegowSghcScreenContent({ screen, pathname }: Props) {
  const [activeTab, setActiveTab] = useState(() => inferActiveTab(screen, pathname));

  const parsed = parseSghcPath(pathname);
  const subActionLabel = parsed?.subAction
    ? bayannoPhrase(parsed.subAction.replace(/\//g, '_'))
    : null;

  const table = screen.tables[0];
  const bayannoColumns = table?.columns ?? [];
  const dataModule = resolveSghcDataModule(screen.route);
  const listTabActive = tabKind(activeTab) === 'list';
  const {
    module,
    columns: apiColumns,
    rows,
    summary,
    moduleLink: dataModuleLink,
    loading,
    error,
    reload,
  } = useSghcScreenData(screen.route, listTabActive && dataModule !== null);
  const displayColumns = useSghcColumns(module ?? dataModule, bayannoColumns);
  const showApiTable = listTabActive && dataModule !== null && displayColumns.length > 0;
  const formMode = resolveSghcFormMode(activeTab);
  const showConnectedForm = formMode !== null && dataModule !== null;
  const showLegacyForm = tabKind(activeTab) === 'form' && !showConnectedForm
    && (bayannoColumns.length === 0 && screen.tabs.length > 0);

  const formFields = useMemo(() => {
    const keys = screen.phraseKeys.filter((key) => ![
      'edit', 'delete', 'option', 'status', 'paid', 'unpaid',
    ].includes(key));
    return keys.slice(0, 8);
  }, [screen.phraseKeys]);

  if (screen.kind === 'dashboard') {
    return <FeegowSghcDashboard screen={screen} />;
  }

  if (screen.kind === 'layout') {
    return (
      <div className="feegow-sghc-layout-info">
        <header className="feegow-sghc-screen-header">
          <div>
            <p className="feegow-sghc-screen-kicker">Layout SGHC</p>
            <h1>{screen.title}</h1>
          </div>
        </header>
        <div className="feegow-patient-card">
          <p>
            Tela de estrutura <code>{screen.file}</code> do Bayanno HMS.
            No sistema Feegow ela é coberta pelo shell global (topbar, sidebar e rodapé).
          </p>
          {screen.moduleLink ? (
            <Link to={screen.moduleLink} className="btn btn-sm">
              Ir para módulo equivalente
            </Link>
          ) : null}
        </div>
      </div>
    );
  }

  return (
    <div className="feegow-sghc-operational">
      <header className="feegow-sghc-screen-header">
        <div>
          <p className="feegow-sghc-screen-kicker">
            SGHC · {screen.role}
            {subActionLabel ? ` · ${subActionLabel}` : ''}
          </p>
          <h1>{screen.title}</h1>
          <p className="feegow-sghc-screen-meta">{screen.route}</p>
        </div>
        {screen.moduleLink ? (
          <Link to={screen.moduleLink} className="btn btn-secondary btn-sm feegow-sghc-module-link">
            Abrir módulo completo
          </Link>
        ) : null}
      </header>

      {screen.tabs.length > 0 ? (
        <div className="feegow-sghc-tabs" role="tablist" aria-label="Abas da tela">
          {screen.tabs.map((tab) => (
            <button
              key={tab.id}
              type="button"
              role="tab"
              aria-selected={activeTab === tab.id}
              className={`feegow-sghc-tab${activeTab === tab.id ? ' is-active' : ''}`}
              onClick={() => setActiveTab(tab.id)}
            >
              {tab.label}
            </button>
          ))}
        </div>
      ) : null}

      {showApiTable ? (
        <FeegowSghcDataTable
          columns={displayColumns.length > 0 ? displayColumns : apiColumns}
          rows={rows}
          loading={loading}
          error={error}
          summary={summary}
          moduleLink={dataModuleLink ?? screen.moduleLink}
          onRefresh={reload}
        />
      ) : null}

      {listTabActive && !showApiTable && bayannoColumns.length > 0 ? (
        <div className="feegow-patient-card feegow-sghc-table-card">
          <div className="table-wrap">
            <table className="data-table feegow-sghc-table">
              <thead>
                <tr>
                  {bayannoColumns.map((col) => (
                    <th key={col.labelKey}>{col.label}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                <tr className="feegow-sghc-empty-row">
                  <td colSpan={bayannoColumns.length}>
                    Nenhum registro carregado. Use &quot;Abrir módulo completo&quot; para operação com dados reais
                    {screen.moduleLink ? (
                      <> ou <Link to={screen.moduleLink}>{bayannoPhrase('dashboard')}</Link></>
                    ) : null}.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      ) : null}

      {showConnectedForm && formMode ? (
        <FeegowSghcScreenForm
          module={dataModule!}
          mode={formMode}
          route={screen.route}
          moduleLink={dataModuleLink ?? screen.moduleLink}
          onSaved={reload}
        />
      ) : null}

      {showLegacyForm ? (
        <div className="feegow-patient-card feegow-sghc-form-card">
          <form className="feegow-sghc-form" onSubmit={(event) => event.preventDefault()}>
            {formFields.map((field) => (
              <label key={field} className="feegow-sghc-field">
                <span>{bayannoPhrase(field)}</span>
                <input type="text" placeholder={bayannoPhrase(field)} />
              </label>
            ))}
            <div className="feegow-sghc-form-actions">
              {screen.moduleLink ? (
                <Link to={screen.moduleLink} className="btn btn-sm">Abrir módulo completo</Link>
              ) : (
                <button type="submit" className="btn btn-sm">Salvar</button>
              )}
              <button type="button" className="btn btn-secondary btn-sm">Cancelar</button>
            </div>
          </form>
        </div>
      ) : null}

      {bayannoColumns.length === 0 && screen.tabs.length === 0 && !showApiTable ? (
        <div className="feegow-patient-card">
          <p>
            Tela operacional do perfil <strong>{screen.role}</strong>.
            {screen.moduleLink
              ? ' Os dados e fluxos completos estão no módulo integrado do sistema.'
              : ' Configure os parâmetros em Configurações quando necessário.'}
          </p>
          {screen.phraseKeys.length > 0 ? (
            <ul className="feegow-sghc-phrase-list">
              {screen.phraseKeys.slice(0, 12).map((key) => (
                <li key={key}>{bayannoPhrase(key)}</li>
              ))}
            </ul>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

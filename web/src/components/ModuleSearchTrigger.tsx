import { useOpenModuleSearch } from './ModuleSearchProvider';
import { NavIcon } from './NavIcon';

export function ModuleSearchTrigger() {
  const openSearch = useOpenModuleSearch();

  return (
    <>
      <div className="global-search-wrap desktop-only">
        <button
          type="button"
          className="module-search-trigger"
          onClick={openSearch}
          aria-label="Buscar pacientes e módulos"
        >
          <svg className="module-search-trigger-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
            <circle cx="11" cy="11" r="7" />
            <path d="M20 20l-3-3" />
          </svg>
          <span className="module-search-trigger-label">Pesquise pacientes, módulos e telas…</span>
          <kbd className="module-search-trigger-kbd">Ctrl+K</kbd>
        </button>
        <button
          type="button"
          className="global-search-submit"
          onClick={openSearch}
          aria-label="Buscar"
          title="Buscar"
        >
          <NavIcon name="search" />
        </button>
      </div>

      <button
        type="button"
        className="topbar-icon-btn mobile-only"
        onClick={openSearch}
        aria-label="Buscar"
        title="Buscar"
      >
        <NavIcon name="search" />
      </button>
    </>
  );
}

import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { loadHospitalParams } from '../config/clinicOnDoctorProfile';
import {
  buildModuleSearchIndex,
  searchModules,
  type ModuleSearchItem,
} from '../navigation/moduleSearchIndex';

type Props = {
  open: boolean;
  onClose: () => void;
};

export function ModuleSearchDialog({ open, onClose }: Props) {
  const navigate = useNavigate();
  const { hasRole, hasPermission } = useAuth();
  const inputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);

  const isStaff = !hasRole('Patient');
  const isAdminOrReception = hasRole('Admin', 'Reception')
    || hasPermission('patients.create', 'billing.write');
  const isAdmin = hasRole('Admin')
    || hasPermission('users.manage', 'security.manage');
  const hasSecurityLgpd = hasPermission(
    'audit.read',
    'security.manage',
    'lgpd.manage',
    'lgpd.consent.manage',
    'lgpd.subject_requests',
    'incidents.manage',
  );

  const index = useMemo(() => {
    if (!isStaff) return [];
    return buildModuleSearchIndex({
      isStaff,
      isAdminOrReception,
      isAdmin,
      hasSecurityLgpd,
      unreadCount: 0,
      hasPermission,
      modules: loadHospitalParams().modules,
    });
  }, [isStaff, isAdminOrReception, isAdmin, hasSecurityLgpd, hasPermission]);

  const results = useMemo(
    () => searchModules(index, query, 14),
    [index, query],
  );

  useEffect(() => {
    if (!open) {
      setQuery('');
      setActiveIndex(0);
      return;
    }
    const t = window.setTimeout(() => inputRef.current?.focus(), 0);
    return () => window.clearTimeout(t);
  }, [open]);

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  useEffect(() => {
    if (!open) return;
    function onKey(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        e.preventDefault();
        onClose();
        return;
      }
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setActiveIndex((i) => Math.min(i + 1, Math.max(results.length - 1, 0)));
      }
      if (e.key === 'ArrowUp') {
        e.preventDefault();
        setActiveIndex((i) => Math.max(i - 1, 0));
      }
      if (e.key === 'Enter' && results[activeIndex]) {
        e.preventDefault();
        goTo(results[activeIndex]);
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [open, results, activeIndex, onClose]);

  function goTo(item: ModuleSearchItem) {
    const [path, search] = item.path.split('?');
    navigate(search ? { pathname: path, search: `?${search}` } : path);
    onClose();
  }

  if (!open || !isStaff) return null;

  return (
    <div className="module-search-overlay" onClick={onClose} role="presentation">
      <div
        className="module-search-panel"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-label="Buscar módulo"
      >
        <div className="module-search-input-wrap">
          <svg className="module-search-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden>
            <circle cx="11" cy="11" r="7" />
            <path d="M20 20l-3-3" />
          </svg>
          <input
            ref={inputRef}
            type="search"
            className="module-search-input"
            placeholder="Buscar módulo, tela ou aba…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            autoComplete="off"
            spellCheck={false}
          />
          <kbd className="module-search-kbd">esc</kbd>
        </div>

        <div className="module-search-results">
          {results.length === 0 ? (
            <p className="module-search-empty">Nenhum módulo encontrado para &quot;{query}&quot;.</p>
          ) : (
            <ul role="listbox">
              {results.map((item, i) => (
                <li key={item.id}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={i === activeIndex}
                    className={`module-search-result${i === activeIndex ? ' active' : ''}`}
                    onMouseEnter={() => setActiveIndex(i)}
                    onClick={() => goTo(item)}
                  >
                    <span className="module-search-result-main">
                      <strong>{item.label}</strong>
                      {item.parentLabel && (
                        <span className="module-search-result-parent">em {item.parentLabel}</span>
                      )}
                    </span>
                    <span className="module-search-result-meta">
                      {item.section}
                      {item.kind === 'tab' && ' · aba'}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        <div className="module-search-footer">
          <span><kbd>↑</kbd> <kbd>↓</kbd> navegar</span>
          <span><kbd>Enter</kbd> abrir</span>
          <span><kbd>Ctrl</kbd>+<kbd>K</kbd> buscar</span>
        </div>
      </div>
    </div>
  );
}

/** Atalho global Ctrl+K / Cmd+K */
export function useModuleSearchShortcut(onOpen: () => void) {
  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        onOpen();
      }
    }
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onOpen]);
}

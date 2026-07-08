import { NavLink, useLocation } from 'react-router-dom';
import { useOpenModuleSearch } from '../../ModuleSearchProvider';
import {
  FEEGOW_INVENTORY_CONFIG,
  FEEGOW_INVENTORY_ITEM_TYPES,
  feegowInventoryInsertPath,
  feegowInventoryListPath,
  isInventoryInsertRoute,
  isInventoryListRoute,
  parseInventoryTipo,
  resolveInventoryConfigId,
  type FeegowInventoryItemType,
} from './feegowInventoryNav';

export const FEEGOW_INVENTORY_SIDEBAR_HOST_ID = 'feegow-inventory-sidebar-host';

function isItemTypeActive(pathname: string, search: string, tipo: FeegowInventoryItemType): boolean {
  const configId = resolveInventoryConfigId(pathname);
  if (configId) return false;
  if (isInventoryListRoute(pathname)) {
    return parseInventoryTipo(new URLSearchParams(search).get('tipo')) === tipo;
  }
  if (isInventoryInsertRoute(pathname)) {
    return parseInventoryTipo(new URLSearchParams(search).get('tipo')) === tipo;
  }
  return false;
}

export function FeegowInventorySidebar() {
  const { pathname, search } = useLocation();
  const openSearch = useOpenModuleSearch();
  const activeConfig = resolveInventoryConfigId(pathname);

  return (
    <div className="feegow-inventory-sidebar">
      <div className="feegow-quick-search feegow-inventory-search">
        <span className="feegow-search-icon" aria-hidden>🔍</span>
        <button type="button" className="feegow-search-input" onClick={openSearch}>
          Busca rápida…
        </button>
      </div>

      <p className="feegow-inventory-section-label">TIPOS DE ITENS</p>
      <nav className="feegow-inventory-nav" aria-label="Tipos de itens">
        {FEEGOW_INVENTORY_ITEM_TYPES.map((item) => (
          <NavLink
            key={item.id}
            to={feegowInventoryInsertPath(item.id)}
            className={() =>
              `feegow-inventory-nav-item${isItemTypeActive(pathname, search, item.id) ? ' is-active' : ''}`
            }
          >
            <span className="feegow-inventory-nav-icon" aria-hidden>{item.icon}</span>
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>

      <p className="feegow-inventory-section-label">OPERAÇÕES</p>
      <nav className="feegow-inventory-nav" aria-label="Operações de estoque">
        <NavLink
          to={feegowInventoryListPath('geral')}
          className={() =>
            `feegow-inventory-nav-item${isInventoryListRoute(pathname) ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>📋</span>
          <span>Listar produtos</span>
        </NavLink>
        <NavLink
          to="/estoque/dashboard"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '') === '/estoque/dashboard' ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>📊</span>
          <span>Dashboard</span>
        </NavLink>
        <NavLink
          to="/estoque/entrada"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '') === '/estoque/entrada' ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>📥</span>
          <span>Entrada NF</span>
        </NavLink>
        <NavLink
          to="/estoque/saida"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '') === '/estoque/saida' ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>📤</span>
          <span>Saída</span>
        </NavLink>
        <NavLink
          to="/estoque/lotes"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '') === '/estoque/lotes' ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>🏷️</span>
          <span>Lotes</span>
        </NavLink>
        <NavLink
          to="/estoque/movimentacoes"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '') === '/estoque/movimentacoes' ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>⇄</span>
          <span>Movimentações</span>
        </NavLink>
        <NavLink
          to="/estoque/requisicoes"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '').startsWith('/estoque/requisicoes') ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>📝</span>
          <span>Requisições</span>
        </NavLink>
        <NavLink
          to="/estoque/kits"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '').startsWith('/estoque/kits') ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>🧰</span>
          <span>Kits de produtos</span>
        </NavLink>
        <NavLink
          to="/estoque/config/medicamento-convenio"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '').startsWith('/estoque/config/medicamento-convenio') ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>💊</span>
          <span>Medicamento por convênio</span>
        </NavLink>
        <NavLink
          to="/estoque/farmacia-ala"
          className={() =>
            `feegow-inventory-nav-item${pathname.split('?')[0].replace(/\/$/, '') === '/estoque/farmacia-ala' ? ' is-active' : ''}`
          }
        >
          <span className="feegow-inventory-nav-icon" aria-hidden>💊</span>
          <span>Farmácia por Ala</span>
        </NavLink>
      </nav>

      <p className="feegow-inventory-section-label">CONFIGURAÇÕES</p>
      <nav className="feegow-inventory-nav feegow-inventory-config-nav" aria-label="Configurações de estoque">
        {FEEGOW_INVENTORY_CONFIG.map((item) => (
          <NavLink
            key={item.id}
            to={item.path}
            className={() =>
              `feegow-inventory-nav-item feegow-inventory-config-item${activeConfig === item.id ? ' is-active' : ''}`
            }
          >
            <span>{item.label}</span>
          </NavLink>
        ))}
      </nav>
    </div>
  );
}

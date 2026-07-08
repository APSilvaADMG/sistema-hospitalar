import { NavLink } from 'react-router-dom';
import {
  FEEGOW_PRODUCT_TABS,
  feegowInventoryInsertPath,
  type FeegowInventoryItemType,
  type FeegowProductTab,
} from './feegowInventoryNav';

type Props = {
  tipo: FeegowInventoryItemType;
  productId?: string;
  activeTab: FeegowProductTab;
};

export function FeegowProductSubSidebar({ tipo, productId, activeTab }: Props) {
  return (
    <div className="feegow-inventory-sidebar feegow-product-sub-sidebar">
      <p className="feegow-inventory-section-label">OPÇÕES DE CONFIGURAÇÕES</p>
      <nav className="feegow-inventory-nav">
        {FEEGOW_PRODUCT_TABS.map((tab) => {
          const className = `feegow-inventory-nav-item${activeTab === tab.id ? ' is-active' : ''}`;
          return (
            <NavLink
              key={tab.id}
              to={feegowInventoryInsertPath(tipo, { id: productId, aba: tab.id })}
              className={className}
            >
              <span className="feegow-inventory-nav-icon" aria-hidden>{tab.icon}</span>
              <span>{tab.label}</span>
            </NavLink>
          );
        })}
      </nav>
    </div>
  );
}

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import './index.css';
import './styles/theme-variants.css';
import './styles/bayanno-theme.css';
import './styles/bayanno-php-screens.css';
import './styles/bayanno-layout.css';
import './styles/bayanno-pages.css';
import './styles/bayanno-reports.css';
import './styles/bayanno-reports-page.css';
import './styles/bayanno-guides-page.css';
import './styles/login-screen.css';
import './styles/bayanno-friendly-screens.css';
import './styles/bayanno-global-ui.css';
import './styles/feegow-theme.css';
import './styles/feegow-global-ui.css';
import './styles/feegow-login-screen.css';
import './styles/feegow-shell.css';
import './styles/feegow-dashboard.css';
import './styles/operational-dashboard.css';
import './styles/feegow-help.css';
import './styles/feegow-tv.css';
import './styles/feegow-daily-agenda.css';
import './styles/feegow-espera.css';
import './styles/feegow-patients.css';
import './styles/feegow-products.css';
import './styles/feegow-inventory.css';
import './styles/feegow-sghc.css';
import './styles/feegow-connect.css';
import './styles/feegow-finance.css';
import './styles/feegow-table-pagination.css';
import './styles/feegow-stock-requisition.css';
import './styles/feegow-warehouse.css';
import './styles/agenda-timeline.css';
import { AppearanceProvider } from './theme/AppearanceProvider';
import { hydrateAppearance } from './theme/appearanceConfig';
import { initOfflineSync } from './offline/pepOfflineSync';

hydrateAppearance();

if ('serviceWorker' in navigator && import.meta.env.PROD) {
  navigator.serviceWorker.register('/sw.js').catch(console.error);
}

initOfflineSync();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AppearanceProvider>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </AppearanceProvider>
  </StrictMode>,
);

import { Navigate, useLocation } from 'react-router-dom';

import { parseFeegowProductRoute } from '../components/feegow/products/feegowProductForm';

import { FeegowInventoryScreenLayout } from '../components/feegow/inventory/FeegowInventoryScreenLayout';

import {

  isInventoryLookupInsertRoute,

  resolveInventoryConfigId,

  resolveInventoryLookupConfigId,

} from '../components/feegow/inventory/feegowInventoryNav';

import { FeegowInventoryLookupInsertPage } from './FeegowInventoryLookupInsertPage';

import { FeegowInventoryLookupListPage } from './FeegowInventoryLookupListPage';

import { FeegowMedicationInsurancePage } from './FeegowMedicationInsurancePage';

import { FeegowProductInsertPage } from './FeegowProductInsertPage';

import { FeegowProductKitInsertPage } from './FeegowProductKitInsertPage';

import { FeegowProductKitListPage } from './FeegowProductKitListPage';

import { FeegowProductListPage } from './FeegowProductListPage';

import { FeegowStockRequisitionInsertPage } from './FeegowStockRequisitionInsertPage';

import { FeegowStockRequisitionListPage } from './FeegowStockRequisitionListPage';

import { FeegowWarehouseDashboardPage } from './FeegowWarehouseDashboardPage';

import { FeegowStockReceiptPage } from './FeegowStockReceiptPage';

import { FeegowStockIssuePage } from './FeegowStockIssuePage';

import { FeegowStockLotsPage } from './FeegowStockLotsPage';

import { FeegowStockMovementsPage } from './FeegowStockMovementsPage';

import { FeegowWardPharmacyPage } from './FeegowWardPharmacyPage';



export function FeegowInventoryWorkspacePage() {

  const { pathname } = useLocation();

  const path = pathname.split('?')[0].replace(/\/$/, '') || '/';

  if (path === '/estoque/farmacia-ala') {

    return <FeegowWardPharmacyPage />;

  }

  if (path === '/estoque/lista') {
    return <Navigate to="/estoque/listar?tipo=geral" replace />;
  }

  if (path === '/estoque/dashboard') {
    return <FeegowWarehouseDashboardPage />;
  }

  if (path === '/estoque/entrada') {
    return <FeegowStockReceiptPage />;
  }

  if (path === '/estoque/saida') {
    return <FeegowStockIssuePage />;
  }

  if (path === '/estoque/lotes') {
    return <FeegowStockLotsPage />;
  }

  if (path === '/estoque/movimentacoes') {
    return <FeegowStockMovementsPage />;
  }

  const route = parseFeegowProductRoute(pathname);

  const configId = resolveInventoryConfigId(pathname);

  const lookupConfigId = resolveInventoryLookupConfigId(pathname);



  if (!route && !configId) {

    return <Navigate to="/estoque/listar?tipo=geral" replace />;

  }



  if (lookupConfigId) {

    if (isInventoryLookupInsertRoute(pathname)) {

      return <FeegowInventoryLookupInsertPage configId={lookupConfigId} />;

    }

    return <FeegowInventoryLookupListPage configId={lookupConfigId} />;

  }



  if (configId === 'medicamento-convenio') {

    return <FeegowMedicationInsurancePage />;

  }



  if (route === 'list') {

    return <FeegowProductListPage />;

  }



  if (route === 'kits-list') {

    return <FeegowProductKitListPage />;

  }



  if (route === 'kits-insert') {

    return <FeegowProductKitInsertPage />;

  }



  if (route === 'requisitions-list') {

    return <FeegowStockRequisitionListPage />;

  }



  if (route === 'requisitions-insert') {

    return <FeegowStockRequisitionInsertPage />;

  }



  if (route === 'config') {

    return (

      <FeegowInventoryScreenLayout>

        <div className="feegow-inventory-empty-panel">Configuração não encontrada.</div>

      </FeegowInventoryScreenLayout>

    );

  }



  return <FeegowProductInsertPage />;

}


import { useEffect, useMemo, useState } from 'react';

import { Link } from 'react-router-dom';

import {

  api,

  type PharmacyDispensingDto,

  type ProductDto,

  type StockMovementDto,

} from '../../api/client';

import { ModuleNav } from '../../components/ModuleNav';

import {

  getPharmacyGroupBySlug,

  PHARMACY_FUNCTIONAL_GROUPS,

  type PharmacyFunctionalGroup,

} from '../../data/pharmacyFunctionalGroups';

import { pharmacyTabs } from '../../navigation/moduleSections';

import { useModuleSection } from '../../navigation/useModuleSection';

import { formatBrDateTime } from '../../utils/dateUtils';

import { PharmacyPage } from '../PharmacyPage';
import { PharmacyBillingPage } from '../PharmacyBillingPage';



function daysUntil(dateIso: string): number {

  const target = new Date(dateIso);

  const now = new Date();

  return Math.ceil((target.getTime() - now.getTime()) / (1000 * 60 * 60 * 24));

}



function PharmacyHubOverview() {

  const { section } = useModuleSection('/farmacia');

  const sectionGroup = getPharmacyGroupBySlug(section || undefined);

  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(

    () => sectionGroup?.id ?? 'dispensacao',

  );

  const [dispensings, setDispensings] = useState<PharmacyDispensingDto[]>([]);

  const [lowStockProducts, setLowStockProducts] = useState<ProductDto[]>([]);

  const [expiringMovements, setExpiringMovements] = useState<StockMovementDto[]>([]);

  const [loading, setLoading] = useState(true);



  const activeGroup = useMemo(

    () => PHARMACY_FUNCTIONAL_GROUPS.find((g: PharmacyFunctionalGroup) => g.id === selectedGroupId) ?? null,

    [selectedGroupId],

  );



  useEffect(() => {

    const group = getPharmacyGroupBySlug(section || undefined);

    if (group) setSelectedGroupId(group.id);

  }, [section]);



  useEffect(() => {

    setLoading(true);

    Promise.all([

      api.getDispensings(),

      api.getProducts(undefined, true),

      api.getStockMovements(),

    ])

      .then(([disp, lowStock, movements]) => {

        setDispensings(disp);

        setLowStockProducts(lowStock);

        const expiring = movements

          .filter((m) => m.expiryDate && daysUntil(m.expiryDate) <= 30 && daysUntil(m.expiryDate) >= 0)

          .sort((a, b) => (a.expiryDate ?? '').localeCompare(b.expiryDate ?? ''));

        setExpiringMovements(expiring);

      })

      .catch(console.error)

      .finally(() => setLoading(false));

  }, []);



  return (

    <div className="page-content guides-bayanno-page">

      <ModuleNav basePath="/farmacia" tabs={pharmacyTabs} contextId="pharmacy" />



      <div className="box">

        <div className="box-content padded">

          <div className="guides-toolbar">

            <h2 style={{ margin: 0, flex: 1 }}>Farmácia e Estoque — Hub</h2>

            <Link to="/estoque" className="btn btn-secondary">Almoxarifado</Link>

            <Link to="/relatorios/estoque-farmacia" className="btn btn-secondary">Relatórios</Link>

          </div>



          <div className="guides-kpi-row">

            <div className="guides-kpi-card"><strong>{dispensings.length}</strong><span>Dispensações recentes</span></div>

            <div className="guides-kpi-card"><strong>{lowStockProducts.length}</strong><span>Estoque mínimo</span></div>

            <div className="guides-kpi-card"><strong>{expiringMovements.length}</strong><span>Vencendo em 30d</span></div>

          </div>



          <div className="guides-bayanno-layout">

            <nav className="guides-module-nav" aria-label="Grupos farmácia">

              <div className="guides-module-nav-head">Grupos funcionais</div>

              {PHARMACY_FUNCTIONAL_GROUPS.map((group: PharmacyFunctionalGroup) => (

                <button

                  key={group.id}

                  type="button"

                  className={`guides-module-btn${selectedGroupId === group.id ? ' active' : ''}`}

                  onClick={() => setSelectedGroupId(group.id)}

                >

                  {group.label}

                  <span className="guides-module-btn-desc">{group.description}</span>

                </button>

              ))}

            </nav>



            <div className="guides-main-panel">

              <p style={{ color: 'var(--muted)', marginTop: 0 }}>

                Resumo operacional de <strong>{activeGroup?.label ?? 'Farmácia'}</strong>.

                {loading ? ' Carregando dados…' : ' Use as abas do módulo para operação completa.'}

              </p>



              {selectedGroupId === 'dispensacao' && (

                <div className="guides-table-wrap">

                  <div className="guides-table-meta">

                    <span>{dispensings.length} dispensação(ões)</span>

                    {loading && <span className="guides-table-loading">Carregando…</span>}

                  </div>

                  <div className="guides-table-scroll">

                    <table className="guides-data-table">

                      <thead>

                        <tr>

                          <th>Data</th>

                          <th>Paciente</th>

                          <th>Produto</th>

                          <th>Qtd</th>

                          <th>Profissional</th>

                        </tr>

                      </thead>

                      <tbody>

                        {dispensings.slice(0, 25).map((d) => (

                          <tr key={d.id}>

                            <td>{formatBrDateTime(d.dispensedAt)}</td>

                            <td>{d.patientName}</td>

                            <td>{d.productName}</td>

                            <td>{d.quantity}</td>

                            <td>{d.professionalName ?? '—'}</td>

                          </tr>

                        ))}

                        {dispensings.length === 0 && !loading && (

                          <tr>

                            <td colSpan={5} className="guides-table-empty">Nenhuma dispensação.</td>

                          </tr>

                        )}

                      </tbody>

                    </table>

                  </div>

                  <div style={{ marginTop: 12 }}>

                    <Link to="/farmacia" className="btn btn-primary btn-sm">Abrir dispensação completa</Link>

                  </div>

                </div>

              )}



              {selectedGroupId === 'estoque' && (

                <div className="guides-table-wrap">

                  <div className="guides-table-meta">

                    <span>{lowStockProducts.length} produto(s) abaixo do mínimo</span>

                  </div>

                  <div className="guides-table-scroll">

                    <table className="guides-data-table">

                      <thead>

                        <tr><th>Produto</th><th>SKU</th><th>Saldo</th><th>Mínimo</th></tr>

                      </thead>

                      <tbody>

                        {lowStockProducts.slice(0, 25).map((p) => (

                          <tr key={p.id}>

                            <td>{p.name}</td>

                            <td>{p.sku}</td>

                            <td>{p.quantityOnHand}</td>

                            <td>{p.minimumStock}</td>

                          </tr>

                        ))}

                        {lowStockProducts.length === 0 && !loading && (

                          <tr><td colSpan={4} className="guides-table-empty">Nenhum produto em estoque mínimo.</td></tr>

                        )}

                      </tbody>

                    </table>

                  </div>

                  <div style={{ marginTop: 12 }}>

                    <Link to="/estoque" className="btn btn-primary btn-sm">Abrir almoxarifado</Link>

                  </div>

                </div>

              )}



              {selectedGroupId === 'validades' && (

                <div className="guides-table-wrap">

                  <div className="guides-table-meta">

                    <span>{expiringMovements.length} lote(s) vencendo em 30 dias</span>

                  </div>

                  <div className="guides-table-scroll">

                    <table className="guides-data-table">

                      <thead>

                        <tr><th>Produto</th><th>Lote</th><th>Validade</th><th>Dias</th></tr>

                      </thead>

                      <tbody>

                        {expiringMovements.slice(0, 25).map((m) => (

                          <tr key={m.id}>

                            <td>{m.productName}</td>

                            <td>{m.batchNumber ?? '—'}</td>

                            <td>{m.expiryDate?.slice(0, 10) ?? '—'}</td>

                            <td>{m.expiryDate ? daysUntil(m.expiryDate) : '—'}</td>

                          </tr>

                        ))}

                        {expiringMovements.length === 0 && !loading && (

                          <tr><td colSpan={4} className="guides-table-empty">Nenhum lote próximo do vencimento.</td></tr>

                        )}

                      </tbody>

                    </table>

                  </div>

                </div>

              )}



              {selectedGroupId !== 'dispensacao' && selectedGroupId !== 'estoque' && selectedGroupId !== 'validades' && (

                <div style={{ marginTop: 16 }}>

                  <Link to={`/farmacia/${selectedGroupId}`} className="btn btn-primary">

                    Ir para {activeGroup?.label}

                  </Link>

                </div>

              )}

            </div>

          </div>

        </div>

      </div>

    </div>

  );

}



export function PharmacyHubPage() {

  const { section } = useModuleSection('/farmacia');

  if (section === 'faturamento') {
    return <PharmacyBillingPage />;
  }

  if (section === 'hub') {

    return <PharmacyHubOverview />;

  }

  return <PharmacyPage />;

}


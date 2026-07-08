import { useEffect, useState, useCallback } from 'react';

import { Link, Navigate, useLocation } from 'react-router-dom';

import { api, type CommandCenterDashboardDto } from '../api/client';

import { useAuth } from '../auth/AuthContext';

import { occupancyRateCssClass, occupancyRateTone, OpsDashKpi } from '../components/dashboard/OpsDashKpi';

import { ModuleTabs } from '../components/ModuleTabs';

import { PageHeader } from '../components/PageHeader';

import { dashboardTabs } from '../navigation/moduleSections';

import { findMenuBreadcrumb } from '../navigation/sidebarMenu';

import { formatBrDateTime, formatBrTime } from '../utils/dateUtils';

import { RecentHospitalEventsPanel } from '../components/RecentHospitalEventsPanel';

import { subscribeOperationsRefresh } from '../offline/operationsRealtimeSync';



const REFRESH_MS = 30_000;



export function CommandCenterPage() {

  const { hasPermission, hasRole } = useAuth();

  const { pathname } = useLocation();

  const breadcrumb = findMenuBreadcrumb(pathname);

  const [data, setData] = useState<CommandCenterDashboardDto | null>(null);

  const [error, setError] = useState('');

  const [loading, setLoading] = useState(true);

  const [lastRefresh, setLastRefresh] = useState<Date | null>(null);

  const [eventsRefreshKey, setEventsRefreshKey] = useState(0);



  const isPatient = hasRole('Patient');

  const isStaff = !isPatient && hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage');



  const load = useCallback(async () => {

    try {

      const result = await api.getCommandCenterDashboard();

      setData(result);

      setError('');

      setLastRefresh(new Date());

      setEventsRefreshKey((k) => k + 1);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar centro de comando');

    } finally {

      setLoading(false);

    }

  }, []);



  useEffect(() => {

    if (!isStaff) {

      setLoading(false);

      return;

    }

    load();

    const timer = window.setInterval(load, REFRESH_MS);

    const unsubscribe = subscribeOperationsRefresh(() => {

      load();

    });

    return () => {

      window.clearInterval(timer);

      unsubscribe();

    };

  }, [isStaff, load]);



  if (isPatient) return <Navigate to="/portal-paciente" replace />;

  if (!isStaff) return <div className="card">Acesso restrito à equipe operacional.</div>;

  if (loading && !data) return <div className="card">Carregando centro de comando...</div>;

  if (error && !data) return <div className="alert alert-error">{error}</div>;

  if (!data) return null;



  const psTone = data.emergency.waiting > 5 ? 'red' : data.emergency.waiting > 0 ? 'yellow' : 'green';

  const waitTone = data.emergency.averageWaitMinutes > 60 ? 'red' : data.emergency.averageWaitMinutes > 30 ? 'yellow' : 'teal';



  return (

    <>

      <PageHeader

        eyebrow="TELA-CC · Centro de Comando"

        title={breadcrumb.title || 'Centro de Comando'}

        subtitle="Painel operacional em tempo real — filas, leitos, estoque, cirurgias e pendências."

      >

        <div className="dashboard-header-actions">

          <button type="button" className="btn btn-secondary btn-sm" onClick={() => load()}>

            Atualizar

          </button>

        </div>

      </PageHeader>



      <ModuleTabs basePath="/" tabs={dashboardTabs} />



      <div className="ops-dashboard">

        {lastRefresh && (

          <p className="dashboard-meta">

            Última atualização: {formatBrTime(lastRefresh.toISOString())}

            {data.generatedAt ? ` · Servidor: ${formatBrTime(data.generatedAt)}` : ''}

            {' · '}refresh automático 30s + tempo real (SignalR)

          </p>

        )}



        <section aria-label="Indicadores operacionais">

          <h2 className="feegow-dash-section-label">Indicadores operacionais</h2>

          <div className="feegow-dash-kpi-grid feegow-dash-kpi-grid-primary" style={{ marginTop: 12 }}>

            <OpsDashKpi

              value={data.emergency.waiting}

              label="PS aguardando"

              tone={psTone}

              footer={<Link to="/emergencia">Abrir PS</Link>}

            />

            <OpsDashKpi value={data.emergency.inCare} label="PS em atendimento" tone="teal" />

            <OpsDashKpi value={data.emergency.critical} label="Casos críticos na fila" tone="red" />

            <OpsDashKpi

              value={data.emergency.slaViolations}

              label="SLA PS ultrapassado"

              tone={data.emergency.slaViolations > 0 ? 'red' : 'neutral'}

            />

            <OpsDashKpi

              value={`${data.beds.occupancyRate}%`}

              label="Ocupação de leitos"

              tone={occupancyRateTone(data.beds.occupancyRate)}

              footer={<span>{data.beds.occupied}/{data.beds.total} leitos</span>}

            />

            <OpsDashKpi

              value={data.criticalClinicalAlerts}

              label="Alertas clínicos críticos"

              tone={data.criticalClinicalAlerts > 0 ? 'red' : 'green'}

              footer={<Link to="/notificacoes">Ver alertas</Link>}

            />

            <OpsDashKpi

              value={data.operations.pendingCleaning}

              label="Higienizações pendentes"

              tone={data.operations.pendingCleaning > 0 ? 'yellow' : 'green'}

            />

            <OpsDashKpi

              value={data.operations.pendingTransport}

              label="Transportes em fila"

              tone={data.operations.pendingTransport > 0 ? 'yellow' : 'neutral'}

            />

            <OpsDashKpi

              value={data.operations.activeAmbulanceDispatches}

              label="Ambulâncias ativas"

              tone={data.operations.activeAmbulanceDispatches > 0 ? 'teal' : 'neutral'}

            />

          </div>

        </section>



        <section aria-label="Pendências e suprimentos">

          <div className="feegow-dash-kpi-grid feegow-dash-kpi-grid-secondary">

            <OpsDashKpi

              value={data.openPendencies}

              label="Pendências abertas"

              tone={data.openPendencies > 0 ? 'yellow' : 'neutral'}

              footer={<Link to="/pendencias">Centro de pendências</Link>}

            />

            <OpsDashKpi

              value={data.pendingRequisitions}

              label="Requisições de estoque"

              tone={data.pendingRequisitions > 0 ? 'yellow' : 'neutral'}

            />

            <OpsDashKpi

              value={data.warehouse.lowStockProducts}

              label="Estoque abaixo do mínimo"

              tone={data.warehouse.lowStockProducts > 0 ? 'red' : 'green'}

            />

            <OpsDashKpi

              value={data.warehouse.expiringLots}

              label="Lotes a vencer (30 dias)"

              tone={data.warehouse.expiringLots > 0 ? 'yellow' : 'green'}

            />

          </div>

        </section>



        <div className="ops-dash-columns">

          <div className="card-panel appt-panel">

            <div className="card-panel-header">Pronto-socorro</div>

            <div className="card-panel-body">

              <p className="ops-dash-stat-line">

                Tempo médio de espera:{' '}

                <strong className={`feegow-dash-kpi-${waitTone}`} style={{ fontSize: 'inherit' }}>

                  {Math.round(data.emergency.averageWaitMinutes)} min

                </strong>

              </p>

              <p className="ops-dash-stat-line">

                Aguardando: <strong>{data.emergency.waiting}</strong>

                {' · '}Em atendimento: <strong>{data.emergency.inCare}</strong>

                {' · '}Críticos: <strong>{data.emergency.critical}</strong>

              </p>

              <div className="card-panel-footer" style={{ padding: 0, marginTop: 12 }}>

                <Link to="/emergencia" className="btn btn-secondary btn-sm">Abrir PS</Link>

                <Link to="/dashboard/assistencial" className="btn btn-secondary btn-sm">Painel assistencial</Link>

              </div>

            </div>

          </div>



          <div className="card-panel appt-panel">

            <div className="card-panel-header">Leitos — resumo</div>

            <div className="card-panel-body">

              <div className="ops-dash-mini-kpi-grid">

                <OpsDashKpi value={data.beds.occupied} label="Ocupados" tone="teal" />

                <OpsDashKpi value={data.beds.available} label="Disponíveis" tone="green" />

                <OpsDashKpi value={data.beds.cleaning} label="Higienização" tone="yellow" />

                <OpsDashKpi value={data.beds.maintenance} label="Manutenção" tone="neutral" />

                <OpsDashKpi value={data.beds.reserved} label="Reservados" tone="teal" />

              </div>

              <div className="card-panel-footer" style={{ padding: 0, marginTop: 12 }}>

                <Link to="/internacao/leitos" className="btn btn-secondary btn-sm">Mapa de leitos</Link>

              </div>

            </div>

          </div>

        </div>



        <div className="ops-dash-columns">

          <div className="card-panel appt-panel">

            <div className="card-panel-header">Fila do PS — aguardando</div>

            <div className="card-panel-body" style={{ padding: 0 }}>

              <div className="feegow-dash-table-wrap">

                <table className="feegow-dash-table">

                  <thead>

                    <tr>

                      <th>Paciente</th>

                      <th>Queixa</th>

                      <th>Risco</th>

                      <th>Espera (min)</th>

                    </tr>

                  </thead>

                  <tbody>

                    {data.emergencyQueue.map((item) => (

                      <tr key={item.id}>

                        <td>{item.patientName}</td>

                        <td>{item.chiefComplaint}</td>

                        <td>{item.urgency}</td>

                        <td>{Math.round(item.waitMinutes)}</td>

                      </tr>

                    ))}

                    {data.emergencyQueue.length === 0 && (

                      <tr>

                        <td colSpan={4} className="feegow-dash-empty">Nenhum paciente aguardando</td>

                      </tr>

                    )}

                  </tbody>

                </table>

              </div>

            </div>

          </div>



          <div className="card-panel appt-panel">

            <div className="card-panel-header">Chamadas recentes — TV / painel</div>

            <div className="card-panel-body" style={{ padding: 0 }}>

              <div className="feegow-dash-table-wrap">

                <table className="feegow-dash-table">

                  <thead>

                    <tr>

                      <th>Senha</th>

                      <th>Paciente</th>

                      <th>Destino</th>

                      <th>Horário</th>

                    </tr>

                  </thead>

                  <tbody>

                    {data.recentTvCalls.map((call, index) => (

                      <tr key={`${call.ticketNumber}-${call.calledAt}-${index}`}>

                        <td>{call.ticketNumber}</td>

                        <td>{call.patientName ?? '—'}</td>

                        <td>{call.destination}</td>

                        <td>{formatBrDateTime(call.calledAt)}</td>

                      </tr>

                    ))}

                    {data.recentTvCalls.length === 0 && (

                      <tr>

                        <td colSpan={4} className="feegow-dash-empty">Nenhuma chamada recente</td>

                      </tr>

                    )}

                  </tbody>

                </table>

              </div>

              <div className="card-panel-footer" style={{ padding: 12 }}>

                <Link to="/tv" className="btn btn-secondary btn-sm">Painéis de TV</Link>

              </div>

            </div>

          </div>

        </div>



        <div className="ops-dash-columns">

          <div className="card-panel appt-panel">

            <div className="card-panel-header">Almoxarifado</div>

            <div className="card-panel-body">

              <p className="ops-dash-stat-line">Estoque baixo: <strong>{data.warehouse.lowStockProducts}</strong> produto(s)</p>

              <p className="ops-dash-stat-line">Lotes a vencer (30 dias): <strong>{data.warehouse.expiringLots}</strong></p>

              <p className="ops-dash-stat-line">Requisições pendentes: <strong>{data.pendingRequisitions}</strong></p>

              <div className="card-panel-footer" style={{ padding: 0, marginTop: 12 }}>

                <Link to="/estoque/dashboard" className="btn btn-secondary btn-sm">Dashboard almoxarifado</Link>

              </div>

            </div>

          </div>



          <div className="card-panel appt-panel">

            <div className="card-panel-header">Centro cirúrgico — hoje</div>

            <div className="card-panel-body">

              <p className="ops-dash-stat-line">Total: <strong>{data.surgeries.total}</strong></p>

              <p className="ops-dash-stat-line">

                Agendadas: <strong>{data.surgeries.scheduled}</strong>

                {' · '}Em sala: <strong>{data.surgeries.inProgress}</strong>

              </p>

              <p className="ops-dash-stat-line">

                Concluídas: <strong>{data.surgeries.completed}</strong>

                {' · '}Canceladas: <strong>{data.surgeries.cancelled}</strong>

              </p>

              <div className="card-panel-footer" style={{ padding: 0, marginTop: 12 }}>

                <Link to="/centro-cirurgico" className="btn btn-secondary btn-sm">Agenda cirúrgica</Link>

              </div>

            </div>

          </div>

        </div>



        <div className="card-panel appt-panel">

          <div className="card-panel-header">Mapa de leitos por ala</div>

          <div className="card-panel-body" style={{ padding: 0 }}>

            <div className="feegow-dash-table-wrap">

              <table className="feegow-dash-table">

                <thead>

                  <tr>

                    <th>Ala</th>

                    <th>Total</th>

                    <th>Ocupados</th>

                    <th>Disponíveis</th>

                    <th>Higienização</th>

                    <th>Manutenção</th>

                    <th>Reservados</th>

                    <th>Ocupação</th>

                  </tr>

                </thead>

                <tbody>

                  {data.wards.map((w) => {

                    const rate = w.total === 0 ? 0 : Math.round((w.occupied / w.total) * 100);

                    return (

                      <tr key={w.wardId}>

                        <td>{w.wardName}</td>

                        <td>{w.total}</td>

                        <td>{w.occupied}</td>

                        <td>{w.available}</td>

                        <td>{w.cleaning}</td>

                        <td>{w.maintenance}</td>

                        <td>{w.reserved}</td>

                        <td>

                          <span className={`ops-dash-occupancy ${occupancyRateCssClass(rate)}`}>

                            {rate}%

                          </span>

                        </td>

                      </tr>

                    );

                  })}

                  {data.wards.length === 0 && (

                    <tr>

                      <td colSpan={8} className="feegow-dash-empty">Nenhuma ala cadastrada</td>

                    </tr>

                  )}

                </tbody>

              </table>

            </div>

          </div>

          <div className="card-panel-footer">

            <Link to="/pendencias" className="btn btn-secondary btn-sm">Centro de pendências</Link>

            <Link to="/notificacoes" className="btn btn-secondary btn-sm">Alertas</Link>

          </div>

        </div>



        <RecentHospitalEventsPanel key={eventsRefreshKey} limit={20} />

      </div>

    </>

  );

}


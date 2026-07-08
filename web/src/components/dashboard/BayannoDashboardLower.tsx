import type { DashboardAlertDto } from '../../api/client';
import { formatBrDate } from '../../utils/dateUtils';

type BayannoDashboardLowerProps = {
  alerts: DashboardAlertDto[];
  scheduleDate?: string;
};

/**
 * Copiado de admin/dashboard.php — calendário + mural (noticeboard).
 */
export function BayannoDashboardLower({ alerts, scheduleDate }: BayannoDashboardLowerProps) {
  const dateLabel = scheduleDate
    ? formatBrDate(`${scheduleDate}T12:00:00`)
    : formatBrDate(new Date().toISOString());

  return (
    <div className="bayanno-php-screen">
      <hr />
      <div className="container-fluid padded">
        <div className="row-fluid">
          <div className="span6">
            <div className="box">
              <div className="box-header">
                <div className="title">
                  <i className="icon-calendar" aria-hidden />
                  {' '}
                  Agenda — {dateLabel}
                </div>
              </div>
              <div className="box-content">
                <p style={{ margin: 0, color: '#777' }}>
                  Calendário integrado ao módulo de agendamentos APSMedCore.
                  Use os tiles acima ou o menu lateral para abrir a agenda completa.
                </p>
              </div>
            </div>
          </div>

          <div className="span6">
            <div className="box">
              <div className="box-header">
                <span className="title">
                  <i className="icon-reorder" aria-hidden />
                  {' '}
                  Mural de avisos
                </span>
              </div>
              <div className="box-content scrollable" style={{ maxHeight: 500, overflowY: 'auto' }}>
                {alerts.length === 0 ? (
                  <p style={{ margin: 0, color: '#777' }}>Nenhum aviso no momento.</p>
                ) : (
                  alerts.map((alert, index) => (
                      <div key={`${alert.code}-${index}`} className="box-section news with-icons">
                        <div className="avatar blue">
                          <i className="icon-tag icon-2x" aria-hidden />
                        </div>
                        <div className="news-time">
                          <span>{index + 1}</span>
                          {' '}
                          aviso
                        </div>
                        <div className="news-content">
                          <div className="news-title">{alert.title}</div>
                          <div className="news-text">{alert.message}</div>
                        </div>
                      </div>
                  ))
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

import type { OperationalDashboardDto } from '../../api/client';

type BayannoAreaTopProps = {
  title: string;
  data?: OperationalDashboardDto | null;
};

/**
 * Copiado de application/views/page_info.php (area-top + sparkline-box).
 */
export function BayannoAreaTop({ title, data }: BayannoAreaTopProps) {
  return (
    <div className="bayanno-php-screen">
      <div className="container-fluid">
        <div className="row-fluid">
          <div className="area-top clearfix">
            <div className="pull-left header">
              <h3 className="title">
                <i className="icon-info-sign" aria-hidden />
                {title}
              </h3>
            </div>
            {data && (
              <ul className="inline pull-right sparkline-box">
                <li className="sparkline-row">
                  <h4 className="green">
                    <span>Pacientes</span>
                    {data.totalPatients}
                  </h4>
                </li>
                <li className="sparkline-row">
                  <h4 className="red">
                    <span>Atend. hoje</span>
                    {data.attendancesToday}
                  </h4>
                </li>
                <li className="sparkline-row">
                  <h4 className="green">
                    <span>Internações</span>
                    {data.activeHospitalizations}
                  </h4>
                </li>
              </ul>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

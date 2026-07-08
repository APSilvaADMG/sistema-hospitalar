import { useLocation } from 'react-router-dom';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';

/**
 * Cópia de application/views/page_info.php — faixa area-top em todas as telas.
 */
export function BayannoRouteChrome() {
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const title = breadcrumb.title || 'Sistema hospitalar';

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
          </div>
        </div>
      </div>
    </div>
  );
}

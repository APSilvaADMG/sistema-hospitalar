import { Link } from 'react-router-dom';
import type { BayannoDashboardTile } from '../../config/bayannoDashboardTiles';

type BayannoActionTilesProps = {
  rows: BayannoDashboardTile[][];
};

/**
 * Markup copiado de application/views/admin/dashboard.php (action-nav-normal).
 */
export function BayannoActionTiles({ rows }: BayannoActionTilesProps) {
  if (rows.length === 0) return null;

  return (
    <div className="bayanno-php-screen">
      <div className="container-fluid padded">
        <div className="row-fluid">
          <div className="span30">
            <div className="action-nav-normal">
              {rows.map((row, rowIndex) => (
                <div key={rowIndex} className="row-fluid">
                  {row.map((tile) => (
                    <div key={`${tile.to}-${tile.label}`} className="span2 action-nav-button">
                      <Link to={tile.to}>
                        <i className={tile.icon} aria-hidden />
                        <span>{tile.label}</span>
                      </Link>
                    </div>
                  ))}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

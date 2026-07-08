import { Link } from 'react-router-dom';
import {
  BAYANNO_ADMIN_TILES,
  BAYANNO_BILLING_TILES,
  BAYANNO_DOCTOR_TILES,
  BAYANNO_LAB_TILES,
  BAYANNO_NURSE_TILES,
  BAYANNO_PATIENT_TILES,
  BAYANNO_PHARMACY_TILES,
  type BayannoDashboardTile,
} from '../../../config/bayannoDashboardTiles';
import { bayannoPhrase } from '../../../data/bayanno';
import type { BayannoScreen } from '../../../data/bayanno';
import { bayannoRoleToUserRole } from './feegowSghcNav';

type Props = {
  screen: BayannoScreen;
};

function tilesForRole(role: string): BayannoDashboardTile[][] {
  switch (role) {
    case 'accountant':
      return BAYANNO_BILLING_TILES;
    case 'admin':
      return BAYANNO_ADMIN_TILES;
    case 'doctor':
      return BAYANNO_DOCTOR_TILES;
    case 'nurse':
      return BAYANNO_NURSE_TILES;
    case 'patient':
      return BAYANNO_PATIENT_TILES;
    case 'pharmacist':
      return BAYANNO_PHARMACY_TILES;
    case 'laboratorist':
      return BAYANNO_LAB_TILES;
    default:
      return BAYANNO_ADMIN_TILES;
  }
}

export function FeegowSghcDashboard({ screen }: Props) {
  const tiles = tilesForRole(screen.role);
  const userRole = bayannoRoleToUserRole(screen.role);

  return (
    <div className="feegow-sghc-dashboard">
      <header className="feegow-sghc-screen-header">
        <div>
          <p className="feegow-sghc-screen-kicker">SGHC · {userRole}</p>
          <h1>{screen.title}</h1>
        </div>
      </header>

      <div className="feegow-sghc-tile-grid">
        {tiles.flat().map((tile) => (
          <Link key={`${tile.to}-${tile.label}`} to={tile.to} className="feegow-sghc-tile">
            <span className="feegow-sghc-tile-icon" aria-hidden>◆</span>
            <span>{tile.label}</span>
          </Link>
        ))}
      </div>

      <div className="feegow-sghc-feed">
        <h2>Avisos e agenda</h2>
        <ul className="feegow-sghc-feed-list">
          {screen.phraseKeys.slice(0, 4).map((key) => (
            <li key={key} className="feegow-sghc-feed-item">
              <span className="feegow-sghc-feed-badge" aria-hidden>●</span>
              <div>
                <strong>{bayannoPhrase(key)}</strong>
                <p>Acesso rápido conforme perfil {screen.role}.</p>
              </div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

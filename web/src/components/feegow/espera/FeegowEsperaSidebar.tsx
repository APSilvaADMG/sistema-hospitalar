import { useEffect, useState } from 'react';
import { useOpenModuleSearch } from '../../ModuleSearchProvider';
import { Modal } from '../../Modal';

export const FEEGOW_ESPERA_SIDEBAR_HOST_ID = 'feegow-espera-sidebar-host';

const LOCATION_KEY = 'iasgh-espera-location';

const LOCATION_OPTIONS = [
  'CONSULTÓRIO 01',
  'CONSULTÓRIO 02',
  'SALA DE ESPERA',
  'RECEPÇÃO',
];

export type EsperaSidebarCounts = {
  waiting: number;
  inCare: number;
  completedToday: number;
  emergencyWaiting: number;
  byRoom: { room: string; waiting: number; inCare: number }[];
};

type Props = {
  counts?: EsperaSidebarCounts;
};

export function FeegowEsperaSidebar({ counts }: Props) {
  const openSearch = useOpenModuleSearch();
  const [location, setLocation] = useState('CONSULTÓRIO 01');
  const [showLocationModal, setShowLocationModal] = useState(false);
  const [draftLocation, setDraftLocation] = useState(location);

  useEffect(() => {
    try {
      const saved = localStorage.getItem(LOCATION_KEY);
      if (saved) setLocation(saved);
    } catch {
      /* ignore */
    }
  }, []);

  function saveLocation() {
    setLocation(draftLocation);
    try {
      localStorage.setItem(LOCATION_KEY, draftLocation);
    } catch {
      /* ignore */
    }
    setShowLocationModal(false);
  }

  const locationRoomCounts = counts?.byRoom.find(
    (r) => r.room.toUpperCase().includes(location.replace('CONSULTÓRIO', 'Sala').slice(0, 8))
      || location.toUpperCase().includes(r.room.toUpperCase().slice(0, 4)),
  );

  return (
    <>
      <div className="feegow-agenda-sidebar feegow-espera-sidebar">
        <div className="feegow-quick-search feegow-espera-search">
          <span className="feegow-search-icon" aria-hidden>🔍</span>
          <button type="button" className="feegow-search-input" onClick={openSearch}>
            Busca rápida…
          </button>
        </div>

        {counts ? (
          <div className="feegow-espera-live-counts">
            <p className="feegow-espera-counts-title">Fila ao vivo</p>
            <div className="feegow-espera-count-row">
              <span>Aguardando</span>
              <strong>{counts.waiting}</strong>
            </div>
            <div className="feegow-espera-count-row">
              <span>Em atendimento</span>
              <strong>{counts.inCare}</strong>
            </div>
            <div className="feegow-espera-count-row">
              <span>Finalizados hoje</span>
              <strong>{counts.completedToday}</strong>
            </div>
            <div className="feegow-espera-count-row feegow-espera-count-emergency">
              <span>PS aguardando</span>
              <strong>{counts.emergencyWaiting}</strong>
            </div>
            {locationRoomCounts ? (
              <div className="feegow-espera-room-hint">
                <span>{locationRoomCounts.room}</span>
                <span>{locationRoomCounts.waiting} aguard. / {locationRoomCounts.inCare} atend.</span>
              </div>
            ) : null}
          </div>
        ) : null}

        <div className="feegow-location-block">
          <p className="feegow-location-label">LOCAL DE ATENDIMENTO</p>
          <p className="feegow-location-value">VOCÊ ESTÁ EM {location}</p>
          <button
            type="button"
            className="feegow-location-change"
            onClick={() => {
              setDraftLocation(location);
              setShowLocationModal(true);
            }}
          >
            Alterar local <span aria-hidden>✎</span>
          </button>
        </div>
      </div>

      <Modal
        open={showLocationModal}
        title="Alterar local de atendimento"
        onClose={() => setShowLocationModal(false)}
      >
        <div className="feegow-form-grid">
          <label className="feegow-field feegow-field-span-full">
            <span>Local</span>
            <select
              id="espera-location"
              value={draftLocation}
              onChange={(e) => setDraftLocation(e.target.value)}
            >
              {LOCATION_OPTIONS.map((opt) => (
                <option key={opt} value={opt}>{opt}</option>
              ))}
            </select>
          </label>
          <div className="feegow-form-actions">
            <button type="button" className="feegow-form-btn-cancel" onClick={() => setShowLocationModal(false)}>
              Cancelar
            </button>
            <button type="button" className="feegow-patient-save-btn" onClick={saveLocation}>Salvar</button>
          </div>
        </div>
      </Modal>
    </>
  );
}

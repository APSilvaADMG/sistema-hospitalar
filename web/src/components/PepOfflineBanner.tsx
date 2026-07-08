type PepOfflineBannerProps = {
  online: boolean;
  pendingCount: number;
  syncing: boolean;
  onSync: () => void;
};

export function PepOfflineBanner({ online, pendingCount, syncing, onSync }: PepOfflineBannerProps) {
  if (online && pendingCount === 0) return null;

  return (
    <div className={`pep-offline-banner${online ? ' pep-offline-pending' : ' pep-offline-mode'}`} role="status">
      {!online ? (
        <>
          <strong>Modo offline</strong>
          <span>Registros e guias serão salvos localmente e sincronizados quando a rede voltar.</span>
        </>
      ) : (
        <>
          <strong>{pendingCount} item(ns) aguardando sincronização</strong>
          <button type="button" className="btn btn-sm" onClick={onSync} disabled={syncing}>
            {syncing ? 'Sincronizando…' : 'Sincronizar agora'}
          </button>
        </>
      )}
    </div>
  );
}

type OperationsOfflineBarProps = {
  online: boolean;
  pendingCount: number;
  syncing: boolean;
  realtimeConnected?: boolean;
  onSync: () => void;
};

export function OperationsOfflineBar({
  online,
  pendingCount,
  syncing,
  realtimeConnected,
  onSync,
}: OperationsOfflineBarProps) {
  if (online && pendingCount === 0 && realtimeConnected !== false) {
    return null;
  }

  return (
    <div className={`pep-offline-banner${online ? ' pep-offline-pending' : ' pep-offline-mode'}`} role="status">
      {!online ? (
        <>
          <strong>Modo offline — operacional</strong>
          <span>Ações de maqueiro e higienização serão enfileiradas e sincronizadas quando a rede voltar.</span>
        </>
      ) : (
        <>
          <strong>{pendingCount} ação(ões) aguardando sincronização</strong>
          {realtimeConnected === false && (
            <span style={{ marginLeft: 8 }}>· reconectando tempo real…</span>
          )}
          <button type="button" className="btn btn-sm" onClick={onSync} disabled={syncing}>
            {syncing ? 'Sincronizando…' : 'Sincronizar agora'}
          </button>
        </>
      )}
    </div>
  );
}

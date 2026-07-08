import { useEffect, useState } from 'react';
import { api, type AdministrationRouteDto } from '../api/client';

export function useAdministrationRoutes() {
  const [routes, setRoutes] = useState<AdministrationRouteDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    api.getAdministrationRoutes()
      .then((items) => {
        if (!cancelled) {
          setRoutes(items);
          setError('');
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setRoutes([]);
          setError(err instanceof Error ? err.message : 'Erro ao carregar vias de administração.');
        }
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, []);

  return { routes, loading, error };
}

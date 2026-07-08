import { useCallback, useEffect, useState } from 'react';
import { api, type DigitalRecordSummaryDto, type MedicalRecordEntryDto } from '../api/client';

export function usePatientDigitalRecord(patientId: string) {
  const [digital, setDigital] = useState<DigitalRecordSummaryDto | null>(null);
  const [entries, setEntries] = useState<MedicalRecordEntryDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const reload = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const data = await api.getDigitalRecord(patientId);
      setDigital(data);
      setEntries(data.record.entries ?? []);
    } catch (err) {
      setDigital(null);
      setEntries([]);
      setError(err instanceof Error ? err.message : 'Erro ao carregar prontuário.');
    } finally {
      setLoading(false);
    }
  }, [patientId]);

  useEffect(() => {
    reload().catch(console.error);
  }, [reload]);

  return { digital, entries, loading, error, reload };
}

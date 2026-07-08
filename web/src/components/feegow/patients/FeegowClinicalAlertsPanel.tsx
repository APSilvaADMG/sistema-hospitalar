import { useEffect, useState } from 'react';
import { api, type ClinicalAlertDto } from '../../../api/client';

type Props = {
  patientId: string;
};

const severityClass: Record<string, string> = {
  critical: 'feegow-clinical-alert-critical',
  warning: 'feegow-clinical-alert-warning',
  info: 'feegow-clinical-alert-info',
};

export function FeegowClinicalAlertsPanel({ patientId }: Props) {
  const [alerts, setAlerts] = useState<ClinicalAlertDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError('');
    void api.getPatientClinicalAlerts(patientId)
      .then((result) => {
        if (!cancelled) setAlerts(result.alerts);
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Erro ao carregar alertas clínicos.');
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => { cancelled = true; };
  }, [patientId]);

  if (loading) {
    return <p className="feegow-patient-section-empty">Carregando alertas clínicos…</p>;
  }

  if (error) {
    return <p className="feegow-patient-section-empty">{error}</p>;
  }

  if (alerts.length === 0) {
    return <p className="feegow-patient-section-empty">Nenhum alerta clínico ativo para este paciente.</p>;
  }

  return (
    <ul className="feegow-clinical-alerts-list">
      {alerts.map((alert, index) => (
        <li
          key={`${alert.code}-${index}`}
          className={`feegow-clinical-alert ${severityClass[alert.severity] ?? ''}`}
        >
          <strong>{alert.title}</strong>
          {alert.ruleId ? <span className="feegow-clinical-alert-rule">[{alert.ruleId}]</span> : null}
          <p>{alert.message}</p>
        </li>
      ))}
    </ul>
  );
}

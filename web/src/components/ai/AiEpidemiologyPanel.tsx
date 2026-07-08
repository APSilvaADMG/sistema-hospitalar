import { useCallback, useEffect, useState } from 'react';
import {
  api,
  type AiInsightReportDto,
  type GroqStatusDto,
  type PatientDto,
} from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';

const riskClass: Record<string, string> = {
  Normal: 'badge-success',
  Atenção: 'badge-warning',
  Alerta: 'badge-danger',
};

export function AiEpidemiologyPanel() {
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [patientId, setPatientId] = useState('');
  const [reports, setReports] = useState<AiInsightReportDto[]>([]);
  const [selected, setSelected] = useState<AiInsightReportDto | null>(null);
  const [loading, setLoading] = useState('');
  const [error, setError] = useState('');
  const [groqStatus, setGroqStatus] = useState<GroqStatusDto | null>(null);

  const loadHistory = useCallback(async () => {
    try {
      const items = await api.getAiInsightReports(15);
      setReports(items);
    } catch (err) {
      console.error(err);
    }
  }, []);

  useEffect(() => {
    api.getPatients(undefined, 1).then((r) => setPatients(r.items)).catch(console.error);
    loadHistory().catch(console.error);
    api.getGroqStatus().then(setGroqStatus).catch(console.error);
  }, [loadHistory]);

  async function run(action: 'outbreak' | 'triage' | 'recurrent') {
    setLoading(action);
    setError('');
    setSelected(null);
    try {
      let report: AiInsightReportDto;
      if (action === 'outbreak') {
        report = await api.analyzeOutbreak(30);
      } else if (action === 'triage') {
        report = await api.analyzeTriageOperational(7);
      } else {
        if (!patientId) {
          setError('Selecione um paciente para análise de recorrência.');
          return;
        }
        report = await api.analyzeRecurrentPatient(patientId);
      }
      setSelected(report);
      await loadHistory();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar análise.');
    } finally {
      setLoading('');
    }
  }

  return (
    <div className="ai-epidemiology-panel">
      <div className="card-panel appt-panel">
        <div className="card-panel-header">Análises epidemiológicas e operacionais</div>
        <div className="card-panel-body">
          <p className="ai-epi-intro">
            Inspirado em dev-queiroz/sitrep — dados agregados no backend; enriquecimento opcional via Groq
            {groqStatus?.configured
              ? ` (${groqStatus.model})`
              : ' (modo regras — configure Groq:Enabled + ApiKey no appsettings)'}
            .
          </p>
          <div className="ai-epi-actions">
            <button
              type="button"
              className="btn btn-primary"
              disabled={loading === 'outbreak'}
              onClick={() => run('outbreak')}
            >
              {loading === 'outbreak' ? 'Analisando…' : 'Surto respiratório'}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              disabled={loading === 'triage'}
              onClick={() => run('triage')}
            >
              {loading === 'triage' ? 'Analisando…' : 'Triagens PS (operacional)'}
            </button>
            <div className="ai-epi-recurrent">
              <select
                value={patientId}
                onChange={(e) => setPatientId(e.target.value)}
                aria-label="Paciente"
              >
                <option value="">Paciente — recorrência</option>
                {patients.map((p) => (
                  <option key={p.id} value={p.id}>{p.fullName}</option>
                ))}
              </select>
              <button
                type="button"
                className="btn btn-secondary"
                disabled={loading === 'recurrent'}
                onClick={() => run('recurrent')}
              >
                {loading === 'recurrent' ? 'Analisando…' : 'Analisar'}
              </button>
            </div>
          </div>
          {error ? <p className="form-error">{error}</p> : null}
        </div>
      </div>

      {selected ? (
        <div className="card-panel appt-panel ai-epi-result">
          <div className="card-panel-header">
            {selected.title}
            {' '}
            <span className={`badge ${riskClass[selected.riskLevel] ?? ''}`}>{selected.riskLevel}</span>
            {selected.groqEnriched ? (
              <span className="badge badge-info" style={{ marginLeft: 6 }}>Groq</span>
            ) : null}
          </div>
          <div className="card-panel-body">
            <p>{selected.summary}</p>
            <table className="data-table">
              <thead>
                <tr><th>Indicador</th><th>Valor</th></tr>
              </thead>
              <tbody>
                {selected.indicators.map((i) => (
                  <tr key={i.label}>
                    <td>{i.label}</td>
                    <td>{i.value}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <pre className="ai-epi-markdown">{selected.markdown}</pre>
          </div>
        </div>
      ) : null}

      <div className="card-panel appt-panel">
        <div className="card-panel-header">Histórico de relatórios IA</div>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Data</th><th>Tipo</th><th>Título</th><th>Risco</th><th /></tr>
            </thead>
            <tbody>
              {reports.map((r) => (
                <tr key={r.id}>
                  <td>{formatBrDateTime(r.createdAt)}</td>
                  <td>{r.type}</td>
                  <td>{r.title}</td>
                  <td><span className={`badge ${riskClass[r.riskLevel] ?? ''}`}>{r.riskLevel}</span></td>
                  <td>
                    <button type="button" className="btn btn-sm btn-secondary" onClick={() => setSelected(r)}>
                      Ver
                    </button>
                  </td>
                </tr>
              ))}
              {reports.length === 0 ? (
                <tr>
                  <td colSpan={5} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                    Nenhum relatório gerado ainda.
                  </td>
                </tr>
              ) : null}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

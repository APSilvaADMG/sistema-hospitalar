import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type PatientDto,
  type ReportCatalogItemDto,
  type ReportResultDto,
} from '../../api/client';
import {
  type ClinicalDocumentContext,
  findReportSource,
  generateReportFromClinicalData,
  parseClinicalFormData,
  saveReportSource,
  type ReportCaptureData,
} from '../../utils/clinicalDocumentWorkflow';

type Props = {
  reportCode: string;
  reportName?: string;
  patients: PatientDto[];
  workflow?: 'direct' | 'clinical';
  clinicalContext?: ClinicalDocumentContext;
  lockedPatientId?: string;
  initialSourceId?: string;
  defaultDateFrom?: string;
  defaultDateTo?: string;
  onClinicalSaved?: () => void;
  onGenerated?: (result: ReportResultDto) => void;
};

function defaultDateRange() {
  const to = new Date();
  const from = new Date();
  from.setDate(from.getDate() - 30);
  return {
    from: from.toISOString().slice(0, 10),
    to: to.toISOString().slice(0, 10),
  };
}

export function ReportClinicalCaptureForm({
  reportCode,
  reportName,
  patients,
  workflow = 'clinical',
  clinicalContext,
  lockedPatientId,
  initialSourceId,
  defaultDateFrom,
  defaultDateTo,
  onClinicalSaved,
  onGenerated,
}: Props) {
  const range = defaultDateRange();
  const [patientId, setPatientId] = useState(lockedPatientId ?? '');
  const [dateFrom, setDateFrom] = useState(defaultDateFrom ?? range.from);
  const [dateTo, setDateTo] = useState(defaultDateTo ?? range.to);
  const [catalogItem, setCatalogItem] = useState<ReportCatalogItemDto | null>(null);
  const [clinicalSourceId, setClinicalSourceId] = useState<string | undefined>(initialSourceId);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [saving, setSaving] = useState(false);

  const title = reportName ?? catalogItem?.name ?? reportCode;

  useEffect(() => {
    if (lockedPatientId) setPatientId(lockedPatientId);
  }, [lockedPatientId]);

  useEffect(() => {
    api.getReportsCatalog({ search: reportCode })
      .then((items) => setCatalogItem(items.find((i) => i.code === reportCode) ?? items[0] ?? null))
      .catch(console.error);
  }, [reportCode]);

  useEffect(() => {
    if (!patientId || workflow !== 'clinical') return;
    let cancelled = false;

    (async () => {
      if (initialSourceId) {
        const source = await api.getClinicalSource(initialSourceId);
        if (cancelled) return;
        setClinicalSourceId(source.id);
        const parsed = parseClinicalFormData<ReportCaptureData>(source.formDataJson);
        if (parsed) {
          setDateFrom(parsed.filters.dateFrom?.slice(0, 10) ?? dateFrom);
          setDateTo(parsed.filters.dateTo?.slice(0, 10) ?? dateTo);
          return;
        }
      }

      if (clinicalContext) {
        const existing = await findReportSource(patientId, reportCode, clinicalContext);
        if (cancelled) return;
        if (existing) {
          setClinicalSourceId(existing.id);
          const parsed = parseClinicalFormData<ReportCaptureData>(existing.formDataJson);
          if (parsed) {
            setDateFrom(parsed.filters.dateFrom?.slice(0, 10) ?? dateFrom);
            setDateTo(parsed.filters.dateTo?.slice(0, 10) ?? dateTo);
          }
        }
      }
    })().catch(console.error);

    return () => {
      cancelled = true;
    };
  }, [patientId, reportCode, workflow, clinicalContext, initialSourceId]);

  function buildCapture(): ReportCaptureData {
    return {
      reportCode,
      reportName: title,
      filters: {
        patientId: patientId || undefined,
        dateFrom: `${dateFrom}T00:00:00Z`,
        dateTo: `${dateTo}T23:59:59Z`,
      },
    };
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setSuccess('');
    if (!patientId && workflow === 'clinical') {
      setError('Selecione o paciente.');
      return;
    }
    setSaving(true);
    try {
      if (workflow === 'clinical') {
        const source = await saveReportSource(
          patientId,
          reportCode,
          title,
          clinicalContext ?? { label: title },
          buildCapture(),
        );
        setClinicalSourceId(source.id);
        setSuccess('Filtros salvos no sistema.');
        onClinicalSaved?.();
        return;
      }
      const result = await api.runReport(reportCode, buildCapture().filters);
      setSuccess(`Relatório gerado (${result.rows.length} linha(s)).`);
      onGenerated?.(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.');
    } finally {
      setSaving(false);
    }
  }

  async function handleGenerate() {
    if (!patientId) {
      setError('Selecione o paciente.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const { result } = await generateReportFromClinicalData(
        patientId,
        reportCode,
        title,
        clinicalContext ?? { label: title },
        buildCapture(),
        clinicalSourceId,
      );
      setSuccess(`Relatório gerado (${result.rows.length} linha(s)).`);
      onGenerated?.(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar relatório.');
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="card-panel appt-panel" onSubmit={handleSubmit}>
      <div className="card-panel-header">{title}</div>
      {workflow === 'clinical' && (
        <div className="alert alert-info" style={{ margin: '12px 12px 0' }}>
          Salve os filtros durante o fluxo assistencial. O relatório será gerado depois no faturamento.
        </div>
      )}
      {error && <div className="alert alert-error" style={{ margin: 12 }}>{error}</div>}
      {success && <div className="alert alert-success" style={{ margin: 12 }}>{success}</div>}

      <div className="form-grid" style={{ padding: 12 }}>
        <div className="form-field">
          <label>Paciente {workflow === 'clinical' ? '*' : ''}</label>
          <select
            required={workflow === 'clinical'}
            value={patientId}
            disabled={Boolean(lockedPatientId)}
            onChange={(e) => setPatientId(e.target.value)}
          >
            <option value="">Todos / selecione</option>
            {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
          </select>
        </div>
        <div className="form-field">
          <label>Período — de</label>
          <input type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
        </div>
        <div className="form-field">
          <label>Período — até</label>
          <input type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
        </div>
        {catalogItem && !catalogItem.isImplemented && (
          <div className="form-field full">
            <p className="form-hint">Este relatório ainda não possui implementação completa no servidor.</p>
          </div>
        )}
        <div className="form-field full modal-actions">
          <button type="submit" className="btn" disabled={saving}>
            {saving ? 'Salvando…' : workflow === 'clinical' ? 'Salvar filtros no sistema' : 'Gerar relatório'}
          </button>
          {workflow === 'clinical' && (
            <button type="button" className="btn btn-secondary" disabled={saving} onClick={handleGenerate}>
              Gerar relatório agora
            </button>
          )}
        </div>
      </div>
    </form>
  );
}

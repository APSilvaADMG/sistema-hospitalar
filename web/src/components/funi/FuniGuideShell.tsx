import { useState, type FormEvent, type ReactNode } from 'react';
import { Link } from 'react-router-dom';
import type { FuniGuideDefinition } from '../../data/funiGuides/catalog';
import { getFuniPdfUrl } from '../../data/funiGuides/catalog';
import type { HealthInsuranceDto, PatientDto } from '../../api/client';
import { InsuranceLogo } from '../InsuranceLogo';
import { printFuniGuide } from '../../utils/printFuniGuide';
import './funiGuide.css';

type Props = {
  guide: FuniGuideDefinition;
  patients: PatientDto[];
  insurances: HealthInsuranceDto[];
  patientId: string;
  healthInsuranceId: string;
  onPatientChange: (id: string) => void;
  onInsuranceChange: (id: string) => void;
  error?: string;
  success?: string;
  saving?: boolean;
  submitLabel: string;
  onSubmit: (e: FormEvent) => void;
  children: ReactNode;
  workflow?: 'direct' | 'clinical';
  compact?: boolean;
  lockPatient?: boolean;
  secondaryAction?: ReactNode;
};

export function FuniGuideShell({
  guide,
  patients,
  insurances,
  patientId,
  healthInsuranceId,
  onPatientChange,
  onInsuranceChange,
  error,
  success,
  saving,
  submitLabel,
  onSubmit,
  children,
  workflow = 'direct',
  compact = false,
  lockPatient = false,
  secondaryAction,
}: Props) {
  const [showPdf, setShowPdf] = useState(!compact);
  const pdfUrl = getFuniPdfUrl(guide.pdfFile);
  const selectedInsurance = (Array.isArray(insurances) ? insurances : []).find((i) => i.id === healthInsuranceId);

  return (
    <div className="funi-guide-page">
      {!compact && (
        <div className="funi-guide-toolbar no-print">
          <button className="btn" type="button" onClick={() => printFuniGuide(`${guide.funiCode} — ${guide.title}`)}>
            Imprimir / PDF
          </button>
          <button className="btn btn-secondary" type="button" onClick={() => setShowPdf((v) => !v)}>
            {showPdf ? 'Ocultar PDF original' : 'Comparar com PDF'}
          </button>
          <a className="btn btn-secondary" href={pdfUrl} target="_blank" rel="noreferrer">Abrir PDF em nova aba</a>
          <Link className="btn btn-secondary" to="/faturamento-tiss/guias-funi">Catálogo FUNI</Link>
          <Link className="btn btn-secondary" to="/faturamento-tiss">Guias TISS</Link>
        </div>
      )}

      {workflow === 'clinical' && (
        <div className="alert alert-info no-print" style={{ marginBottom: 12 }}>
          Modo assistencial: os campos ficam salvos no sistema. A guia FUNI é gerada depois no faturamento, já preenchida.
        </div>
      )}

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <form onSubmit={onSubmit}>
        <div className="card-panel appt-panel no-print" style={{ marginBottom: 16 }}>
          <div className="form-grid">
            <div className="form-field">
              <label>Paciente *</label>
              <select required value={patientId} disabled={lockPatient} onChange={(e) => onPatientChange(e.target.value)}>
                <option value="">Selecione para preencher automaticamente</option>
                {(Array.isArray(patients) ? patients : []).map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
              </select>
            </div>
            <div className="form-field">
              <label>Convênio *</label>
              <div className="funi-guide-insurance-row">
                {selectedInsurance && (
                  <InsuranceLogo
                    name={selectedInsurance.name}
                    logoUrl={selectedInsurance.logoUrl}
                    size={40}
                    className="funi-guide-insurance-logo"
                  />
                )}
                <select required value={healthInsuranceId} onChange={(e) => onInsuranceChange(e.target.value)}>
                  <option value="">Selecione</option>
                  {(Array.isArray(insurances) ? insurances : []).map((i) => <option key={i.id} value={i.id}>{i.name}</option>)}
                </select>
              </div>
            </div>
          </div>
          {!compact && (
            <p className="form-hint">
              Preenchimento assistido pelo PEP. Use &quot;Comparar com PDF&quot; para conferir campo a campo com o formulário oficial {guide.funiCode}.
            </p>
          )}
        </div>

        <div className={`funi-guide-split ${showPdf ? 'funi-guide-split--with-pdf' : ''}`}>
          {showPdf && (
            <aside className="funi-guide-pdf-panel no-print">
              <div className="funi-guide-pdf-title">{guide.funiCode} — PDF oficial</div>
              <iframe title={`PDF ${guide.funiCode}`} src={pdfUrl} className="funi-guide-pdf-frame" />
            </aside>
          )}
          <div className="funi-guide-form-panel">{children}</div>
        </div>

        <div className="funi-guide-toolbar no-print" style={{ marginTop: 16 }}>
          <button className="btn" type="submit" disabled={saving}>
            {saving ? 'Salvando…' : submitLabel}
          </button>
          {secondaryAction}
        </div>
      </form>
    </div>
  );
}

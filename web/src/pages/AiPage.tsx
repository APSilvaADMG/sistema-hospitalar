import { type FormEvent, useEffect, useMemo, useState } from 'react';

import {

  api,

  manchesterProtocolInfo,

  triageUrgencyLabels,

  type AiTriageLogDto,

  type Cid10SuggestionDto,

  type PatientDto,

  type TriageRequestDto,

  type TriageResponseDto,

} from '../api/client';

import { FilterBar } from '../components/FilterBar';

import { KpiCard } from '../components/KpiCard';

import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { aiTabs } from '../navigation/moduleSections';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useModuleSection } from '../navigation/useModuleSection';
import { formatBrDateTime } from '../utils/dateUtils';

import { useAuth } from '../auth/AuthContext';
import { useLocation } from 'react-router-dom';
import { AiEpidemiologyPanel } from '../components/ai/AiEpidemiologyPanel';



const urgencyClass: Record<string, string> = {

  Emergency: 'urgency-emergency',

  High: 'urgency-high',

  Medium: 'urgency-medium',

  Low: 'urgency-low',

  NonUrgent: 'urgency-nonurgent',

};



const WIZARD_STEPS = [

  { num: 1, title: 'Recepção e Registro', hint: 'Dados pessoais e cobertura' },

  { num: 2, title: 'Avaliação de Enfermagem', hint: 'Sinais vitais e entrevista clínica' },

  { num: 3, title: 'Classificação de Risco', hint: 'Protocolo de Manchester' },

  { num: 4, title: 'Encaminhamento', hint: 'Fila ou consultório' },

];



const emptyForm: TriageRequestDto = {

  symptoms: '',

  patientId: '',

  documentNumber: '',

  susCardNumber: '',

  healthInsuranceName: '',

  systolicBp: undefined,

  diastolicBp: undefined,

  temperatureC: undefined,

  heartRateBpm: undefined,

  oxygenSaturationPct: undefined,

  painLevel: undefined,

  healthHistory: '',

};



export function AiPage() {

  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/ia');
  const activeSection = section || '';

  const [step, setStep] = useState(1);

  const [form, setForm] = useState<TriageRequestDto>(emptyForm);

  const [patients, setPatients] = useState<PatientDto[]>([]);

  const [result, setResult] = useState<TriageResponseDto | null>(null);

  const [cidText, setCidText] = useState('');

  const [cidSuggestions, setCidSuggestions] = useState<Cid10SuggestionDto[]>([]);

  const [logs, setLogs] = useState<AiTriageLogDto[]>([]);

  const [loading, setLoading] = useState(false);

  const [referring, setReferring] = useState(false);

  const [error, setError] = useState('');

  const [success, setSuccess] = useState('');

  const [urgencyFilter, setUrgencyFilter] = useState('');

  const [search, setSearch] = useState('');



  useEffect(() => {

    api.getPatients('', 1).then((p) => setPatients(p.items)).catch(console.error);

    api.getTriageLogs().then(setLogs).catch(console.error);

  }, []);



  const selectedPatient = useMemo(

    () => patients.find((p) => p.id === form.patientId),

    [patients, form.patientId],

  );



  const filteredLogs = useMemo(() => logs

    .filter((l) => !urgencyFilter || l.urgency === urgencyFilter)

    .filter((l) => {

      if (!search.trim()) return true;

      const term = search.toLowerCase();

      return (l.patientName?.toLowerCase().includes(term) ?? false)

        || l.symptoms.toLowerCase().includes(term)

        || (l.suggestedCid10?.toLowerCase().includes(term) ?? false);

    }), [logs, urgencyFilter, search]);



  const stats = useMemo(() => ({

    total: logs.length,

    emergency: logs.filter((l) => l.urgency === 'Emergency').length,

    high: logs.filter((l) => l.urgency === 'High').length,

    today: logs.filter((l) => new Date(l.createdAt).toDateString() === new Date().toDateString()).length,

  }), [logs]);



  if (!hasPermission('patients.read', 'pep.read', 'reports.read', 'hospitalization.manage')) {

    return <div className="card">Acesso restrito à equipe clínica e administrativa.</div>;

  }



  function updateForm<K extends keyof TriageRequestDto>(key: K, value: TriageRequestDto[K]) {

    setForm((prev) => ({ ...prev, [key]: value }));

  }



  function handlePatientChange(patientId: string) {

    const patient = patients.find((p) => p.id === patientId);

    setForm((prev) => ({

      ...prev,

      patientId,

      documentNumber: patient?.cpf ?? prev.documentNumber,

      healthInsuranceName: patient?.primaryInsuranceName ?? prev.healthInsuranceName,

    }));

  }



  function resetTriage() {

    setForm(emptyForm);

    setResult(null);

    setStep(1);

    setError('');

    setSuccess('');

  }



  async function handleClassify(e: FormEvent) {

    e.preventDefault();

    setLoading(true);

    setError('');

    setSuccess('');

    try {

      const payload: TriageRequestDto = {

        ...form,

        patientId: form.patientId || undefined,

        documentNumber: form.documentNumber || undefined,

        susCardNumber: form.susCardNumber || undefined,

        healthInsuranceName: form.healthInsuranceName || undefined,

        healthHistory: form.healthHistory || undefined,

        systolicBp: form.systolicBp ? Number(form.systolicBp) : undefined,

        diastolicBp: form.diastolicBp ? Number(form.diastolicBp) : undefined,

        temperatureC: form.temperatureC ? Number(form.temperatureC) : undefined,

        heartRateBpm: form.heartRateBpm ? Number(form.heartRateBpm) : undefined,

        oxygenSaturationPct: form.oxygenSaturationPct ? Number(form.oxygenSaturationPct) : undefined,

        painLevel: form.painLevel !== undefined && form.painLevel !== null

          ? Number(form.painLevel)

          : undefined,

      };

      const response = await api.analyzeTriage(payload);

      setResult(response);

      setStep(3);

      setLogs(await api.getTriageLogs());

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro na triagem');

    } finally {

      setLoading(false);

    }

  }



  async function handleReferToEmergency() {

    if (!result || !form.patientId) return;

    setReferring(true);

    setError('');

    setSuccess('');

    try {

      await api.createEmergencyVisit({

        patientId: form.patientId,

        chiefComplaint: form.symptoms,

        urgency: result.urgency,

        aiTriageLogId: result.triageLogId,

        notes: `${result.manchesterColor} (${result.urgencyLabel}) — ${result.referralLabel}`,

      });

      setStep(4);

      setSuccess('Paciente encaminhado para a fila do pronto-socorro com prioridade clínica.');

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao encaminhar paciente');

    } finally {

      setReferring(false);

    }

  }



  async function handleCidSuggest(e: FormEvent) {

    e.preventDefault();

    try {

      setCidSuggestions(await api.suggestCid10({ text: cidText, maxResults: 8 }));

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro na sugestão CID-10');

    }

  }



  const waitLabel = result

    ? (result.maxWaitMinutes === 0 ? 'Imediato' : `Até ${result.maxWaitMinutes} min`)

    : '';



  return (

    <>

      <PageHeader

        eyebrow="Atendimento"

        title={activeSection ? breadcrumb.title : 'IA — Triagem Inteligente'}

        subtitle="Classificação de risco pelo Protocolo de Manchester: recepção, avaliação de enfermagem, classificação e encaminhamento."

      />

      <ModuleNav basePath="/ia" tabs={aiTabs} />

      {error && <div className="alert alert-error">{error}</div>}

      {success && <div className="alert alert-success">{success}</div>}

      {activeSection !== 'historico' && activeSection !== 'epidemiologia' && (
      <>
      <div className="kpi-grid">

        <KpiCard label="Triagens realizadas" value={stats.total} variant="primary" />

        <KpiCard label="Vermelho (emergência)" value={stats.emergency} variant="danger" />

        <KpiCard label="Laranja (muito urgente)" value={stats.high} variant="warning" />

        <KpiCard label="Hoje" value={stats.today} variant="info" />

      </div>



      <div className="grid-2" style={{ marginBottom: 20 }}>

        <div className="card">

          <div className="form-section-title">Triagem hospitalar — Protocolo de Manchester</div>

          <p style={{ color: 'var(--muted)', fontSize: '0.9rem', marginTop: 0 }}>

            Prioriza atendimento por gravidade clínica, não por ordem de chegada.

          </p>



          <div className="triage-wizard-steps">

            {WIZARD_STEPS.map((s) => (

              <div

                key={s.num}

                className={`triage-wizard-step${step === s.num ? ' active' : ''}${step > s.num ? ' done' : ''}`}

              >

                <span className="triage-wizard-step-num">{s.num}</span>

                <strong>{s.title}</strong>

                <div style={{ color: 'var(--muted)', marginTop: 4 }}>{s.hint}</div>

              </div>

            ))}

          </div>



          {step === 1 && (

            <form className="form-grid" onSubmit={(e) => { e.preventDefault(); setStep(2); }}>

              <div className="form-field full">

                <label>Paciente *</label>

                <select

                  value={form.patientId ?? ''}

                  onChange={(e) => handlePatientChange(e.target.value)}

                  required

                >

                  <option value="">Selecione o paciente...</option>

                  {patients.map((p) => (

                    <option key={p.id} value={p.id}>{p.fullName}</option>

                  ))}

                </select>

              </div>

              <div className="form-field">

                <label>Documento (CPF/RG)</label>

                <input

                  value={form.documentNumber ?? ''}

                  onChange={(e) => updateForm('documentNumber', e.target.value)}

                  placeholder="Documento de identificação"

                />

              </div>

              <div className="form-field">

                <label>Cartão do SUS</label>

                <input

                  value={form.susCardNumber ?? ''}

                  onChange={(e) => updateForm('susCardNumber', e.target.value)}

                  placeholder="Número do cartão SUS"

                />

              </div>

              <div className="form-field full">

                <label>Convênio / plano de saúde</label>

                <input

                  value={form.healthInsuranceName ?? ''}

                  onChange={(e) => updateForm('healthInsuranceName', e.target.value)}

                  placeholder={selectedPatient?.primaryInsuranceName ?? 'Particular ou convênio'}

                />

              </div>

              <div className="form-field full modal-actions">

                <button type="submit" className="btn">Continuar para avaliação de enfermagem</button>

              </div>

            </form>

          )}



          {step === 2 && (

            <form className="form-grid" onSubmit={handleClassify}>

              <div className="form-field full">

                <label>Queixa principal e sintomas *</label>

                <textarea

                  rows={3}

                  value={form.symptoms}

                  onChange={(e) => updateForm('symptoms', e.target.value)}

                  placeholder="Descreva a queixa relatada pelo paciente na entrevista clínica..."

                  required

                />

              </div>

              <div className="form-field full">

                <label>Histórico de saúde relevante</label>

                <textarea

                  rows={2}

                  value={form.healthHistory ?? ''}

                  onChange={(e) => updateForm('healthHistory', e.target.value)}

                  placeholder="Comorbidades, alergias, medicações em uso, cirurgias prévias..."

                />

              </div>

              <div className="form-field full">

                <div className="form-section-title" style={{ marginBottom: 8 }}>Sinais vitais</div>

                <div className="triage-vitals-grid">

                  <div className="form-field">

                    <label>PA sistólica (mmHg)</label>

                    <input

                      type="number"

                      min={40}

                      max={260}

                      value={form.systolicBp ?? ''}

                      onChange={(e) => updateForm('systolicBp', e.target.value ? Number(e.target.value) : undefined)}

                    />

                  </div>

                  <div className="form-field">

                    <label>PA diastólica (mmHg)</label>

                    <input

                      type="number"

                      min={20}

                      max={160}

                      value={form.diastolicBp ?? ''}

                      onChange={(e) => updateForm('diastolicBp', e.target.value ? Number(e.target.value) : undefined)}

                    />

                  </div>

                  <div className="form-field">

                    <label>Temperatura (°C)</label>

                    <input

                      type="number"

                      step="0.1"

                      min={34}

                      max={42}

                      value={form.temperatureC ?? ''}

                      onChange={(e) => updateForm('temperatureC', e.target.value ? Number(e.target.value) : undefined)}

                    />

                  </div>

                  <div className="form-field">

                    <label>Frequência cardíaca (bpm)</label>

                    <input

                      type="number"

                      min={20}

                      max={220}

                      value={form.heartRateBpm ?? ''}

                      onChange={(e) => updateForm('heartRateBpm', e.target.value ? Number(e.target.value) : undefined)}

                    />

                  </div>

                  <div className="form-field">

                    <label>Saturação O₂ (%)</label>

                    <input

                      type="number"

                      min={50}

                      max={100}

                      value={form.oxygenSaturationPct ?? ''}

                      onChange={(e) => updateForm('oxygenSaturationPct', e.target.value ? Number(e.target.value) : undefined)}

                    />

                  </div>

                  <div className="form-field">

                    <label>Nível de dor (0–10)</label>

                    <input

                      type="number"

                      min={0}

                      max={10}

                      value={form.painLevel ?? ''}

                      onChange={(e) => updateForm('painLevel', e.target.value !== '' ? Number(e.target.value) : undefined)}

                    />

                  </div>

                </div>

              </div>

              <div className="form-field full modal-actions">

                <button type="button" className="btn btn-secondary" onClick={() => setStep(1)}>Voltar</button>

                <button type="submit" className="btn" disabled={loading}>

                  {loading ? 'Classificando...' : 'Classificar risco (Manchester)'}

                </button>

              </div>

            </form>

          )}



          {(step === 3 || step === 4) && result && (

            <div style={{ marginTop: 4 }}>

              <div className={`triage-result ${urgencyClass[result.urgency] ?? ''}`}>

                <div className="triage-result-header">

                  <span

                    className="triage-result-color"

                    style={{ background: result.manchesterColorHex }}

                    aria-hidden

                  />

                  <div>

                    <h3 style={{ margin: 0 }}>{result.manchesterColor} — {result.urgencyLabel}</h3>

                    <p style={{ margin: '4px 0 0', color: 'var(--muted)', fontSize: '0.9rem' }}>

                      Tempo máximo de espera: <strong>{waitLabel}</strong>

                    </p>

                  </div>

                </div>

                <p><strong>Encaminhamento:</strong> {result.referralLabel}</p>

                <p><strong>Especialidade sugerida:</strong> {result.recommendedSpecialty}</p>

                {result.suggestedCid10 && (

                  <p><strong>CID-10 sugerido:</strong> {result.suggestedCid10} — {result.suggestedCid10Description}</p>

                )}

                <p>{result.guidance}</p>
                {form.patientId && (
                  <p className="field-hint" style={{ marginTop: 12 }}>
                    Se for necessária internação nas próximas 72 h, motivo e diagnóstico serão sugeridos automaticamente na admissão.
                  </p>
                )}

              </div>



              <div className="modal-actions" style={{ marginTop: 16 }}>

                {step === 3 && (

                  <>

                    {(result.referral === 'ImmediateConsultation' || result.referral === 'WaitingRoom') && (

                      <button type="button" className="btn" onClick={handleReferToEmergency} disabled={referring}>

                        {referring ? 'Encaminhando...' : 'Encaminhar ao pronto-socorro'}

                      </button>

                    )}

                    {result.referral === 'UbsReferral' && (

                      <button type="button" className="btn btn-secondary" onClick={() => { setStep(4); setSuccess('Paciente orientado para encaminhamento à UBS / atendimento eletivo.'); }}>

                        Registrar orientação UBS

                      </button>

                    )}

                    <button type="button" className="btn btn-secondary" onClick={resetTriage}>Nova triagem</button>

                  </>

                )}

                {step === 4 && (

                  <button type="button" className="btn" onClick={resetTriage}>Nova triagem</button>

                )}

              </div>

            </div>

          )}



          <div className="triage-protocol-grid">

            {manchesterProtocolInfo.map((item) => (

              <div key={item.urgency} className="triage-protocol-card">

                <strong>

                  <span className="manchester-dot" style={{ background: item.hex }} />

                  {item.color}

                </strong>

                <div>{item.description}</div>

                <div style={{ color: 'var(--muted)', marginTop: 4 }}>Prazo: {item.wait}</div>

              </div>

            ))}

          </div>

        </div>



        <div className="card">

          <div className="form-section-title">Sugestão CID-10</div>

          <form onSubmit={handleCidSuggest} className="form-grid">

            <div className="form-field full">

              <label>Texto clínico *</label>

              <input

                value={cidText}

                onChange={(e) => setCidText(e.target.value)}

                placeholder="Ex.: tosse febre gripe"

                required

              />

            </div>

            <div className="form-field full">

              <button type="submit" className="btn btn-secondary">Buscar códigos</button>

            </div>

          </form>

          {cidSuggestions.length > 0 && (

            <table className="data-table" style={{ marginTop: 16 }}>

              <thead><tr><th>Código</th><th>Descrição</th><th>Score</th></tr></thead>

              <tbody>

                {cidSuggestions.map((c) => (

                  <tr key={c.code}>

                    <td><strong>{c.code}</strong></td>

                    <td>{c.description}</td>

                    <td>{c.score}</td>

                  </tr>

                ))}

              </tbody>

            </table>

          )}

        </div>

      </div>
      </>
      )}

      {activeSection === 'epidemiologia' && <AiEpidemiologyPanel />}

      {activeSection === 'historico' && (
      <div className="card-panel appt-panel">

        <div className="card-panel-header">Histórico de triagens — {filteredLogs.length} registro(s)</div>

        <FilterBar>

          <div className="filter-field w-xl">

            <label htmlFor="aiUrgency">Classificação Manchester</label>

            <select id="aiUrgency" value={urgencyFilter} onChange={(e) => setUrgencyFilter(e.target.value)}>

              <option value="">Todas</option>

              {Object.entries(triageUrgencyLabels).map(([k, v]) => (

                <option key={k} value={k}>{v}</option>

              ))}

            </select>

          </div>

          <div className="filter-field grow">

            <label htmlFor="aiSearch">Buscar</label>

            <input

              id="aiSearch"

              placeholder="Paciente, sintomas ou CID-10..."

              value={search}

              onChange={(e) => setSearch(e.target.value)}

            />

          </div>

        </FilterBar>

        <div className="card-panel-body" style={{ padding: 0 }}>

          <table className="data-table">

            <thead>

              <tr><th>Data</th><th>Paciente</th><th>Sintomas</th><th>Manchester</th><th>Prazo</th><th>CID-10</th></tr>

            </thead>

            <tbody>

              {filteredLogs.map((l) => (

                <tr key={l.id}>

                  <td>{formatBrDateTime(l.createdAt)}</td>

                  <td>{l.patientName ?? '—'}</td>

                  <td>{l.symptoms.length > 60 ? `${l.symptoms.slice(0, 60)}...` : l.symptoms}</td>

                  <td>

                    <span className={`badge ${urgencyClass[l.urgency] ?? ''}`}>

                      {l.manchesterColor} — {l.urgencyLabel}

                    </span>

                  </td>

                  <td>{l.maxWaitMinutes === 0 ? 'Imediato' : `${l.maxWaitMinutes} min`}</td>

                  <td>{l.suggestedCid10 ?? '—'}</td>

                </tr>

              ))}

              {filteredLogs.length === 0 && (

                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhuma triagem registrada.</td></tr>

              )}

            </tbody>

          </table>

        </div>

      </div>
      )}

    </>

  );

}



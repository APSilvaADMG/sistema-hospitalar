import { type FormEvent, useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  accessMethodLabels,
  accessPersonTypeLabels,
  accessValidationLabels,
  api,
  kioskTicketTypeLabels,
  vehicleOwnerLabels,
  type AccessControlRecordDto,
  type AccessCredentialDto,
  type AccessIntegrationProfileDto,
  type AccessTurnstileDto,
  type AccessZoneDto,
  type AppointmentDto,
  type AppointmentQrDto,
  type EmployeeSectorAccessDto,
  type FacialBiometricDto,
  type KioskTicketDto,
  type LprReadEventDto,
  type PatientDto,
  type PhysicalAccessDashboardDto,
  type RegisteredVehicleDto,
  type TurnstileValidationResultDto,
} from '../../api/client';
import { KpiCard } from '../../components/KpiCard';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { physicalAccessTabs } from '../../navigation/moduleSections';
import { resolvePageTitle } from '../../navigation/sectionBreadcrumb';
import { useModuleSection } from '../../navigation/useModuleSection';
import { formatBrDateTime } from '../../utils/dateUtils';

type PatrimonyLog = { id: string; item: string; holder: string; action: string; at: string };

export function PhysicalAccessHubPage() {
  const { pathname } = useLocation();
  const { section } = useModuleSection('/acesso-fisico');
  const active = (section || '').split('/')[0];

  const [dashboard, setDashboard] = useState<PhysicalAccessDashboardDto | null>(null);
  const [zones, setZones] = useState<AccessZoneDto[]>([]);
  const [turnstiles, setTurnstiles] = useState<AccessTurnstileDto[]>([]);
  const [records, setRecords] = useState<AccessControlRecordDto[]>([]);
  const [credentials, setCredentials] = useState<AccessCredentialDto[]>([]);
  const [facial, setFacial] = useState<FacialBiometricDto[]>([]);
  const [vehicles, setVehicles] = useState<RegisteredVehicleDto[]>([]);
  const [lprEvents, setLprEvents] = useState<LprReadEventDto[]>([]);
  const [lprLoading, setLprLoading] = useState(false);
  const [tickets, setTickets] = useState<KioskTicketDto[]>([]);
  const [integrations, setIntegrations] = useState<AccessIntegrationProfileDto[]>([]);
  const [employees, setEmployees] = useState<EmployeeSectorAccessDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [turnstileResult, setTurnstileResult] = useState<TurnstileValidationResultDto | null>(null);
  const [appointmentQr, setAppointmentQr] = useState<AppointmentQrDto | null>(null);
  const [actionMsg, setActionMsg] = useState('');

  const [turnstileForm, setTurnstileForm] = useState({ code: 'CAT-MAIN-01', method: 'QrCode', payload: '' });
  const [companionForm, setCompanionForm] = useState({ patientId: '', name: '', zoneId: '', type: 'QrCode' });
  const [facialForm, setFacialForm] = useState({ personType: 'Patient', personName: '', template: '' });
  const [kioskForm, setKioskForm] = useState({ cpf: '', qr: '' });
  const [vehicleForm, setVehicleForm] = useState({
    plate: '', model: '', color: '', ownerCategory: 'Patient', ownerName: '', exempt: false,
  });
  const [lprForm, setLprForm] = useState({ plate: 'ABC1D23', camera: 'Entrada Principal', direction: 'Entry' });
  const [keyLogs, setKeyLogs] = useState<PatrimonyLog[]>([]);
  const [lockerLogs, setLockerLogs] = useState<PatrimonyLog[]>([]);
  const [elevatorLogs, setElevatorLogs] = useState<PatrimonyLog[]>([]);
  const [vendorLogs, setVendorLogs] = useState<PatrimonyLog[]>([]);
  const [keyForm, setKeyForm] = useState({ keyCode: '', holder: '', action: 'Retirada' });
  const [lockerForm, setLockerForm] = useState({ locker: '', holder: '', contents: '' });
  const [elevatorForm, setElevatorForm] = useState({ elevator: 'Elevador A', floor: '', holder: '' });
  const [vendorForm, setVendorForm] = useState({ company: '', contact: '', sector: '', badge: '' });

  function load() {
    Promise.all([
      api.getPhysicalAccessDashboard(),
      api.getAccessZones(),
      api.getAccessTurnstiles(),
      api.getAccessRecords(200),
      api.getAccessCredentials(),
      api.getFacialEnrollments(),
      api.getRegisteredVehicles(),
      api.getLprEvents(),
      api.getKioskTickets(),
      api.getAccessIntegrations(),
      api.getEmployeeSectorAccess(),
      api.getPatients('', 1),
      api.getAppointments(),
    ]).then(([dash, z, t, r, c, f, v, l, tk, integ, emp, p, a]) => {
      setDashboard(dash);
      setZones(Array.isArray(z) ? z : []);
      setTurnstiles(Array.isArray(t) ? t : []);
      setRecords(Array.isArray(r) ? r : []);
      setCredentials(Array.isArray(c) ? c : []);
      setFacial(Array.isArray(f) ? f : []);
      setVehicles(Array.isArray(v) ? v : []);
      setLprEvents(Array.isArray(l) ? l : []);
      setTickets(Array.isArray(tk) ? tk : []);
      setIntegrations(Array.isArray(integ) ? integ : []);
      setEmployees(Array.isArray(emp) ? emp : []);
      setPatients(Array.isArray(p?.items) ? p.items : []);
      setAppointments(Array.isArray(a) ? a : []);
    }).catch(console.error);
  }

  useEffect(() => {
    if (active !== 'lpr') return;
    setLprLoading(true);
    api.getLprEvents()
      .then((events) => setLprEvents(Array.isArray(events) ? events : []))
      .catch(console.error)
      .finally(() => setLprLoading(false));
  }, [active]);

  useEffect(() => { load(); }, []);

  const todayAppts = appointments.filter(
    (a) => a.scheduledAt?.startsWith(new Date().toISOString().slice(0, 10)),
  );

  const lprEventList = Array.isArray(lprEvents) ? lprEvents : [];

  async function handleTurnstileValidate(e: FormEvent) {
    e.preventDefault();
    const result = await api.validateTurnstile({
      turnstileCode: turnstileForm.code,
      method: turnstileForm.method,
      payload: turnstileForm.payload,
    });
    setTurnstileResult(result);
    load();
  }

  async function handleCompanionIssue(e: FormEvent) {
    e.preventDefault();
    setActionMsg('');
    try {
      await api.issueCompanionCredential({
        patientId: companionForm.patientId,
        companionName: companionForm.name,
        credentialType: companionForm.type,
        allowedZoneId: companionForm.zoneId || undefined,
      });
      setActionMsg('Credencial de acompanhante emitida.');
      load();
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : 'Erro ao emitir credencial.');
    }
  }

  async function handleFacialEnroll(e: FormEvent) {
    e.preventDefault();
    await api.enrollFacial({
      personType: facialForm.personType,
      personName: facialForm.personName,
      templatePayload: facialForm.template || `mock-template-${Date.now()}`,
    });
    setActionMsg('Template facial cadastrado.');
    load();
  }

  async function handleKioskCheckIn(e: FormEvent) {
    e.preventDefault();
    const result = await api.kioskCheckIn({
      cpf: kioskForm.cpf || undefined,
      qrPayload: kioskForm.qr || undefined,
    });
    setActionMsg(result.message);
    if (result.success) load();
  }

  async function handleVehicleRegister(e: FormEvent) {
    e.preventDefault();
    try {
      await api.registerAccessVehicle({
        plate: vehicleForm.plate,
        model: vehicleForm.model,
        color: vehicleForm.color,
        ownerCategory: vehicleForm.ownerCategory,
        ownerName: vehicleForm.ownerName,
        parkingExempt: vehicleForm.exempt,
      });
      setActionMsg('Veículo cadastrado.');
      load();
    } catch (err) {
      setActionMsg(err instanceof Error ? err.message : 'Erro ao cadastrar veículo.');
    }
  }

  async function handleLprSimulate(e: FormEvent) {
    e.preventDefault();
    const result = await api.processLprRead({
      plate: lprForm.plate,
      cameraLocation: lprForm.camera,
      direction: lprForm.direction,
    });
    setActionMsg(result.message);
    load();
  }

  async function loadAppointmentQr(apptId: string) {
    const qr = await api.getAppointmentQr(apptId);
    setAppointmentQr(qr);
    setTurnstileForm((f) => ({ ...f, payload: qr.qrPayload }));
  }

  return (
    <>
      <PageHeader
        eyebrow="Segurança e Controle de Acesso"
        title={resolvePageTitle(pathname)}
        subtitle="Controle de Acesso Físico e Mobilidade Hospitalar — catracas, facial, totens, estacionamento e LPR."
      />

      <ModuleNav basePath="/acesso-fisico" tabs={physicalAccessTabs} contextId="physicalAccess" />

      {actionMsg && (
        <div className="alert alert-info" style={{ marginTop: 12 }}>{actionMsg}</div>
      )}

      {(active === '' || active === 'monitoramento') && dashboard && (
        <>
          <div className="kpi-grid" style={{ marginTop: 16 }}>
            <KpiCard label="Pessoas no complexo (est.)" value={dashboard.peopleInsideEstimate} variant="primary" />
            <KpiCard label="Acessos liberados hoje" value={dashboard.accessGrantedToday} variant="success" />
            <KpiCard label="Acessos negados hoje" value={dashboard.accessDeniedToday} variant="danger" />
            <KpiCard label="Acompanhantes ativos" value={dashboard.activeCompanions} />
            <KpiCard label="Veículos no pátio" value={dashboard.vehiclesInside} />
            <KpiCard label="Cadastros faciais" value={dashboard.facialEnrollments} />
          </div>

          <div className="card-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Monitoramento em tempo real — últimos acessos</div>
            <AccessTable records={dashboard.recentAccess ?? []} />
          </div>
        </>
      )}

      {active === 'visitantes' && (
        <div className="card-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Controle de visitantes</div>
          <p style={{ padding: '12px 16px', color: 'var(--muted)' }}>
            Cadastro com foto, crachá e vínculo ao paciente internado. Integrado ao módulo de portaria.
          </p>
          <Link to="/seguranca" className="btn" style={{ margin: '0 16px 16px' }}>Abrir portaria completa</Link>
        </div>
      )}

      {active === 'catracas' && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel">
            <div className="card-panel-header">Catracas cadastradas</div>
            <table className="data-table">
              <thead><tr><th>Código</th><th>Nome</th><th>Setor</th><th>Integração</th></tr></thead>
              <tbody>
                {turnstiles.map((t) => (
                  <tr key={t.id}>
                    <td><code>{t.code}</code></td>
                    <td>{t.name}</td>
                    <td>{t.zoneName ?? '—'}</td>
                    <td>{t.integrationVendor ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="card-panel">
            <div className="card-panel-header">Simular leitura na catraca</div>
            <form onSubmit={handleTurnstileValidate} className="form-grid form-panel">
              <div className="form-field">
                <label>Catraca</label>
                <select value={turnstileForm.code} onChange={(e) => setTurnstileForm({ ...turnstileForm, code: e.target.value })}>
                  {turnstiles.map((t) => <option key={t.id} value={t.code}>{t.name}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Método</label>
                <select value={turnstileForm.method} onChange={(e) => setTurnstileForm({ ...turnstileForm, method: e.target.value })}>
                  {Object.entries(accessMethodLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                </select>
              </div>
              <div className="form-field full">
                <label>Payload (QR / RFID)</label>
                <input value={turnstileForm.payload} onChange={(e) => setTurnstileForm({ ...turnstileForm, payload: e.target.value })} placeholder="HMS-APT:..." />
              </div>
              <div className="form-field full">
                <strong>Consultas de hoje — gerar QR</strong>
                <div className="table-actions" style={{ marginTop: 8 }}>
                  {todayAppts.slice(0, 5).map((a) => (
                    <button key={a.id} type="button" className="btn btn-secondary btn-sm" onClick={() => loadAppointmentQr(a.id)}>
                      {a.patientName}
                    </button>
                  ))}
                </div>
                {appointmentQr && (
                  <p className="form-hint" style={{ marginTop: 8 }}>
                    QR: <code>{appointmentQr.qrPayload}</code> — {appointmentQr.patientName}
                  </p>
                )}
              </div>
              <div className="form-actions">
                <button type="submit" className="btn">Validar acesso</button>
              </div>
              {turnstileResult && (
                <p className="form-field full" style={{ color: turnstileResult.granted ? 'var(--success)' : 'var(--danger)' }}>
                  {turnstileResult.message} ({accessValidationLabels[turnstileResult.result] ?? turnstileResult.result})
                </p>
              )}
            </form>
          </div>
        </div>
      )}

      {active === 'facial' && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel">
            <div className="card-panel-header">Templates biométricos</div>
            <table className="data-table">
              <thead><tr><th>Pessoa</th><th>Tipo</th><th>Status</th><th>Cadastro</th></tr></thead>
              <tbody>
                {facial.map((f) => (
                  <tr key={f.id}>
                    <td>{f.personName}</td>
                    <td>{accessPersonTypeLabels[f.personType] ?? f.personType}</td>
                    <td>{f.status}</td>
                    <td>{formatBrDateTime(f.enrolledAt)}</td>
                  </tr>
                ))}
                {facial.length === 0 && <tr><td colSpan={4} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Nenhum cadastro facial.</td></tr>}
              </tbody>
            </table>
            <p style={{ padding: 12, fontSize: 13, color: 'var(--muted)' }}>
              Aplicações: entrada hospitalar, prescrição (face + senha), farmácia (face + token), ponto (face + geolocalização), assinaturas (face + certificado).
            </p>
          </div>
          <div className="card-panel">
            <div className="card-panel-header">Cadastro biométrico facial</div>
            <form onSubmit={handleFacialEnroll} className="form-grid form-panel">
              <div className="form-field">
                <label>Tipo</label>
                <select value={facialForm.personType} onChange={(e) => setFacialForm({ ...facialForm, personType: e.target.value })}>
                  {Object.entries(accessPersonTypeLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Nome</label>
                <input value={facialForm.personName} onChange={(e) => setFacialForm({ ...facialForm, personName: e.target.value })} required />
              </div>
              <div className="form-field full">
                <label>Template (mock)</label>
                <input value={facialForm.template} onChange={(e) => setFacialForm({ ...facialForm, template: e.target.value })} placeholder="Hash biométrico simulado" />
              </div>
              <div className="form-actions">
                <button type="submit" className="btn">Cadastrar face</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {active === 'totens' && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel">
            <div className="card-panel-header">Check-in de consultas (totem)</div>
            <form onSubmit={handleKioskCheckIn} className="form-grid form-panel">
              <div className="form-field">
                <label>CPF do paciente</label>
                <input value={kioskForm.cpf} onChange={(e) => setKioskForm({ ...kioskForm, cpf: e.target.value })} placeholder="000.000.000-00" />
              </div>
              <div className="form-field">
                <label>ou QR Code da consulta</label>
                <input value={kioskForm.qr} onChange={(e) => setKioskForm({ ...kioskForm, qr: e.target.value })} placeholder="HMS-APT:..." />
              </div>
              <div className="form-actions">
                <button type="submit" className="btn">Confirmar presença</button>
              </div>
            </form>
            <p style={{ padding: '0 16px 16px', fontSize: 13, color: 'var(--muted)' }}>
              Também suporta reconhecimento facial e emissão de senhas para consulta, exame, internação, emergência e laboratório.
            </p>
          </div>
          <div className="card-panel">
            <div className="card-panel-header">Senhas emitidas</div>
            <table className="data-table">
              <thead><tr><th>Senha</th><th>Tipo</th><th>Paciente</th><th>Setor</th><th>Horário</th><th></th></tr></thead>
              <tbody>
                {tickets.map((t) => (
                  <tr key={t.id}>
                    <td><strong>{t.ticketNumber}</strong></td>
                    <td>{kioskTicketTypeLabels[t.ticketType] ?? t.ticketType}</td>
                    <td>{t.patientName ?? '—'}</td>
                    <td>{t.sector ?? '—'}</td>
                    <td>{formatBrDateTime(t.issuedAt)}</td>
                    <td>
                      {!t.called ? (
                        <button
                          type="button"
                          className="btn btn-sm"
                          onClick={() => api.callKioskTicketOnTv(t.id, { destination: t.sector ?? 'Atendimento' }).then(() => load()).catch(console.error)}
                        >
                          Chamar na TV
                        </button>
                      ) : '—'}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'estacionamento' && (
        <>
          <div className="card-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Estacionamento e totens</div>
            <p style={{ padding: '12px 16px', color: 'var(--muted)' }}>
              Entrada por placa (OCR/LPR), QR Code ou face. Saída com cálculo de permanência, isenção e cobrança.
            </p>
            <Link to="/estacionamento" className="btn" style={{ margin: '0 16px 16px' }}>Abrir módulo de estacionamento</Link>
            <table className="data-table">
              <thead><tr><th>Placa</th><th>Proprietário</th><th>Categoria</th><th>Isenção</th></tr></thead>
              <tbody>
                {vehicles.map((v) => (
                  <tr key={v.id}>
                    <td>{v.plate}</td>
                    <td>{v.ownerName}</td>
                    <td>{vehicleOwnerLabels[v.ownerCategory] ?? v.ownerCategory}</td>
                    <td>{v.parkingExempt ? 'Sim' : 'Não'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Cadastrar veículo</div>
            <form onSubmit={handleVehicleRegister} className="form-grid cols-3 form-panel">
              <div className="form-field">
                <label>Placa</label>
                <input value={vehicleForm.plate} onChange={(e) => setVehicleForm({ ...vehicleForm, plate: e.target.value })} required />
              </div>
              <div className="form-field">
                <label>Modelo</label>
                <input value={vehicleForm.model} onChange={(e) => setVehicleForm({ ...vehicleForm, model: e.target.value })} />
              </div>
              <div className="form-field">
                <label>Cor</label>
                <input value={vehicleForm.color} onChange={(e) => setVehicleForm({ ...vehicleForm, color: e.target.value })} />
              </div>
              <div className="form-field">
                <label>Categoria</label>
                <select value={vehicleForm.ownerCategory} onChange={(e) => setVehicleForm({ ...vehicleForm, ownerCategory: e.target.value })}>
                  {Object.entries(vehicleOwnerLabels).map(([k, v]) => <option key={k} value={k}>{v}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Proprietário</label>
                <input value={vehicleForm.ownerName} onChange={(e) => setVehicleForm({ ...vehicleForm, ownerName: e.target.value })} required />
              </div>
              <div className="form-field checkbox align-end">
                <label>
                  <input type="checkbox" checked={vehicleForm.exempt} onChange={(e) => setVehicleForm({ ...vehicleForm, exempt: e.target.checked })} />
                  Isento
                </label>
              </div>
              <div className="form-actions">
                <button type="submit" className="btn">Cadastrar</button>
              </div>
            </form>
          </div>
        </>
      )}

      {active === 'lpr' && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="appt-panel-toolbar">
            <div>
              <strong style={{ fontSize: '0.95rem' }}>Leitura de placas (LPR)</strong>
              <p className="form-hint" style={{ margin: '4px 0 0' }}>
                Simule OCR/LPR na entrada ou saída e acompanhe cancelas e proprietários.
              </p>
            </div>
          </div>

          <div className="grid-2" style={{ gap: 16, padding: '0 16px 16px' }}>
            <div className="card-panel">
              <div className="card-panel-header">Simular leitura LPR</div>
              <form onSubmit={handleLprSimulate} className="form-grid form-panel">
                <div className="form-field">
                  <label>Placa</label>
                  <input value={lprForm.plate} onChange={(e) => setLprForm({ ...lprForm, plate: e.target.value.toUpperCase() })} placeholder="ABC1D23" />
                </div>
                <div className="form-field">
                  <label>Câmera</label>
                  <input value={lprForm.camera} onChange={(e) => setLprForm({ ...lprForm, camera: e.target.value })} placeholder="Entrada Principal" />
                </div>
                <div className="form-field">
                  <label>Direção</label>
                  <select value={lprForm.direction} onChange={(e) => setLprForm({ ...lprForm, direction: e.target.value })}>
                    <option value="Entry">Entrada</option>
                    <option value="Exit">Saída</option>
                  </select>
                </div>
                <div className="form-actions">
                  <button type="submit" className="btn">Processar leitura</button>
                </div>
              </form>
            </div>
            <div className="card-panel">
              <div className="card-panel-header">Eventos LPR recentes</div>
              <div className="card-panel-body" style={{ padding: 0 }}>
                {lprLoading ? (
                  <p className="form-hint" style={{ padding: 16 }}>Carregando eventos...</p>
                ) : (
                  <table className="data-table">
                    <thead><tr><th>Placa</th><th>Câmera</th><th>Cancela</th><th>Proprietário</th><th>Horário</th></tr></thead>
                    <tbody>
                      {lprEventList.length === 0 ? (
                        <tr>
                          <td colSpan={5} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                            Nenhuma leitura LPR registrada. Use o formulário ao lado para simular uma placa.
                          </td>
                        </tr>
                      ) : (
                        lprEventList.map((e) => (
                          <tr key={e.id}>
                            <td><strong>{e.plate}</strong></td>
                            <td>{e.cameraLocation}</td>
                            <td>
                              <span className={`badge badge-${e.gateOpened ? 'success' : 'danger'}`}>
                                {e.gateOpened ? 'Aberta' : 'Bloqueada'}
                              </span>
                            </td>
                            <td>{e.ownerName ?? '—'}</td>
                            <td>{formatBrDateTime(e.readAt)}</td>
                          </tr>
                        ))
                      )}
                    </tbody>
                  </table>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {active === 'credenciais' && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel">
            <div className="card-panel-header">Credenciais emitidas</div>
            <table className="data-table">
              <thead><tr><th>Titular</th><th>Tipo</th><th>Token</th><th>Setor</th><th>Validade</th></tr></thead>
              <tbody>
                {credentials.map((c) => (
                  <tr key={c.id}>
                    <td>{c.holderName}</td>
                    <td>{accessPersonTypeLabels[c.personType] ?? c.personType}</td>
                    <td><code style={{ fontSize: 11 }}>{c.token.slice(0, 24)}…</code></td>
                    <td>{c.zoneName ?? 'Geral'}</td>
                    <td>{c.validUntil ? formatBrDateTime(c.validUntil) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card-panel">
            <div className="card-panel-header">Emitir credencial de acompanhante</div>
            <form onSubmit={handleCompanionIssue} className="form-grid form-panel">
              <div className="form-field full">
                <label>Paciente</label>
                <select value={companionForm.patientId} onChange={(e) => setCompanionForm({ ...companionForm, patientId: e.target.value })} required>
                  <option value="">Selecione</option>
                  {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Nome do acompanhante</label>
                <input value={companionForm.name} onChange={(e) => setCompanionForm({ ...companionForm, name: e.target.value })} required />
              </div>
              <div className="form-field">
                <label>Setor autorizado</label>
                <select value={companionForm.zoneId} onChange={(e) => setCompanionForm({ ...companionForm, zoneId: e.target.value })}>
                  <option value="">Todos permitidos</option>
                  {zones.map((z) => <option key={z.id} value={z.id}>{z.name}</option>)}
                </select>
              </div>
              <div className="form-field">
                <label>Formato</label>
                <select value={companionForm.type} onChange={(e) => setCompanionForm({ ...companionForm, type: e.target.value })}>
                  <option value="QrCode">QR Code</option>
                  <option value="Rfid">Cartão RFID</option>
                  <option value="FacialLinked">Vinculado à face</option>
                </select>
              </div>
              <div className="form-actions">
                <button type="submit" className="btn">Emitir credencial</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {active === 'setores' && (
        <div className="grid-2 hub-form-split">
          <div className="card-panel">
            <div className="card-panel-header">Setores e zonas de acesso</div>
            <table className="data-table">
              <thead><tr><th>Código</th><th>Setor</th><th>Prédio</th><th>Restrito</th></tr></thead>
              <tbody>
                {zones.map((z) => (
                  <tr key={z.id}>
                    <td><code>{z.code}</code></td>
                    <td>{z.name}</td>
                    <td>{z.building ?? '—'}{z.floor ? ` / ${z.floor}º` : ''}</td>
                    <td>{z.requiresAuthorization ? 'Sim' : 'Não'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="card-panel">
            <div className="card-panel-header">Colaboradores — jornada e setor (RH)</div>
            <table className="data-table">
              <thead><tr><th>Nome</th><th>Departamento</th><th>Em escala</th><th>Último acesso</th></tr></thead>
              <tbody>
                {employees.map((e) => (
                  <tr key={e.employeeId}>
                    <td>{e.employeeName}</td>
                    <td>{e.department}</td>
                    <td>{e.onShift ? 'Sim' : 'Não'}</td>
                    <td>{e.lastAccess ? formatBrDateTime(e.lastAccess) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'auditoria' && (
        <div className="card-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Auditoria de acessos — {records.length} registro(s)</div>
          <AccessTable records={records} />
        </div>
      )}

      {active === 'integracoes' && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="card-panel-header">Integrações recomendadas (mock)</div>
          <div className="card-panel-body" style={{ padding: 0 }}>
            <table className="data-table">
              <thead><tr><th>Fornecedor</th><th>Categoria</th><th>Descrição</th><th>Status</th></tr></thead>
              <tbody>
                {(Array.isArray(integrations) ? integrations : []).map((i) => (
                  <tr key={`${i.vendor}-${i.category}`}>
                    <td><strong>{i.vendor}</strong></td>
                    <td>{i.category}</td>
                    <td>{i.description}</td>
                    <td>{i.mockEnabled ? 'Mock ativo' : 'Produção'}</td>
                  </tr>
                ))}
                {integrations.length === 0 && (
                  <tr>
                    <td colSpan={4} style={{ textAlign: 'center', padding: 24, color: 'var(--muted)' }}>
                      Nenhuma integração cadastrada. Verifique se a API de acesso físico está disponível.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {active === 'chaves' && (
        <div style={{ marginTop: 16, display: 'grid', gap: 16 }}>
          <form className="card form-grid" onSubmit={(e) => {
            e.preventDefault();
            if (!keyForm.keyCode.trim() || !keyForm.holder.trim()) return;
            setKeyLogs((prev) => [{
              id: crypto.randomUUID(),
              item: keyForm.keyCode,
              holder: keyForm.holder,
              action: keyForm.action,
              at: new Date().toISOString(),
            }, ...prev]);
            setKeyForm({ keyCode: '', holder: '', action: 'Retirada' });
            setActionMsg('Movimentação de chave registrada.');
          }}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Controle de chaves</h3>
            <div className="form-field"><label>Código da chave</label>
              <input value={keyForm.keyCode} onChange={(e) => setKeyForm({ ...keyForm, keyCode: e.target.value })} placeholder="Ex.: CH-SALA-12" required />
            </div>
            <div className="form-field"><label>Responsável</label>
              <input value={keyForm.holder} onChange={(e) => setKeyForm({ ...keyForm, holder: e.target.value })} required />
            </div>
            <div className="form-field"><label>Movimentação</label>
              <select value={keyForm.action} onChange={(e) => setKeyForm({ ...keyForm, action: e.target.value })}>
                <option>Retirada</option><option>Devolução</option>
              </select>
            </div>
            <div className="form-actions"><button className="btn" type="submit">Registrar</button></div>
          </form>
          <PatrimonyTable logs={keyLogs} columns={['Chave', 'Responsável', 'Movimento']} />
        </div>
      )}

      {active === 'armarios' && (
        <div style={{ marginTop: 16, display: 'grid', gap: 16 }}>
          <form className="card form-grid" onSubmit={(e) => {
            e.preventDefault();
            if (!lockerForm.locker.trim()) return;
            setLockerLogs((prev) => [{
              id: crypto.randomUUID(),
              item: lockerForm.locker,
              holder: lockerForm.holder,
              action: lockerForm.contents || 'Armário atribuído',
              at: new Date().toISOString(),
            }, ...prev]);
            setLockerForm({ locker: '', holder: '', contents: '' });
            setActionMsg('Armário registrado.');
          }}>
            <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Controle de armários</h3>
            <div className="form-field"><label>Armário / gaveta</label>
              <input value={lockerForm.locker} onChange={(e) => setLockerForm({ ...lockerForm, locker: e.target.value })} required />
            </div>
            <div className="form-field"><label>Responsável</label>
              <input value={lockerForm.holder} onChange={(e) => setLockerForm({ ...lockerForm, holder: e.target.value })} />
            </div>
            <div className="form-field full"><label>Conteúdo / observação</label>
              <input value={lockerForm.contents} onChange={(e) => setLockerForm({ ...lockerForm, contents: e.target.value })} />
            </div>
            <div className="form-actions"><button className="btn" type="submit">Registrar</button></div>
          </form>
          <PatrimonyTable logs={lockerLogs} columns={['Armário', 'Responsável', 'Conteúdo']} />
        </div>
      )}

      {active === 'elevadores' && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="appt-panel-toolbar">
            <div>
              <strong style={{ fontSize: '0.95rem' }}>Controle de elevadores</strong>
              <p className="form-hint" style={{ margin: '4px 0 0' }}>
                Autorização de andar por crachá, face ou credencial — registro patrimonial.
              </p>
            </div>
          </div>
          <div className="grid-2" style={{ gap: 16, padding: '0 16px 16px' }}>
            <div className="card-panel">
              <div className="card-panel-header">Registrar acesso ao elevador</div>
              <form
                className="form-grid form-panel"
                onSubmit={(e) => {
                  e.preventDefault();
                  if (!elevatorForm.floor.trim() || !elevatorForm.holder.trim()) return;
                  setElevatorLogs((prev) => [{
                    id: crypto.randomUUID(),
                    item: elevatorForm.elevator,
                    holder: elevatorForm.holder,
                    action: `Andar ${elevatorForm.floor}`,
                    at: new Date().toISOString(),
                  }, ...prev]);
                  setElevatorForm({ elevator: 'Elevador A', floor: '', holder: '' });
                  setActionMsg('Acesso a elevador registrado.');
                }}
              >
                <div className="form-field">
                  <label>Elevador</label>
                  <select value={elevatorForm.elevator} onChange={(e) => setElevatorForm({ ...elevatorForm, elevator: e.target.value })}>
                    <option>Elevador A</option>
                    <option>Elevador B</option>
                    <option>Elevador UTI</option>
                    <option>Elevador Centro Cirúrgico</option>
                  </select>
                </div>
                <div className="form-field">
                  <label>Andar autorizado</label>
                  <input value={elevatorForm.floor} onChange={(e) => setElevatorForm({ ...elevatorForm, floor: e.target.value })} placeholder="Ex.: 3" required />
                </div>
                <div className="form-field">
                  <label>Portador</label>
                  <input value={elevatorForm.holder} onChange={(e) => setElevatorForm({ ...elevatorForm, holder: e.target.value })} placeholder="Nome ou crachá" required />
                </div>
                <div className="form-actions">
                  <button className="btn" type="submit">Registrar acesso</button>
                </div>
              </form>
            </div>
            <PatrimonyTable logs={elevatorLogs} columns={['Elevador', 'Portador', 'Destino']} />
          </div>
        </div>
      )}

      {active === 'terceiros' && (
        <div className="card-panel appt-panel hub-form-split">
          <div className="appt-panel-toolbar">
            <div>
              <strong style={{ fontSize: '0.95rem' }}>Controle de terceiros</strong>
              <p className="form-hint" style={{ margin: '4px 0 0' }}>
                Prestadores de serviço, manutenção e fornecedores — entrada com crachá e setor autorizado.
              </p>
            </div>
            <Link to="/seguranca" className="btn btn-secondary btn-sm">Portaria</Link>
          </div>
          <div className="grid-2" style={{ gap: 16, padding: '0 16px 16px' }}>
            <div className="card-panel">
              <div className="card-panel-header">Registrar prestador</div>
              <form
                className="form-grid form-panel"
                onSubmit={(e) => {
                  e.preventDefault();
                  if (!vendorForm.company.trim()) return;
                  setVendorLogs((prev) => [{
                    id: crypto.randomUUID(),
                    item: vendorForm.company,
                    holder: vendorForm.contact,
                    action: `${vendorForm.sector || 'Geral'} · Crachá ${vendorForm.badge || '—'}`,
                    at: new Date().toISOString(),
                  }, ...prev]);
                  setVendorForm({ company: '', contact: '', sector: '', badge: '' });
                  setActionMsg('Prestador de serviço registrado.');
                }}
              >
                <div className="form-field">
                  <label>Empresa</label>
                  <input value={vendorForm.company} onChange={(e) => setVendorForm({ ...vendorForm, company: e.target.value })} placeholder="Razão social ou nome fantasia" required />
                </div>
                <div className="form-field">
                  <label>Contato responsável</label>
                  <input value={vendorForm.contact} onChange={(e) => setVendorForm({ ...vendorForm, contact: e.target.value })} placeholder="Nome e telefone" />
                </div>
                <div className="form-field">
                  <label>Setor autorizado</label>
                  <input value={vendorForm.sector} onChange={(e) => setVendorForm({ ...vendorForm, sector: e.target.value })} placeholder="Ex.: Manutenção, TI, Limpeza" />
                </div>
                <div className="form-field">
                  <label>Nº crachá</label>
                  <input value={vendorForm.badge} onChange={(e) => setVendorForm({ ...vendorForm, badge: e.target.value })} placeholder="Emitido na portaria" />
                </div>
                <div className="form-actions">
                  <button className="btn" type="submit">Registrar entrada</button>
                </div>
              </form>
            </div>
            <PatrimonyTable logs={vendorLogs} columns={['Empresa', 'Contato', 'Autorização']} />
          </div>
        </div>
      )}

      {active === 'central' && (
        <div style={{ marginTop: 16 }}>
          <div className="kpi-grid">
            <KpiCard label="Chaves em uso" value={keyLogs.filter((l) => l.action === 'Retirada').length} />
            <KpiCard label="Armários" value={lockerLogs.length} variant="info" />
            <KpiCard label="Elevadores (hoje)" value={elevatorLogs.length} variant="warning" />
            <KpiCard label="Terceiros ativos" value={vendorLogs.length} variant="primary" />
          </div>
          <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
            <div className="card-panel-header">Central patrimonial — resumo</div>
            <p style={{ padding: '12px 16px', margin: 0, color: 'var(--muted)' }}>
              Consolidação de chaves, armários, elevadores e prestadores. Eventos de catraca e facial continuam em Monitoramento e Auditoria.
            </p>
            <div style={{ padding: 16, display: 'flex', gap: 8, flexWrap: 'wrap' }}>
              <Link to="/acesso-fisico/monitoramento" className="btn btn-secondary btn-sm">Monitoramento</Link>
              <Link to="/acesso-fisico/auditoria" className="btn btn-secondary btn-sm">Auditoria</Link>
              <Link to="/acesso-fisico/chaves" className="btn btn-secondary btn-sm">Chaves</Link>
            </div>
          </div>
        </div>
      )}

    </>
  );
}

function PatrimonyTable({
  logs,
  columns,
}: {
  logs: { id: string; item: string; holder: string; action: string; at: string }[];
  columns: [string, string, string];
}) {
  const rows = Array.isArray(logs) ? logs : [];
  return (
    <div className="card-panel appt-panel">
      <div className="card-panel-header">Registros recentes</div>
      <div className="card-panel-body" style={{ padding: 0 }}>
        <table className="data-table">
          <thead><tr><th>Horário</th><th>{columns[0]}</th><th>{columns[1]}</th><th>{columns[2]}</th></tr></thead>
          <tbody>
            {rows.map((l) => (
              <tr key={l.id}>
                <td>{formatBrDateTime(l.at)}</td>
                <td>{l.item}</td>
                <td>{l.holder}</td>
                <td>{l.action}</td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr><td colSpan={4} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Nenhum registro.</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function AccessTable({ records }: { records: AccessControlRecordDto[] }) {
  const rows = Array.isArray(records) ? records : [];
  return (
    <table className="data-table">
      <thead>
        <tr><th>Horário</th><th>Pessoa</th><th>Tipo</th><th>Método</th><th>Resultado</th><th>Local</th></tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={r.id}>
            <td>{formatBrDateTime(r.occurredAt)}</td>
            <td>{r.personName}</td>
            <td>{accessPersonTypeLabels[r.personType] ?? r.personType}</td>
            <td>{accessMethodLabels[r.method] ?? r.method}</td>
            <td style={{ color: r.result === 'Granted' ? 'var(--success)' : 'var(--danger)' }}>
              {accessValidationLabels[r.result] ?? r.result}
            </td>
            <td>{r.location ?? '—'}</td>
          </tr>
        ))}
        {rows.length === 0 && (
          <tr><td colSpan={6} style={{ textAlign: 'center', padding: 20, color: 'var(--muted)' }}>Nenhum registro.</td></tr>
        )}
      </tbody>
    </table>
  );
}

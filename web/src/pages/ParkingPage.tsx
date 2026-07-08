import { type FormEvent, useEffect, useState } from 'react';

import {

  api,

  type ParkingGateExitResultDto,

  type PatientDto,

  type ParkingSessionDto,

  type ParkingZoneDto,

} from '../api/client';

import { KpiCard } from '../components/KpiCard';

import { Modal } from '../components/Modal';

import { PageHeader } from '../components/PageHeader';
import { formatBrDateTime } from '../utils/dateUtils';

import { useAuth } from '../auth/AuthContext';

import {

  printParkingEntryTicket,

  printParkingExitReceipt,

  printParkingPaymentReceipt,

} from '../utils/printTemplates';



export function ParkingPage() {

  const { hasPermission } = useAuth();

  const [zones, setZones] = useState<ParkingZoneDto[]>([]);

  const [sessions, setSessions] = useState<ParkingSessionDto[]>([]);

  const [recentCompleted, setRecentCompleted] = useState<ParkingSessionDto[]>([]);

  const [patients, setPatients] = useState<PatientDto[]>([]);

  const [autoPrintEntry, setAutoPrintEntry] = useState(true);

  const [autoPrintPayment, setAutoPrintPayment] = useState(true);

  const [autoPrintExit, setAutoPrintExit] = useState(true);

  const [form, setForm] = useState({ zoneId: '', vehiclePlate: '', patientId: '' });

  const [showModal, setShowModal] = useState(false);

  const [qrInput, setQrInput] = useState('');

  const [gateResult, setGateResult] = useState<ParkingGateExitResultDto | null>(null);

  const [gateLoading, setGateLoading] = useState(false);

  const [error, setError] = useState('');

  const [success, setSuccess] = useState('');



  useEffect(() => { load(); }, []);



  async function load() {

    const [z, active, all, patientList] = await Promise.all([

      api.getParkingZones(),

      api.getParkingSessions(true),

      api.getParkingSessions(false),

      api.getPatients('', 1),

    ]);

    setZones(z);

    setSessions(active);

    setPatients(patientList.items);

    setRecentCompleted(all.filter((s) => s.status === 'Completed').slice(0, 10));

  }



  function zoneRate(zoneId: string) {

    return zones.find((z) => z.id === zoneId)?.hourlyRate ?? 0;

  }



  async function handleCheckIn(e: FormEvent) {

    e.preventDefault();

    setError('');

    try {

      const created = await api.checkInParking({

        zoneId: form.zoneId,

        vehiclePlate: form.vehiclePlate,

        patientId: form.patientId || undefined,

      });

      setForm({ zoneId: '', vehiclePlate: '', patientId: '' });

      setShowModal(false);

      await load();

      if (autoPrintEntry) {

        await printParkingEntryTicket(created, zoneRate(created.zoneId), true);

      }

      setSuccess(`Entrada registrada — placa ${created.vehiclePlate}. Ticket com QR Code gerado.`);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao registrar entrada.');

    }

  }



  async function handlePay(session: ParkingSessionDto) {

    setError('');

    setSuccess('');

    try {

      const paid = await api.payParking(session.id);

      await load();

      if (autoPrintPayment) {

        await printParkingPaymentReceipt(paid, zoneRate(paid.zoneId));

      }

      setSuccess(`Pagamento registrado — ${paid.vehiclePlate}. Cancela liberada após leitura do QR Code.`);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao registrar pagamento.');

    }

  }



  async function handleCheckOut(session: ParkingSessionDto) {

    setError('');

    setSuccess('');

    try {

      const checkedOut = await api.checkOutParking(session.id);

      await load();

      if (checkedOut && autoPrintExit) {

        printParkingExitReceipt(checkedOut, zoneRate(checkedOut.zoneId));

      }

      setSuccess(`Saída manual registrada — ${session.vehiclePlate}.`);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro na saída.');

    }

  }



  async function handleGateScan(e: FormEvent) {

    e.preventDefault();

    if (!qrInput.trim()) return;

    setGateLoading(true);

    setGateResult(null);

    setError('');

    try {

      const result = await api.processParkingGateExit(qrInput.trim());

      setGateResult(result);

      if (result.allowed) {

        setSuccess(result.message);

        setQrInput('');

        await load();

      } else {

        setError(result.message);

      }

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro na cancela.');

    } finally {

      setGateLoading(false);

    }

  }



  if (!hasPermission('patients.create', 'reports.read')) {

    return <div className="card">Acesso restrito à recepção.</div>;

  }



  const totalSpots = zones.reduce((s, z) => s + z.totalSpots, 0);

  const occupied = zones.reduce((s, z) => s + z.occupiedSpots, 0);

  const awaitingPayment = sessions.filter((s) => !s.isPaid).length;



  return (

    <>

      <PageHeader

        eyebrow="Infraestrutura"

        title="Estacionamento"

        subtitle="Tickets com QR Code, pagamento no caixa e liberação na cancela somente após quitação."

      >

        <button className="btn" type="button" onClick={() => setShowModal(true)}>+ Registrar entrada</button>

      </PageHeader>



      {error && <div className="alert alert-error">{error}</div>}

      {success && <div className="alert alert-success">{success}</div>}



      <div className="kpi-grid">

        <KpiCard label="Vagas ocupadas" value={`${occupied}/${totalSpots}`} variant="primary" />

        <KpiCard label="Aguardando pagamento" value={awaitingPayment} variant="warning" />

        <KpiCard label="Veículos no pátio" value={sessions.length} variant="success" />

        {zones.map((z) => (

          <KpiCard

            key={z.id}

            label={z.name}

            value={`${z.occupiedSpots}/${z.totalSpots}`}

            variant={z.occupiedSpots >= z.totalSpots ? 'danger' : 'neutral'}

          />

        ))}

      </div>



      <div className="grid-2" style={{ marginTop: 24 }}>

        <div className="card-panel appt-panel">

          <div className="card-panel-header">Veículos no pátio — {sessions.length} ativo(s)</div>

          <div className="card-panel-body">

            <div className="prefs-stack">

              <label className="print-option">

                <input type="checkbox" checked={autoPrintPayment} onChange={(e) => setAutoPrintPayment(e.target.checked)} />

                Imprimir comprovante de pagamento automaticamente

              </label>

              <label className="print-option">

                <input type="checkbox" checked={autoPrintExit} onChange={(e) => setAutoPrintExit(e.target.checked)} />

                Imprimir comprovante de saída (saída manual)

              </label>

            </div>

          </div>

          <div className="card-panel-body" style={{ padding: 0 }}>

            <table className="data-table">

              <thead>

                <tr><th>Placa</th><th>Zona</th><th>Entrada</th><th>Pagamento</th><th>Ações</th></tr>

              </thead>

              <tbody>

                {sessions.map((s) => (

                  <tr key={s.id}>

                    <td><strong>{s.vehiclePlate}</strong></td>

                    <td>{s.zoneName}</td>

                    <td>{formatBrDateTime(s.enteredAt)}</td>

                    <td>

                      {s.isPaid ? (

                        <span className="badge" style={{ background: '#dcfce7', color: '#166534' }}>Pago</span>

                      ) : (

                        <span className="badge" style={{ background: '#fef9c3', color: '#854d0e' }}>

                          Pendente

                          {s.estimatedAmount != null && ` — ${s.estimatedAmount.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}`}

                        </span>

                      )}

                    </td>

                    <td>

                      <div className="table-actions">

                        <button

                          className="btn btn-secondary btn-sm"

                          type="button"

                          onClick={() => printParkingEntryTicket(s, zoneRate(s.zoneId))}

                        >

                          Ticket

                        </button>

                        {!s.isPaid && (

                          <button className="btn btn-sm" type="button" onClick={() => handlePay(s)}>

                            Pagar

                          </button>

                        )}

                        {s.isPaid && (

                          <button className="btn btn-sm" type="button" onClick={() => handleCheckOut(s)}>

                            Saída manual

                          </button>

                        )}

                      </div>

                    </td>

                  </tr>

                ))}

                {sessions.length === 0 && (

                  <tr><td colSpan={5} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum veículo ativo</td></tr>

                )}

              </tbody>

            </table>

          </div>

        </div>



        <div className="card parking-gate-panel">

          <div className="form-section-title">Cancela de saída — leitor QR Code</div>

          <p style={{ color: 'var(--muted)', fontSize: '0.9rem', marginTop: 0 }}>

            Simule a leitura do QR Code do ticket. A cancela só abre se o estacionamento já estiver pago.

          </p>

          <form onSubmit={handleGateScan} className="form-grid">

            <div className="form-field full">

              <label htmlFor="gateQr">QR Code / ticket</label>

              <input

                id="gateQr"

                placeholder="Cole o conteúdo do QR ou HMS-PARK:..."

                value={qrInput}

                onChange={(e) => setQrInput(e.target.value)}

              />

            </div>

            <div className="form-field full">

              <button type="submit" className="btn" disabled={gateLoading || !qrInput.trim()}>

                {gateLoading ? 'Validando...' : 'Validar na cancela'}

              </button>

            </div>

          </form>

          {gateResult && (

            <div className={`alert ${gateResult.allowed ? 'alert-success' : 'alert-error'}`} style={{ marginTop: 16 }}>

              <strong>{gateResult.allowed ? 'Cancela liberada' : 'Saída bloqueada'}</strong>

              <p style={{ margin: '6px 0 0' }}>{gateResult.message}</p>

              {gateResult.session && (

                <p style={{ margin: '6px 0 0', fontSize: '0.85rem' }}>

                  Placa: {gateResult.session.vehiclePlate} · Ticket: {gateResult.session.id.slice(0, 8).toUpperCase()}

                </p>

              )}

            </div>

          )}

        </div>

      </div>



      {recentCompleted.length > 0 && (

        <div className="card-panel appt-panel" style={{ marginTop: 24 }}>

          <div className="card-panel-header">Saídas recentes — {recentCompleted.length} registro(s)</div>

          <div className="card-panel-body" style={{ padding: 0 }}>

            <table className="data-table">

              <thead>

                <tr><th>Placa</th><th>Zona</th><th>Entrada</th><th>Saída</th><th>Valor</th><th>Ações</th></tr>

              </thead>

              <tbody>

                {recentCompleted.map((s) => (

                  <tr key={s.id}>

                    <td>{s.vehiclePlate}</td>

                    <td>{s.zoneName}</td>

                    <td>{formatBrDateTime(s.enteredAt)}</td>

                    <td>{s.exitedAt ? formatBrDateTime(s.exitedAt) : '—'}</td>

                    <td>{s.amountCharged != null ? s.amountCharged.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' }) : '—'}</td>

                    <td>

                      <button className="btn btn-secondary btn-sm" type="button" onClick={() => printParkingExitReceipt(s, zoneRate(s.zoneId))}>

                        Reimprimir

                      </button>

                    </td>

                  </tr>

                ))}

              </tbody>

            </table>

          </div>

        </div>

      )}



      <Modal open={showModal} onClose={() => setShowModal(false)} title="Entrada — ticket com QR Code" width="md">

        <form onSubmit={handleCheckIn} className="form-grid">

          <div className="form-field">

            <label htmlFor="parkZone">Zona *</label>

            <select id="parkZone" value={form.zoneId} onChange={(e) => setForm({ ...form, zoneId: e.target.value })} required>

              <option value="">Selecione...</option>

              {zones.map((z) => (

                <option key={z.id} value={z.id}>

                  {z.name} — {z.hourlyRate.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}/h

                </option>

              ))}

            </select>

          </div>

          <div className="form-field">

            <label htmlFor="parkPlate">Placa *</label>

            <input

              id="parkPlate"

              placeholder="Ex: ABC1D23"

              value={form.vehiclePlate}

              onChange={(e) => setForm({ ...form, vehiclePlate: e.target.value.toUpperCase() })}

              required

            />

          </div>

          <div className="form-field">

            <label htmlFor="parkPatient">Paciente vinculado</label>

            <select id="parkPatient" value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>

              <option value="">Opcional</option>

              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}

            </select>

          </div>

          <div className="form-field full">

            <label className="print-option">

              <input type="checkbox" checked={autoPrintEntry} onChange={(e) => setAutoPrintEntry(e.target.checked)} />

              Imprimir ticket de entrada com QR Code automaticamente

            </label>

          </div>

          <div className="form-field full modal-actions">

            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>

            <button className="btn" type="submit">Registrar entrada</button>

          </div>

        </form>

      </Modal>

    </>

  );

}



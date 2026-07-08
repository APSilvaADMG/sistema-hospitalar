import { type FormEvent, useEffect, useMemo, useState } from 'react';
import {
  api,
  hospitalityBookingStatusLabels,
  hospitalityRoomStatusLabels,
  type HospitalityBookingDto,
  type HospitalityRoomDto,
  type PatientDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { formatBrDate } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

export function HospitalityPage() {
  const { hasPermission } = useAuth();
  const [rooms, setRooms] = useState<HospitalityRoomDto[]>([]);
  const [bookings, setBookings] = useState<HospitalityBookingDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [form, setForm] = useState({
    roomId: '',
    patientId: '',
    guestName: '',
    guestDocument: '',
    checkInDate: new Date().toISOString().slice(0, 10),
    checkOutDate: '',
    notes: '',
  });
  const [showModal, setShowModal] = useState(false);
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    load();
    api.getPatients('', 1).then((r) => setPatients(r.items)).catch(console.error);
  }, []);

  async function load() {
    const [r, b] = await Promise.all([api.getHospitalityRooms(), api.getHospitalityBookings()]);
    setRooms(r);
    setBookings(b);
  }

  const stats = useMemo(() => ({
    total: rooms.length,
    available: rooms.filter((r) => r.status === 'Available').length,
    occupied: rooms.filter((r) => r.status === 'Occupied').length,
    activeBookings: bookings.filter((b) => b.status === 'Reserved' || b.status === 'CheckedIn').length,
  }), [rooms, bookings]);

  const filtered = useMemo(() => {
    return bookings
      .filter((b) => !statusFilter || b.status === statusFilter)
      .filter((b) => {
        if (!search.trim()) return true;
        const term = search.toLowerCase();
        return (
          b.guestName.toLowerCase().includes(term)
          || b.roomNumber.toLowerCase().includes(term)
          || (b.patientName?.toLowerCase().includes(term) ?? false)
        );
      });
  }, [bookings, statusFilter, search]);

  const availableRooms = rooms.filter((r) => r.status === 'Available');

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    await api.createHospitalityBooking({
      roomId: form.roomId,
      patientId: form.patientId || undefined,
      guestName: form.guestName,
      guestDocument: form.guestDocument || undefined,
      checkInDate: form.checkInDate,
      checkOutDate: form.checkOutDate || undefined,
      notes: form.notes || undefined,
    });
    setSuccess('Reserva criada com sucesso.');
    setShowModal(false);
    setForm({ roomId: '', patientId: '', guestName: '', guestDocument: '', checkInDate: form.checkInDate, checkOutDate: '', notes: '' });
    await load();
  }

  async function checkIn(id: string) {
    await api.checkInHospitality(id);
    setSuccess('Check-in realizado.');
    await load();
  }

  async function checkOut(id: string) {
    await api.checkOutHospitality(id);
    setSuccess('Check-out realizado.');
    await load();
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito à recepção.</div>;
  }

  return (
    <>
      <PageHeader
        eyebrow="Internação"
        title="Hotelaria"
        subtitle="Acomodação de acompanhantes e hóspedes vinculados a pacientes internados."
      >
        <button className="btn" type="button" onClick={() => setShowModal(true)}>
          + Nova reserva
        </button>
      </PageHeader>

      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Quartos disponíveis" value={stats.available} variant="success" />
        <KpiCard label="Quartos ocupados" value={stats.occupied} variant="warning" />
        <KpiCard label="Reservas ativas" value={stats.activeBookings} variant="primary" />
        <KpiCard label="Total de quartos" value={stats.total} variant="info" />
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Quartos — {rooms.length} unidade(s)</div>
        <div className="card-panel-body">
          <div className="bed-grid">
            {rooms.map((r) => (
              <div key={r.id} className={`bed-card bed-status-${r.status === 'Available' ? 1 : r.status === 'Occupied' ? 2 : 3}`}>
                <strong>Quarto {r.roomNumber}</strong>
                <span>Andar {r.floor ?? '—'} · {r.capacity} pessoa(s)</span>
                <span>{r.dailyRate.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}/dia</span>
                <span className="badge">{hospitalityRoomStatusLabels[r.status] ?? r.status}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 24 }}>
        <div className="card-panel-header">Reservas — {filtered.length} registro(s)</div>
        <FilterBar>
          <div className="filter-field w-md">
            <label htmlFor="bookingStatus">Status</label>
            <select id="bookingStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
              <option value="">Todos</option>
              {Object.entries(hospitalityBookingStatusLabels).map(([k, v]) => (
                <option key={k} value={k}>{v}</option>
              ))}
            </select>
          </div>
          <div className="filter-field grow">
            <label htmlFor="bookingSearch">Buscar</label>
            <input
              id="bookingSearch"
              placeholder="Hóspede, quarto ou paciente..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Quarto</th><th>Hóspede</th><th>Paciente</th><th>Check-in</th><th>Status</th><th></th></tr>
            </thead>
            <tbody>
              {filtered.map((b) => (
                <tr key={b.id}>
                  <td><strong>{b.roomNumber}</strong></td>
                  <td>{b.guestName}</td>
                  <td>{b.patientName ?? '—'}</td>
                  <td>{formatBrDate(b.checkInDate)}</td>
                  <td><span className="badge">{hospitalityBookingStatusLabels[b.status] ?? b.status}</span></td>
                  <td>
                    <div className="table-actions">
                      {b.status === 'Reserved' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => checkIn(b.id)}>Check-in</button>
                      )}
                      {b.status === 'CheckedIn' && (
                        <button className="btn btn-secondary btn-sm" type="button" onClick={() => checkOut(b.id)}>Check-out</button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhuma reserva encontrada.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={showModal}
        onClose={() => setShowModal(false)}
        title="Nova reserva"
        subtitle="Reserve um quarto para acompanhante ou hóspede."
        width="lg"
      >
        <form className="form-grid" onSubmit={handleCreate}>
          <div className="form-field">
            <label htmlFor="roomId">Quarto *</label>
            <select id="roomId" value={form.roomId} onChange={(e) => setForm({ ...form, roomId: e.target.value })} required>
              <option value="">Selecione...</option>
              {availableRooms.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.roomNumber} — {r.dailyRate.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}/dia
                </option>
              ))}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="guestName">Nome do hóspede *</label>
            <input id="guestName" required value={form.guestName} onChange={(e) => setForm({ ...form, guestName: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="guestDocument">Documento</label>
            <input id="guestDocument" value={form.guestDocument} onChange={(e) => setForm({ ...form, guestDocument: e.target.value })} />
          </div>
          <div className="form-field full">
            <label htmlFor="patientId">Paciente vinculado</label>
            <select id="patientId" value={form.patientId} onChange={(e) => setForm({ ...form, patientId: e.target.value })}>
              <option value="">Opcional</option>
              {patients.map((p) => <option key={p.id} value={p.id}>{p.fullName}</option>)}
            </select>
          </div>
          <div className="form-field">
            <label htmlFor="checkInDate">Data de entrada</label>
            <input id="checkInDate" type="date" value={form.checkInDate} onChange={(e) => setForm({ ...form, checkInDate: e.target.value })} />
          </div>
          <div className="form-field">
            <label htmlFor="checkOutDate">Data de saída prevista</label>
            <input id="checkOutDate" type="date" value={form.checkOutDate} onChange={(e) => setForm({ ...form, checkOutDate: e.target.value })} />
          </div>
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Reservar</button>
          </div>
        </form>
      </Modal>
    </>
  );
}

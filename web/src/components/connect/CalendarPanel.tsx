import { type FormEvent, useCallback, useEffect, useMemo, useState } from 'react';

import {

  api,

  connectCalendarEventTypeLabels,

  connectCalendarParticipantResponseLabels,

  connectCalendarRecurrenceLabels,

  type ConnectCalendarEventDetailDto,

  type ConnectCalendarEventListItemDto,

  type ConnectCalendarEventType,

  type ConnectCalendarParticipantResponse,

  type ConnectCalendarRecurrenceRule,

  type DepartmentDto,

  type UserListDto,

} from '../../api/client';

import { Modal } from '../Modal';

import { formatBrDate, formatBrDateTime } from '../../utils/dateUtils';

import { subscribeConnectCalendarRefresh } from '../../offline/connectRealtimeSync';



type ViewMode = 'month' | 'week';



const HOURS = Array.from({ length: 24 }, (_, i) => i);

const DEFAULT_COLORS = ['#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#8b5cf6', '#ec4899'];



function startOfMonth(d: Date) {

  return new Date(d.getFullYear(), d.getMonth(), 1);

}



function endOfMonth(d: Date) {

  return new Date(d.getFullYear(), d.getMonth() + 1, 0, 23, 59, 59);

}



function startOfWeek(d: Date) {

  const copy = new Date(d);

  const day = copy.getDay();

  copy.setDate(copy.getDate() - day);

  copy.setHours(0, 0, 0, 0);

  return copy;

}



function endOfWeek(d: Date) {

  const copy = startOfWeek(d);

  copy.setDate(copy.getDate() + 6);

  copy.setHours(23, 59, 59, 999);

  return copy;

}



function weekDays(cursor: Date) {

  const start = startOfWeek(cursor);

  return Array.from({ length: 7 }, (_, i) => {

    const d = new Date(start);

    d.setDate(d.getDate() + i);

    return d;

  });

}



function emptyForm() {

  return {

    titulo: '',

    descricao: '',

    inicio: '',

    fim: '',

    local: '',

    tipo: 'Reuniao' as ConnectCalendarEventType,

    allDay: false,

    recurrenceRule: 'None' as ConnectCalendarRecurrenceRule,

    color: DEFAULT_COLORS[0],

    reminderMinutes: '',

    setorId: '',

    participantIds: [] as string[],

  };

}



function eventKey(ev: ConnectCalendarEventListItemDto) {
  return `${ev.id}-${ev.inicio}`;
}

function eventLabel(ev: ConnectCalendarEventListItemDto) {
  const suffix = ev.isRecurrenceInstance || (ev.recurrenceRule && ev.recurrenceRule !== 'None') ? ' ↻' : '';
  return `${ev.titulo}${suffix}`;
}

export function CalendarPanel({ canWrite }: { canWrite: boolean }) {

  const [cursor, setCursor] = useState(() => new Date());

  const [view, setView] = useState<ViewMode>('month');

  const [scope, setScope] = useState<'all' | 'mine' | 'team'>('mine');

  const [events, setEvents] = useState<ConnectCalendarEventListItemDto[]>([]);

  const [users, setUsers] = useState<UserListDto[]>([]);

  const [departments, setDepartments] = useState<DepartmentDto[]>([]);

  const [error, setError] = useState('');

  const [showModal, setShowModal] = useState(false);

  const [editingId, setEditingId] = useState<string | null>(null);

  const [selectedId, setSelectedId] = useState<string | null>(null);

  const [detail, setDetail] = useState<ConnectCalendarEventDetailDto | null>(null);

  const [form, setForm] = useState(emptyForm);



  const range = useMemo(() => {

    if (view === 'week') {

      return { from: startOfWeek(cursor), to: endOfWeek(cursor) };

    }

    return { from: startOfMonth(cursor), to: endOfMonth(cursor) };

  }, [cursor, view]);



  const load = useCallback(async () => {

    setError('');

    try {

      const list = await api.getConnectCalendarEvents({

        from: range.from.toISOString(),

        to: range.to.toISOString(),

        scope,

      });

      setEvents(list);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar agenda');

    }

  }, [range.from, range.to, scope]);



  const loadDetail = useCallback(async (id: string) => {

    try {

      setDetail(await api.getConnectCalendarEvent(id));

      setSelectedId(id);

    } catch (err) {

      setError(err instanceof Error ? err.message : 'Erro ao carregar evento');

    }

  }, []);



  useEffect(() => {

    load().catch(console.error);

  }, [load]);



  useEffect(() => {

    return subscribeConnectCalendarRefresh(() => {

      load().catch(console.error);

      if (selectedId) loadDetail(selectedId).catch(console.error);

    });

  }, [load, loadDetail, selectedId]);



  useEffect(() => {

    api.getUsers().then(setUsers).catch(console.error);

    api.getDepartments().then(setDepartments).catch(console.error);

  }, []);



  const grouped = useMemo(() => {

    const map = new Map<string, ConnectCalendarEventListItemDto[]>();

    for (const ev of events) {

      const key = formatBrDate(ev.inicio);

      if (!map.has(key)) map.set(key, []);

      map.get(key)!.push(ev);

    }

    return Array.from(map.entries()).sort((a, b) => a[0].localeCompare(b[0]));

  }, [events]);



  const days = useMemo(() => weekDays(cursor), [cursor]);



  const periodLabel = useMemo(() => {

    if (view === 'week') {

      return `${formatBrDate(range.from)} — ${formatBrDate(range.to)}`;

    }

    return cursor.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' });

  }, [cursor, view, range.from, range.to]);



  function shiftPeriod(delta: number) {

    const next = new Date(cursor);

    if (view === 'week') next.setDate(next.getDate() + delta * 7);

    else next.setMonth(next.getMonth() + delta);

    setCursor(next);

  }



  function openCreate() {

    setEditingId(null);

    setForm(emptyForm());

    setShowModal(true);

  }



  function openEdit() {

    if (!detail) return;

    setEditingId(detail.id);

    setForm({

      titulo: detail.titulo,

      descricao: detail.descricao ?? '',

      inicio: toInputValue(detail.inicio, detail.allDay),

      fim: toInputValue(detail.fim, detail.allDay),

      local: detail.local ?? '',

      tipo: detail.tipo,

      allDay: detail.allDay,

      recurrenceRule: detail.recurrenceRule ?? 'None',

      color: detail.color ?? DEFAULT_COLORS[0],

      reminderMinutes: detail.reminderMinutes != null ? String(detail.reminderMinutes) : '',

      setorId: detail.setorId ?? '',

      participantIds: detail.participants.map((p) => p.userId),

    });

    setShowModal(true);

  }



  async function handleSave(e: FormEvent) {

    e.preventDefault();

    if (!form.titulo.trim() || !form.inicio || !form.fim) return;



    const payload = {

      titulo: form.titulo,

      descricao: form.descricao || undefined,

      inicio: new Date(form.inicio).toISOString(),

      fim: new Date(form.fim).toISOString(),

      local: form.local || undefined,

      tipo: form.tipo,

      allDay: form.allDay,

      recurrenceRule: form.recurrenceRule,

      color: form.color || undefined,

      reminderMinutes: form.reminderMinutes ? Number(form.reminderMinutes) : undefined,

      setorId: form.setorId || undefined,

      participantUserIds: form.participantIds,

    };



    if (editingId) {

      await api.updateConnectCalendarEvent(editingId, payload);

      await loadDetail(editingId);

    } else {

      const created = await api.createConnectCalendarEvent(payload);

      await loadDetail(created.id);

    }



    setShowModal(false);

    setForm(emptyForm());

    setEditingId(null);

    await load();

  }



  async function handleDelete() {

    if (!detail || !window.confirm('Excluir este evento?')) return;

    await api.deleteConnectCalendarEvent(detail.id);

    setDetail(null);

    setSelectedId(null);

    await load();

  }



  async function handleRespond(response: ConnectCalendarParticipantResponse) {

    if (!detail) return;

    const updated = await api.respondConnectCalendarEvent(detail.id, response);

    setDetail(updated);

    await load();

  }



  function eventsForDay(day: Date) {

    const dayStart = new Date(day);

    dayStart.setHours(0, 0, 0, 0);

    const dayEnd = new Date(day);

    dayEnd.setHours(23, 59, 59, 999);

    return events.filter((ev) => {

      const start = new Date(ev.inicio);

      const end = new Date(ev.fim);

      return start <= dayEnd && end >= dayStart;

    });

  }



  return (

    <div className="connect-panel" style={{ display: 'grid', gridTemplateColumns: detail ? '1fr 320px' : '1fr', gap: '1rem' }}>

      <div>

        <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', alignItems: 'center', marginBottom: '1rem' }}>

          <button type="button" className="btn btn-secondary btn-sm" onClick={() => shiftPeriod(-1)}>◀</button>

          <strong style={{ minWidth: 180, textAlign: 'center', textTransform: 'capitalize' }}>{periodLabel}</strong>

          <button type="button" className="btn btn-secondary btn-sm" onClick={() => shiftPeriod(1)}>▶</button>

          <button type="button" className="btn btn-secondary btn-sm" onClick={() => setCursor(new Date())}>Hoje</button>

          <select value={view} onChange={(e) => setView(e.target.value as ViewMode)}>

            <option value="month">Mês (lista)</option>

            <option value="week">Semana (grade)</option>

          </select>

          <select value={scope} onChange={(e) => setScope(e.target.value as typeof scope)}>

            <option value="mine">Meus eventos</option>

            <option value="team">Equipe / setor</option>

            <option value="all">Todos</option>

          </select>

          {canWrite ? (

            <button type="button" className="btn btn-primary btn-sm" onClick={openCreate}>Novo evento</button>

          ) : null}

          <button type="button" className="btn btn-secondary btn-sm" onClick={() => load().catch(console.error)}>Atualizar</button>

        </div>



        {error ? <p className="text-danger">{error}</p> : null}



        {view === 'week' ? (

          <div style={{ overflowX: 'auto' }}>

            <table className="data-table" style={{ width: '100%', minWidth: 700, tableLayout: 'fixed' }}>

              <thead>

                <tr>

                  <th style={{ width: 48 }}>Hora</th>

                  {days.map((d) => (

                    <th key={d.toISOString()} style={{ textAlign: 'center' }}>

                      {d.toLocaleDateString('pt-BR', { weekday: 'short', day: '2-digit', month: '2-digit' })}

                    </th>

                  ))}

                </tr>

              </thead>

              <tbody>

                {HOURS.map((hour) => (

                  <tr key={hour}>

                    <td className="text-muted" style={{ fontSize: '0.75rem', verticalAlign: 'top' }}>

                      {String(hour).padStart(2, '0')}:00

                    </td>

                    {days.map((day) => {

                      const dayEvents = eventsForDay(day).filter((ev) => {

                        if (ev.allDay) return hour === 0;

                        const h = new Date(ev.inicio).getHours();

                        return h === hour;

                      });

                      return (

                        <td

                          key={`${day.toISOString()}-${hour}`}

                          style={{ verticalAlign: 'top', padding: 2, border: '1px solid var(--border, #e5e7eb)', minHeight: 28 }}

                        >

                          {dayEvents.map((ev) => (

                            <button

                              key={eventKey(ev)}

                              type="button"

                              onClick={() => loadDetail(ev.id).catch(console.error)}

                              style={{

                                display: 'block',

                                width: '100%',

                                textAlign: 'left',

                                fontSize: '0.7rem',

                                padding: '2px 4px',

                                marginBottom: 2,

                                border: 'none',

                                borderRadius: 4,

                                cursor: 'pointer',

                                background: ev.color ?? '#3b82f6',

                                color: '#fff',

                              }}

                            >

                              {eventLabel(ev)}

                            </button>

                          ))}

                        </td>

                      );

                    })}

                  </tr>

                ))}

              </tbody>

            </table>

          </div>

        ) : events.length === 0 ? (

          <p className="text-muted">Nenhum evento no período selecionado.</p>

        ) : (

          <table className="data-table" style={{ width: '100%' }}>

            <thead>

              <tr>

                <th>Data</th>

                <th>Horário</th>

                <th>Título</th>

                <th>Tipo</th>

                <th>Local</th>

                <th>Organizador</th>

              </tr>

            </thead>

            <tbody>

              {grouped.flatMap(([date, dayEvents]) =>

                dayEvents.map((ev, idx) => (

                  <tr

                    key={eventKey(ev)}

                    onClick={() => loadDetail(ev.id).catch(console.error)}

                    style={{ cursor: 'pointer', background: selectedId === ev.id ? 'var(--surface-elevated, #f0f4f8)' : undefined }}

                  >

                    {idx === 0 ? (

                      <td rowSpan={dayEvents.length} style={{ verticalAlign: 'top', fontWeight: 600 }}>{date}</td>

                    ) : null}

                    <td>

                      {ev.allDay

                        ? 'Dia inteiro'

                        : `${formatBrDateTime(ev.inicio).split(' ')[1]} – ${formatBrDateTime(ev.fim).split(' ')[1]}`}

                    </td>

                    <td>

                      <span style={{ display: 'inline-block', width: 8, height: 8, borderRadius: '50%', background: ev.color ?? '#3b82f6', marginRight: 6 }} />

                      {eventLabel(ev)}

                    </td>

                    <td>{connectCalendarEventTypeLabels[ev.tipo]}</td>

                    <td>{ev.local ?? '—'}</td>

                    <td>{ev.organizadorName}</td>

                  </tr>

                )),

              )}

            </tbody>

          </table>

        )}

      </div>



      {detail ? (

        <aside className="connect-panel" style={{ alignSelf: 'start', position: 'sticky', top: 8 }}>

          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.5rem' }}>

            <h3 style={{ marginTop: 0, marginBottom: '0.25rem' }}>{detail.titulo}</h3>

            <button type="button" className="btn btn-secondary btn-sm" onClick={() => { setDetail(null); setSelectedId(null); }}>✕</button>

          </div>

          <p className="text-muted" style={{ fontSize: '0.85rem' }}>

            {detail.allDay

              ? formatBrDate(detail.inicio)

              : `${formatBrDateTime(detail.inicio)} – ${formatBrDateTime(detail.fim)}`}

          </p>

          {detail.local ? <p><strong>Local:</strong> {detail.local}</p> : null}

          {detail.descricao ? <p style={{ whiteSpace: 'pre-wrap' }}>{detail.descricao}</p> : null}

          <p className="text-muted" style={{ fontSize: '0.85rem' }}>

            {connectCalendarEventTypeLabels[detail.tipo]} · {detail.organizadorName}

            {detail.recurrenceRule && detail.recurrenceRule !== 'None'

              ? ` · ${connectCalendarRecurrenceLabels[detail.recurrenceRule]}`

              : ''}

            {detail.reminderMinutes ? ` · Lembrete ${detail.reminderMinutes} min antes` : ''}

          </p>



          {detail.participants.length > 0 ? (

            <div style={{ marginTop: '0.75rem' }}>

              <strong>Participantes</strong>

              <ul style={{ paddingLeft: '1.2rem', margin: '0.25rem 0' }}>

                {detail.participants.map((p) => (

                  <li key={p.userId}>

                    {p.userName}

                    {p.response ? ` — ${connectCalendarParticipantResponseLabels[p.response]}` : ''}

                  </li>

                ))}

              </ul>

            </div>

          ) : null}



          {!detail.isOrganizer && detail.myResponse !== 'Aceito' ? (

            <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.35rem', marginTop: '0.75rem' }}>

              <span className="text-muted" style={{ width: '100%', fontSize: '0.85rem' }}>RSVP:</span>

              {(['Aceito', 'Recusado', 'Talvez'] as ConnectCalendarParticipantResponse[]).map((r) => (

                <button key={r} type="button" className="btn btn-secondary btn-sm" onClick={() => handleRespond(r).catch(console.error)}>

                  {connectCalendarParticipantResponseLabels[r]}

                </button>

              ))}

            </div>

          ) : null}



          {canWrite && detail.isOrganizer ? (

            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '1rem' }}>

              <button type="button" className="btn btn-secondary btn-sm" onClick={openEdit}>Editar</button>

              <button type="button" className="btn btn-secondary btn-sm" onClick={() => handleDelete().catch(console.error)}>Excluir</button>

            </div>

          ) : null}

        </aside>

      ) : null}



      <Modal open={showModal} title={editingId ? 'Editar evento' : 'Novo evento'} onClose={() => setShowModal(false)}>

        <form onSubmit={handleSave} style={{ display: 'grid', gap: '0.75rem' }}>

          <label>

            Título

            <input value={form.titulo} onChange={(e) => setForm({ ...form, titulo: e.target.value })} required />

          </label>

          <label>

            Descrição

            <textarea rows={3} value={form.descricao} onChange={(e) => setForm({ ...form, descricao: e.target.value })} />

          </label>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.5rem' }}>

            <label>

              Início

              <input type={form.allDay ? 'date' : 'datetime-local'} value={form.inicio} onChange={(e) => setForm({ ...form, inicio: e.target.value })} required />

            </label>

            <label>

              Fim

              <input type={form.allDay ? 'date' : 'datetime-local'} value={form.fim} onChange={(e) => setForm({ ...form, fim: e.target.value })} required />

            </label>

          </div>

          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>

            <input type="checkbox" checked={form.allDay} onChange={(e) => setForm({ ...form, allDay: e.target.checked })} />

            Dia inteiro

          </label>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr 1fr', gap: '0.5rem' }}>

            <label>

              Recorrência

              <select value={form.recurrenceRule} onChange={(e) => setForm({ ...form, recurrenceRule: e.target.value as ConnectCalendarRecurrenceRule })}>

                {Object.entries(connectCalendarRecurrenceLabels).map(([k, label]) => (

                  <option key={k} value={k}>{label}</option>

                ))}

              </select>

            </label>

            <label>

              Cor

              <input type="color" value={form.color} onChange={(e) => setForm({ ...form, color: e.target.value })} />

            </label>

            <label>

              Lembrete (min)

              <input type="number" min={0} step={5} placeholder="—" value={form.reminderMinutes} onChange={(e) => setForm({ ...form, reminderMinutes: e.target.value })} />

            </label>

          </div>

          <label>

            Local

            <input value={form.local} onChange={(e) => setForm({ ...form, local: e.target.value })} />

          </label>

          <label>

            Tipo

            <select value={form.tipo} onChange={(e) => setForm({ ...form, tipo: e.target.value as ConnectCalendarEventType })}>

              {Object.entries(connectCalendarEventTypeLabels).map(([k, label]) => (

                <option key={k} value={k}>{label}</option>

              ))}

            </select>

          </label>

          <label>

            Setor (opcional)

            <select value={form.setorId} onChange={(e) => setForm({ ...form, setorId: e.target.value })}>

              <option value="">—</option>

              {departments.map((d) => (

                <option key={d.id} value={d.id}>{d.name}</option>

              ))}

            </select>

          </label>

          <label>

            Participantes

            <select multiple size={4} value={form.participantIds} onChange={(e) => setForm({ ...form, participantIds: Array.from(e.target.selectedOptions, (o) => o.value) })}>

              {users.filter((u) => u.isActive).map((u) => (

                <option key={u.id} value={u.id}>{u.fullName}</option>

              ))}

            </select>

          </label>

          <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>

            <button type="button" className="btn btn-secondary" onClick={() => setShowModal(false)}>Cancelar</button>

            <button type="submit" className="btn btn-primary">Salvar</button>

          </div>

        </form>

      </Modal>

    </div>

  );

}



function toInputValue(iso: string, allDay: boolean) {

  const d = new Date(iso);

  if (allDay) {

    return d.toISOString().slice(0, 10);

  }

  const pad = (n: number) => String(n).padStart(2, '0');

  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;

}



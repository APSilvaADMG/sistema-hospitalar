import { type FormEvent, type ReactNode, useCallback, useEffect, useState } from 'react';
import {
  api,
  type CallKioskTicketRequest,
  type CallTvQueueRequest,
  type CreateTvAnnouncementRequest,
  type CreateTvCampaignRequest,
  type CreateTvDisplayRequest,
  type CreateTvNewsRequest,
  type KioskTicketDto,
  type TvAnnouncementDto,
  type TvCampaignDto,
  type TvDisplayDto,
  type TvLayoutDto,
  type TvMediaDto,
  type TvMonitorSummaryDto,
  type TvNewsDto,
  type TvQueueCallDto,
} from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { KpiCard } from '../../components/KpiCard';
import { PageHeader } from '../../components/PageHeader';
import { TvLayoutEditor } from './TvLayoutEditor';
import { isTvVideoMedia, resolveTvMediaUrl } from './tvMediaUtils';

type Tab = 'monitor' | 'displays' | 'layouts' | 'campaigns' | 'media' | 'news' | 'announcements' | 'calls';

const TAB_CONFIG: { id: Tab; label: string; icon: string; hint: string }[] = [
  { id: 'monitor', label: 'Monitoramento', icon: '📊', hint: 'Status em tempo real das telas' },
  { id: 'displays', label: 'TVs', icon: '📺', hint: 'Cadastro e links do player' },
  { id: 'layouts', label: 'Layouts', icon: '🧩', hint: 'Zonas e widgets por tela' },
  { id: 'campaigns', label: 'Campanhas', icon: '📅', hint: 'Conteúdo por horário e dia' },
  { id: 'media', label: 'Mídias', icon: '🎬', hint: 'Imagens e vídeos institucionais' },
  { id: 'news', label: 'Notícias', icon: '📰', hint: 'Faixa de notícias no rodapé' },
  { id: 'announcements', label: 'Avisos', icon: '📢', hint: 'Alertas e comunicados' },
  { id: 'calls', label: 'Chamadas', icon: '🔔', hint: 'Fila, totem e histórico' },
];

function TvPanel({ title, subtitle, children, actions }: {
  title: string;
  subtitle?: string;
  children: ReactNode;
  actions?: ReactNode;
}) {
  return (
    <div className="card-panel tv-admin-panel">
      <div className="tv-admin-panel-head">
        <div>
          <div className="card-panel-header">{title}</div>
          {subtitle ? <p className="tv-admin-panel-sub">{subtitle}</p> : null}
        </div>
        {actions ? <div className="tv-admin-panel-actions">{actions}</div> : null}
      </div>
      <div className="card-panel-body tv-admin-panel-body">{children}</div>
    </div>
  );
}

function TvEmpty({ icon, title, text }: { icon: string; title: string; text: string }) {
  return (
    <div className="tv-admin-empty">
      <span className="tv-admin-empty-icon" aria-hidden>{icon}</span>
      <strong>{title}</strong>
      <p>{text}</p>
    </div>
  );
}

function formatWeekDays(days?: string) {
  if (!days) return 'Todos os dias';
  const names = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];
  return days.split(',').map((d) => names[Number(d.trim())] ?? d).join(', ');
}

export function TvSignageAdminPage() {
  const { hasPermission } = useAuth();
  const canAdmin = hasPermission('connect.admin');
  const canWrite = hasPermission('connect.write');
  const [tab, setTab] = useState<Tab>('monitor');
  const [monitor, setMonitor] = useState<TvMonitorSummaryDto | null>(null);
  const [displays, setDisplays] = useState<TvDisplayDto[]>([]);
  const [layouts, setLayouts] = useState<TvLayoutDto[]>([]);
  const [campaigns, setCampaigns] = useState<TvCampaignDto[]>([]);
  const [media, setMedia] = useState<TvMediaDto[]>([]);
  const [news, setNews] = useState<TvNewsDto[]>([]);
  const [announcements, setAnnouncements] = useState<TvAnnouncementDto[]>([]);
  const [calls, setCalls] = useState<TvQueueCallDto[]>([]);
  const [kioskTickets, setKioskTickets] = useState<KioskTicketDto[]>([]);
  const [selectedLayoutId, setSelectedLayoutId] = useState<string>('');
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  const [displayForm, setDisplayForm] = useState<CreateTvDisplayRequest>({
    name: '', slug: '', sector: '', layoutId: undefined, orientation: 1,
    showPatientName: false, enableSound: true, callDisplaySeconds: 30, weatherCity: 'Arapiraca - AL',
  });
  const [callForm, setCallForm] = useState<CallTvQueueRequest>({
    ticketNumber: '', patientName: '', destination: '', sector: '', displayId: undefined, showPatientName: false,
  });
  const [kioskCallForm, setKioskCallForm] = useState<CallKioskTicketRequest>({
    destination: 'Consultório', displayId: undefined, showPatientName: false,
  });
  const [campaignForm, setCampaignForm] = useState<CreateTvCampaignRequest>({
    name: '', sector: '', priority: 1, mediaIds: [], dailyStart: '08:00', dailyEnd: '18:00', daysOfWeek: '1,2,3,4,5',
  });
  const [newsForm, setNewsForm] = useState<CreateTvNewsRequest>({ title: '', summary: '', sector: '' });
  const [announcementForm, setAnnouncementForm] = useState<CreateTvAnnouncementRequest>({
    title: '', body: '', sector: '', priority: 1,
  });
  const [uploadTitle, setUploadTitle] = useState('');
  const [uploadFile, setUploadFile] = useState<File | null>(null);

  const refresh = useCallback(async () => {
    try {
      const [m, d, l, c, med, n, a, callsList, tickets] = await Promise.all([
        api.getTvSignageMonitor(),
        api.getTvDisplays(),
        api.getTvLayouts(),
        api.getTvCampaigns(),
        api.getTvMedia(),
        api.getTvNews(),
        api.getTvAnnouncements(),
        api.getTvCalls(30),
        api.getKioskTickets(true),
      ]);
      setMonitor(m);
      setDisplays(d);
      setLayouts(l);
      setCampaigns(c);
      setMedia(med);
      setNews(n);
      setAnnouncements(a);
      setCalls(callsList);
      setKioskTickets(tickets);
      if (!selectedLayoutId && l.length > 0) setSelectedLayoutId(l[0].id);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar TV Corporativa');
    }
  }, [selectedLayoutId]);

  useEffect(() => {
    refresh().catch(console.error);
  }, [refresh]);

  useEffect(() => {
    if (!message) return;
    const t = window.setTimeout(() => setMessage(null), 4000);
    return () => window.clearTimeout(t);
  }, [message]);

  async function copyPlayerUrl(url: string) {
    await navigator.clipboard.writeText(url);
    setMessage('Link do player copiado.');
  }

  async function handleCreateDisplay(e: FormEvent) {
    e.preventDefault();
    if (!canAdmin) return;
    await api.createTvDisplay(displayForm);
    setMessage('TV cadastrada com sucesso.');
    setDisplayForm({ ...displayForm, name: '', slug: '' });
    await refresh();
  }

  async function handleCall(e: FormEvent) {
    e.preventDefault();
    if (!canWrite) return;
    await api.callTvQueue(callForm);
    setMessage('Chamada enviada para as TVs.');
    setCallForm({ ...callForm, ticketNumber: '', patientName: '' });
    await refresh();
  }

  async function handleKioskCall(ticket: KioskTicketDto) {
    if (!canWrite) return;
    await api.callTvKioskTicket(ticket.id, {
      ...kioskCallForm,
      destination: kioskCallForm.destination || ticket.sector || 'Atendimento',
    });
    setMessage(`Senha ${ticket.ticketNumber} chamada na TV.`);
    await refresh();
  }

  async function handleUpload(e: FormEvent) {
    e.preventDefault();
    if (!canAdmin || !uploadFile) return;
    const form = new FormData();
    form.append('title', uploadTitle);
    form.append('mediaType', uploadFile.type.startsWith('video/') ? '2' : '1');
    form.append('file', uploadFile);
    await api.uploadTvMedia(form);
    setUploadTitle('');
    setUploadFile(null);
    setMessage('Mídia enviada.');
    await refresh();
  }

  async function handleCreateNews(e: FormEvent) {
    e.preventDefault();
    if (!canAdmin) return;
    await api.createTvNews(newsForm);
    setNewsForm({ title: '', summary: '', sector: '' });
    setMessage('Notícia publicada.');
    await refresh();
  }

  async function handleCreateAnnouncement(e: FormEvent) {
    e.preventDefault();
    if (!canAdmin) return;
    await api.createTvAnnouncement(announcementForm);
    setAnnouncementForm({ title: '', body: '', sector: '', priority: 1 });
    setMessage('Aviso publicado.');
    await refresh();
  }

  async function handleCreateCampaign(e: FormEvent) {
    e.preventDefault();
    if (!canAdmin) return;
    await api.createTvCampaign(campaignForm);
    setCampaignForm({ ...campaignForm, name: '', mediaIds: [] });
    setMessage('Campanha criada.');
    await refresh();
  }

  const selectedLayout = layouts.find((l) => l.id === selectedLayoutId) ?? null;
  const activeTab = TAB_CONFIG.find((t) => t.id === tab)!;

  const tabCounts: Partial<Record<Tab, number>> = {
    displays: displays.length,
    layouts: layouts.length,
    campaigns: campaigns.length,
    media: media.length,
    news: news.length,
    announcements: announcements.length,
    calls: kioskTickets.length,
  };

  return (
    <div className="tv-admin-page">
      <PageHeader
        title="TV Corporativa"
        subtitle="Central de Comunicação Inteligente — painéis, campanhas, filas e conteúdo em tempo real."
      />

      {error ? <div className="alert alert-danger tv-admin-alert">{error}</div> : null}
      {message ? <div className="alert alert-success tv-admin-alert">{message}</div> : null}

      <nav className="tv-admin-tabs" role="tablist" aria-label="Seções da TV Corporativa">
        {TAB_CONFIG.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            className={tab === t.id ? 'active' : ''}
            onClick={() => setTab(t.id)}
          >
            <span className="tv-tab-icon" aria-hidden>{t.icon}</span>
            <span className="tv-tab-label">{t.label}</span>
            {tabCounts[t.id] !== undefined && tabCounts[t.id]! > 0 ? (
              <span className="tv-tab-badge">{tabCounts[t.id]}</span>
            ) : null}
          </button>
        ))}
      </nav>

      <p className="tv-admin-tab-hint">{activeTab.hint}</p>

      {tab === 'monitor' && monitor ? (
        <div className="tv-admin-tab-content">
          <div className="kpi-grid tv-admin-kpis">
            <KpiCard label="TVs cadastradas" value={monitor.totalDisplays} variant="primary" />
            <KpiCard label="Online" value={monitor.onlineDisplays} variant="success" />
            <KpiCard label="Offline" value={monitor.offlineDisplays} variant="warning" />
            <KpiCard label="Chamadas hoje" value={monitor.callsToday} variant="info" />
            <KpiCard label="Mídias ativas" value={monitor.activeMedia} variant="neutral" />
          </div>

          <div className="tv-display-cards">
            {monitor.displays.map((d) => (
              <article key={d.id} className={`tv-display-card tv-display-card-${d.status}`}>
                <div className="tv-display-card-top">
                  <span className={`tv-status tv-status-${d.status}`}>{d.status === 1 ? 'Online' : 'Offline'}</span>
                  <span className="tv-display-card-sector">{d.sector ?? 'Geral'}</span>
                </div>
                <h4>{d.name}</h4>
                <p className="tv-display-meta">
                  {d.lastSeenAt
                    ? `Última comunicação: ${new Date(d.lastSeenAt).toLocaleString('pt-BR')}`
                    : 'Sem comunicação recente'}
                </p>
                <div className="tv-display-card-actions">
                  <a className="btn btn-sm" href={d.playerUrl} target="_blank" rel="noreferrer">Abrir player</a>
                  <button type="button" className="btn btn-sm btn-secondary" onClick={() => copyPlayerUrl(d.playerUrl).catch(console.error)}>
                    Copiar link
                  </button>
                </div>
              </article>
            ))}
          </div>

          <TvPanel title="Detalhamento das telas" subtitle="Visão completa para suporte e auditoria">
            <div className="tv-table-wrap">
              <table className="data-table tv-admin-table">
                <thead>
                  <tr>
                    <th>Nome</th>
                    <th>Setor</th>
                    <th>Status</th>
                    <th>Última comunicação</th>
                    <th>Player</th>
                  </tr>
                </thead>
                <tbody>
                  {monitor.displays.map((d) => (
                    <tr key={d.id}>
                      <td><strong>{d.name}</strong></td>
                      <td>{d.sector ?? '—'}</td>
                      <td><span className={`tv-status tv-status-${d.status}`}>{d.status === 1 ? 'Online' : 'Offline'}</span></td>
                      <td>{d.lastSeenAt ? new Date(d.lastSeenAt).toLocaleString('pt-BR') : '—'}</td>
                      <td>
                        <a href={d.playerUrl} target="_blank" rel="noreferrer">Abrir</a>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </TvPanel>
        </div>
      ) : null}

      {tab === 'displays' ? (
        <div className="tv-admin-tab-content tv-admin-split">
          <TvPanel title="Telas cadastradas" subtitle={`${displays.length} TV(s) no parque`}>
            {displays.length === 0 ? (
              <TvEmpty icon="📺" title="Nenhuma TV cadastrada" text="Cadastre a primeira tela ao lado para gerar o link do player." />
            ) : (
              <div className="tv-display-cards tv-display-cards-compact">
                {displays.map((d) => (
                  <article key={d.id} className="tv-display-card">
                    <h4>{d.name}</h4>
                    <p className="tv-display-meta">{d.sector} · {d.layoutName ?? 'Layout padrão'}</p>
                    <code className="tv-slug">{d.slug}</code>
                    <div className="tv-display-card-actions">
                      <a className="btn btn-sm" href={d.playerUrl} target="_blank" rel="noreferrer">Player</a>
                      <button type="button" className="btn btn-sm btn-secondary" onClick={() => copyPlayerUrl(d.playerUrl).catch(console.error)}>Copiar</button>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </TvPanel>
          {canAdmin ? (
            <TvPanel title="Nova TV" subtitle="Gera automaticamente o token do player">
              <form className="tv-form" onSubmit={handleCreateDisplay}>
                <div className="tv-form-grid">
                  <label>Nome<input value={displayForm.name} onChange={(e) => setDisplayForm({ ...displayForm, name: e.target.value })} placeholder="TV Recepção" required /></label>
                  <label>Identificador (slug)<input value={displayForm.slug} onChange={(e) => setDisplayForm({ ...displayForm, slug: e.target.value })} placeholder="recepcao" required /></label>
                  <label>Setor<input value={displayForm.sector ?? ''} onChange={(e) => setDisplayForm({ ...displayForm, sector: e.target.value })} placeholder="Recepção" /></label>
                  <label>Layout
                    <select value={displayForm.layoutId ?? ''} onChange={(e) => setDisplayForm({ ...displayForm, layoutId: e.target.value || undefined })}>
                      <option value="">Padrão</option>
                      {layouts.map((l) => <option key={l.id} value={l.id}>{l.name}</option>)}
                    </select>
                  </label>
                  <label>Cidade (clima)<input value={displayForm.weatherCity ?? ''} onChange={(e) => setDisplayForm({ ...displayForm, weatherCity: e.target.value })} /></label>
                </div>
                <label className="tv-form-check">
                  <input type="checkbox" checked={displayForm.showPatientName} onChange={(e) => setDisplayForm({ ...displayForm, showPatientName: e.target.checked })} />
                  Exibir nome do paciente na chamada (LGPD)
                </label>
                <button type="submit" className="btn">Cadastrar TV</button>
              </form>
            </TvPanel>
          ) : null}
        </div>
      ) : null}

      {tab === 'layouts' ? (
        <div className="tv-admin-tab-content">
          {canAdmin ? (
            <TvPanel
              title="Editor de layouts"
              subtitle="Arraste e redimensione as zonas. Cada zona exibe um widget no player."
              actions={(
                <label className="tv-layout-select">
                  Layout
                  <select value={selectedLayoutId} onChange={(e) => setSelectedLayoutId(e.target.value)}>
                    {layouts.map((l) => <option key={l.id} value={l.id}>{l.name}</option>)}
                  </select>
                </label>
              )}
            >
              <TvLayoutEditor
                key={selectedLayout?.id ?? 'new'}
                layout={selectedLayout}
                onSave={async (data) => {
                  if (selectedLayout) {
                    await api.updateTvLayout(selectedLayout.id, data);
                    setMessage('Layout atualizado.');
                  } else {
                    const created = await api.createTvLayout(data);
                    setSelectedLayoutId(created.id);
                    setMessage('Layout criado.');
                  }
                  await refresh();
                }}
              />
            </TvPanel>
          ) : (
            <TvPanel title="Layouts disponíveis" subtitle="Somente leitura">
              <ul className="tv-item-list">
                {layouts.map((l) => (
                  <li key={l.id}>
                    <strong>{l.name}</strong>
                    <span>{l.description ?? `${l.zones.length} zona(s)`}</span>
                  </li>
                ))}
              </ul>
            </TvPanel>
          )}
        </div>
      ) : null}

      {tab === 'campaigns' ? (
        <div className="tv-admin-tab-content tv-admin-split">
          <TvPanel title="Campanhas programadas" subtitle="Mídias exibidas por horário e dia da semana">
            {campaigns.length === 0 ? (
              <TvEmpty icon="📅" title="Sem campanhas" text="Crie uma campanha para exibir mídias em horários específicos." />
            ) : (
              <div className="tv-campaign-list">
                {campaigns.map((c) => (
                  <article key={c.id} className="tv-campaign-card">
                    <div className="tv-campaign-card-head">
                      <strong>{c.name}</strong>
                      <span className="tv-priority-badge">P{c.priority}</span>
                    </div>
                    <div className="tv-campaign-meta">
                      {c.dailyStart ? <span>⏰ {c.dailyStart} – {c.dailyEnd}</span> : null}
                      <span>📆 {formatWeekDays(c.daysOfWeek)}</span>
                      {c.sector ? <span>📍 {c.sector}</span> : null}
                      <span>🎬 {c.mediaIds.length} mídia(s)</span>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </TvPanel>
          {canAdmin ? (
            <TvPanel title="Nova campanha">
              <form className="tv-form" onSubmit={handleCreateCampaign}>
                <label>Nome<input value={campaignForm.name} onChange={(e) => setCampaignForm({ ...campaignForm, name: e.target.value })} required /></label>
                <label>Setor (opcional)<input value={campaignForm.sector ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, sector: e.target.value })} /></label>
                <label>Horário diário
                  <div className="tv-inline-fields">
                    <input type="time" value={campaignForm.dailyStart ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, dailyStart: e.target.value })} />
                    <input type="time" value={campaignForm.dailyEnd ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, dailyEnd: e.target.value })} />
                  </div>
                </label>
                <label>Dias da semana
                  <input value={campaignForm.daysOfWeek ?? ''} onChange={(e) => setCampaignForm({ ...campaignForm, daysOfWeek: e.target.value })} placeholder="1,2,3,4,5 (0=Dom)" />
                  <span className="tv-field-hint">0=Domingo · 6=Sábado</span>
                </label>
                <label>Prioridade<input type="number" min={1} value={campaignForm.priority} onChange={(e) => setCampaignForm({ ...campaignForm, priority: Number(e.target.value) })} /></label>
                <label>Mídias (Ctrl+clique para várias)
                  <select
                    multiple
                    className="tv-select-multi"
                    value={campaignForm.mediaIds}
                    onChange={(e) => setCampaignForm({
                      ...campaignForm,
                      mediaIds: Array.from(e.target.selectedOptions).map((o) => o.value),
                    })}
                  >
                    {media.map((m) => <option key={m.id} value={m.id}>{m.title}</option>)}
                  </select>
                </label>
                <button type="submit" className="btn">Criar campanha</button>
              </form>
            </TvPanel>
          ) : null}
        </div>
      ) : null}

      {tab === 'media' ? (
        <div className="tv-admin-tab-content tv-admin-split">
          <TvPanel title="Biblioteca de mídias" subtitle={`${media.length} arquivo(s)`}>
            {media.length === 0 ? (
              <TvEmpty icon="🎬" title="Biblioteca vazia" text="Envie imagens ou vídeos institucionais para exibir nas TVs." />
            ) : (
              <div className="tv-media-grid">
                {media.map((m) => (
                  <article key={m.id} className="tv-media-card">
                    <div className="tv-media-thumb">
                      {isTvVideoMedia(m.mediaType) ? (
                        <video src={resolveTvMediaUrl(m.url)} muted />
                      ) : (
                        <img src={resolveTvMediaUrl(m.url)} alt={m.title} />
                      )}
                      <span className="tv-media-type">{isTvVideoMedia(m.mediaType) ? 'Vídeo' : 'Imagem'}</span>
                    </div>
                    <div className="tv-media-info">
                      <strong>{m.title}</strong>
                      <span>{m.durationSeconds}s · prioridade {m.priority}</span>
                    </div>
                  </article>
                ))}
              </div>
            )}
          </TvPanel>
          {canAdmin ? (
            <TvPanel title="Upload de mídia">
              <form className="tv-form" onSubmit={handleUpload}>
                <label>Título<input value={uploadTitle} onChange={(e) => setUploadTitle(e.target.value)} required /></label>
                <label className="tv-file-drop">
                  Arquivo (imagem ou vídeo)
                  <input type="file" accept="image/*,video/*" onChange={(e) => setUploadFile(e.target.files?.[0] ?? null)} required />
                  {uploadFile ? <span className="tv-field-hint">{uploadFile.name}</span> : null}
                </label>
                <button type="submit" className="btn">Enviar mídia</button>
              </form>
            </TvPanel>
          ) : null}
        </div>
      ) : null}

      {tab === 'news' ? (
        <div className="tv-admin-tab-content tv-admin-split">
          <TvPanel title="Notícias no rodapé" subtitle="Texto corrido na faixa inferior do player">
            {news.length === 0 ? (
              <TvEmpty icon="📰" title="Sem notícias" text="Publique campanhas de saúde e avisos institucionais." />
            ) : (
              <ul className="tv-feed-list">
                {news.map((n) => (
                  <li key={n.id}>
                    <strong>{n.title}</strong>
                    <p>{n.summary}</p>
                    {n.sector ? <span className="tv-feed-tag">{n.sector}</span> : null}
                  </li>
                ))}
              </ul>
            )}
          </TvPanel>
          {canAdmin ? (
            <TvPanel title="Nova notícia">
              <form className="tv-form" onSubmit={handleCreateNews}>
                <label>Título<input value={newsForm.title} onChange={(e) => setNewsForm({ ...newsForm, title: e.target.value })} required /></label>
                <label>Resumo<textarea rows={4} value={newsForm.summary ?? ''} onChange={(e) => setNewsForm({ ...newsForm, summary: e.target.value })} /></label>
                <label>Setor (opcional)<input value={newsForm.sector ?? ''} onChange={(e) => setNewsForm({ ...newsForm, sector: e.target.value })} /></label>
                <button type="submit" className="btn">Publicar notícia</button>
              </form>
            </TvPanel>
          ) : null}
        </div>
      ) : null}

      {tab === 'announcements' ? (
        <div className="tv-admin-tab-content tv-admin-split">
          <TvPanel title="Avisos em destaque" subtitle="Blocos de alerta nos layouts que incluem o widget Avisos">
            {announcements.length === 0 ? (
              <TvEmpty icon="📢" title="Sem avisos" text="Comunicados urgentes aparecem no painel lateral do player." />
            ) : (
              <ul className="tv-feed-list tv-feed-list-announce">
                {announcements.map((a) => (
                  <li key={a.id}>
                    <div className="tv-announce-head">
                      <strong>{a.title}</strong>
                      <span className="tv-priority-badge">P{a.priority}</span>
                    </div>
                    <p>{a.body}</p>
                  </li>
                ))}
              </ul>
            )}
          </TvPanel>
          {canAdmin ? (
            <TvPanel title="Novo aviso">
              <form className="tv-form" onSubmit={handleCreateAnnouncement}>
                <label>Título<input value={announcementForm.title} onChange={(e) => setAnnouncementForm({ ...announcementForm, title: e.target.value })} required /></label>
                <label>Texto<textarea rows={5} value={announcementForm.body} onChange={(e) => setAnnouncementForm({ ...announcementForm, body: e.target.value })} required /></label>
                <label>Prioridade<input type="number" min={1} value={announcementForm.priority} onChange={(e) => setAnnouncementForm({ ...announcementForm, priority: Number(e.target.value) })} /></label>
                <button type="submit" className="btn">Publicar aviso</button>
              </form>
            </TvPanel>
          ) : null}
        </div>
      ) : null}

      {tab === 'calls' ? (
        <div className="tv-admin-tab-content">
          <div className="tv-calls-top">
            {canWrite ? (
              <TvPanel title="Chamar paciente agora" subtitle="Dispara a senha nas TVs do setor ou em uma tela específica">
                <form className="tv-form tv-call-form" onSubmit={handleCall}>
                  <div className="tv-form-grid tv-form-grid-3">
                    <label>Senha<input value={callForm.ticketNumber} onChange={(e) => setCallForm({ ...callForm, ticketNumber: e.target.value })} placeholder="A042" required /></label>
                    <label>Nome (opcional)<input value={callForm.patientName ?? ''} onChange={(e) => setCallForm({ ...callForm, patientName: e.target.value })} /></label>
                    <label>Destino<input value={callForm.destination} onChange={(e) => setCallForm({ ...callForm, destination: e.target.value })} placeholder="Consultório 03" required /></label>
                  </div>
                  <div className="tv-form-grid">
                    <label>TV específica
                      <select value={callForm.displayId ?? ''} onChange={(e) => setCallForm({ ...callForm, displayId: e.target.value || undefined })}>
                        <option value="">Todas do setor</option>
                        {displays.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
                      </select>
                    </label>
                    <label className="tv-form-check tv-form-check-inline">
                      <input type="checkbox" checked={!!callForm.showPatientName} onChange={(e) => setCallForm({ ...callForm, showPatientName: e.target.checked })} />
                      Exibir nome na TV
                    </label>
                  </div>
                  <button type="submit" className="btn tv-btn-call">Chamar agora</button>
                </form>
              </TvPanel>
            ) : null}

            <TvPanel title="Senhas do totem" subtitle={`${kioskTickets.length} aguardando chamada`}>
              {kioskTickets.length === 0 ? (
                <TvEmpty icon="🎫" title="Fila vazia" text="Nenhuma senha pendente no totem no momento." />
              ) : (
                <>
                  <div className="tv-table-wrap">
                    <table className="data-table tv-admin-table">
                      <thead><tr><th>Senha</th><th>Paciente</th><th>Setor</th><th></th></tr></thead>
                      <tbody>
                        {kioskTickets.map((t) => (
                          <tr key={t.id}>
                            <td><span className="tv-ticket-pill">{t.ticketNumber}</span></td>
                            <td>{t.patientName ?? '—'}</td>
                            <td>{t.sector ?? '—'}</td>
                            <td>
                              {canWrite ? (
                                <button type="button" className="btn btn-sm tv-btn-call" onClick={() => handleKioskCall(t).catch(console.error)}>
                                  Chamar na TV
                                </button>
                              ) : null}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                  {canWrite ? (
                    <div className="tv-form tv-kiosk-settings">
                      <div className="tv-form-grid">
                        <label>Destino padrão<input value={kioskCallForm.destination} onChange={(e) => setKioskCallForm({ ...kioskCallForm, destination: e.target.value })} /></label>
                        <label>TV específica
                          <select value={kioskCallForm.displayId ?? ''} onChange={(e) => setKioskCallForm({ ...kioskCallForm, displayId: e.target.value || undefined })}>
                            <option value="">Todas do setor</option>
                            {displays.map((d) => <option key={d.id} value={d.id}>{d.name}</option>)}
                          </select>
                        </label>
                      </div>
                    </div>
                  ) : null}
                </>
              )}
            </TvPanel>
          </div>

          <TvPanel title="Histórico de chamadas" subtitle="Últimas 30 chamadas">
            {calls.length === 0 ? (
              <TvEmpty icon="🔔" title="Sem chamadas recentes" text="O histórico aparece aqui após a primeira chamada." />
            ) : (
              <div className="tv-table-wrap">
                <table className="data-table tv-admin-table">
                  <thead><tr><th>Senha</th><th>Paciente</th><th>Destino</th><th>Horário</th></tr></thead>
                  <tbody>
                    {calls.map((c) => (
                      <tr key={c.id}>
                        <td><span className="tv-ticket-pill">{c.ticketNumber}</span></td>
                        <td>{c.patientName ?? '—'}</td>
                        <td>{c.destination}</td>
                        <td>{new Date(c.calledAt).toLocaleString('pt-BR')}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </TvPanel>
        </div>
      ) : null}
    </div>
  );
}

import { useCallback, useEffect, useMemo, useRef, useState, type MutableRefObject } from 'react';
import { useParams, useSearchParams } from 'react-router-dom';
import {
  api,
  resolveTvSpeechUrl,
  type TvLayoutZoneDto,
  type TvPlayerStateDto,
  type TvQueueCallDto,
  type TvNewsDto,
  type TvAnnouncementDto,
  type TvScheduleItemDto,
  type TvWidgetType,
  tvWidgetTypeLabels,
} from '../../api/client';
import { TV_DEFAULT_NEWS } from './tvPlayerDefaults';
import { isHospitalSghLayout } from './tvLayoutThemes';
import { normalizeTvPlayerState } from './tvPlayerStateNormalize';
import { TvHospitalBoard } from './TvHospitalBoard';
import { HospitalLogo } from '../../components/HospitalLogo';
import { isTvVideoMedia } from './tvMediaUtils';
import { buildPatientCallSpeech } from '../../utils/waitingRoomAnnounce';
import {
  connectTvSignageHub,
  disconnectTvSignageHub,
  subscribeTvSignageRefresh,
} from '../../offline/tvSignageRealtimeSync';

function speakCallBrowser(state: TvPlayerStateDto) {
  const call = state.activeCall;
  if (!call || !state.display.enableSound) return;
  if (!('speechSynthesis' in window)) return;

  const text = call.showPatientName && call.patientName
    ? buildPatientCallSpeech(call.patientName, call.destination)
    : `Senha ${call.ticketNumber}, dirija-se a ${call.destination}.`;
  const utterance = new SpeechSynthesisUtterance(text);
  utterance.lang = 'pt-BR';
  utterance.rate = 0.95;
  window.speechSynthesis.cancel();
  window.speechSynthesis.speak(utterance);
}

function playCallSpeech(state: TvPlayerStateDto, audioRef: MutableRefObject<HTMLAudioElement | null>) {
  if (!state.activeCall || !state.display.enableSound) return;

  const provider = state.speechProvider?.toLowerCase() ?? 'browser';
  if (provider === 'browser') {
    speakCallBrowser(state);
    return;
  }

  if (!state.activeCallSpeechUrl) return;
  if (audioRef.current) {
    audioRef.current.pause();
    audioRef.current = null;
  }
  const audio = new Audio(resolveTvSpeechUrl(state.activeCallSpeechUrl));
  audioRef.current = audio;
  audio.play().catch(() => speakCallBrowser(state));
}

function WidgetContent({ widget, state, mediaIndex }: { widget: TvWidgetType; state: TvPlayerStateDto; mediaIndex: number }) {
  const now = useMemo(() => new Date(), [state.generatedAt]);

  if (widget === 2) {
    const calls = state.recentCalls;
    const active = state.activeCall;
    const recent = calls.filter((c: TvQueueCallDto) => !c.isActive);
    return (
      <div className="tv-widget-queue">
        <div className="tv-queue-header">
          <div className="tv-queue-header-icon" aria-hidden>🎫</div>
          <div>
            <div className="tv-queue-header-label">Atendimento</div>
            <div className="tv-queue-header-title">Senhas chamadas</div>
          </div>
        </div>
        {active ? (
          <div className="tv-call-active">
            <div className="tv-call-label">Chamando agora</div>
            <div className="tv-call-ticket">{active.ticketNumber}</div>
            {active.showPatientName && active.patientName ? <div className="tv-call-name">{active.patientName}</div> : null}
            <div className="tv-call-dest">
              <span className="tv-call-dest-label">Destino</span>
              {active.destination}
            </div>
          </div>
        ) : (
          <div className="tv-call-idle">
            <div className="tv-call-idle-icon" aria-hidden>⏳</div>
            <p>Aguarde sua senha aparecer aqui</p>
          </div>
        )}
        {recent.length > 0 ? (
          <>
            <div className="tv-call-list-title">Últimas chamadas</div>
            <div className="tv-call-list">
              {recent.map((c: TvQueueCallDto) => (
                <div key={c.id} className="tv-call-item">
                  <strong>{c.ticketNumber}</strong>
                  <span>{c.destination}</span>
                </div>
              ))}
            </div>
          </>
        ) : null}
      </div>
    );
  }

  if (widget === 3) {
    const items = state.news;
    const tickerItems: TvNewsDto[] = items.length === 0 ? [TV_DEFAULT_NEWS] : items;
    return (
      <div className="tv-widget-ticker-wrap">
        <div className="tv-ticker-badge">Notícias</div>
        <div className="tv-widget-ticker">
          <div className="tv-ticker-track">
            {[...tickerItems, ...tickerItems].map((n, i) => (
              <span key={`${n.id}-${i}`} className="tv-ticker-item">
                <strong>{n.title}</strong>
                <em>{n.summary}</em>
              </span>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (widget === 4 && state.weather) {
    return (
      <div className="tv-widget-weather">
        <div className="tv-weather-icon">{state.weather.icon ?? '☀️'}</div>
        <div className="tv-weather-body">
          <div className="tv-weather-city">{state.weather.city}</div>
          <div className="tv-weather-temp">{state.weather.temperatureC.toFixed(0)}°</div>
          <div className="tv-weather-condition">{state.weather.condition}</div>
          {state.weather.humidityPercent > 0 ? (
            <div className="tv-weather-humidity">Umidade {state.weather.humidityPercent}%</div>
          ) : null}
        </div>
      </div>
    );
  }

  if (widget === 5) {
    return (
      <div className="tv-widget-clock">
        <div className="tv-clock-time">{now.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</div>
        <div className="tv-clock-date">{now.toLocaleDateString('pt-BR', { weekday: 'long', day: '2-digit', month: 'long' })}</div>
      </div>
    );
  }

  if (widget === 6 && state.dashboard) {
    const d = state.dashboard;
    return (
      <div className="tv-widget-dashboard">
        <div><strong>{d.attendancesToday}</strong><span>Atendimentos hoje</span></div>
        <div><strong>{d.emergencyWaiting}</strong><span>PS aguardando</span></div>
        <div><strong>{Math.round(d.averageEmergencyWaitMinutes)} min</strong><span>Espera média</span></div>
        <div><strong>{d.bedOccupancyRate.toFixed(0)}%</strong><span>Ocupação</span></div>
      </div>
    );
  }

  if (widget === 7) {
    return (
      <div className="tv-widget-announcements">
        {state.announcements.map((a: TvAnnouncementDto) => (
          <div key={a.id} className="tv-announcement-item">
            <strong>{a.title}</strong>
            <p>{a.body}</p>
          </div>
        ))}
      </div>
    );
  }

  if (widget === 9) {
    const items = state.schedule ?? [];
    return (
      <div className="tv-widget-schedule">
        <div className="tv-schedule-title">Escalas e agendas do dia</div>
        {items.length === 0 ? (
          <p className="tv-schedule-empty">Sem escalas para exibir.</p>
        ) : items.map((item: TvScheduleItemDto, index: number) => (
          <div key={`${item.name}-${index}`} className="tv-schedule-item">
            <div>
              <strong>{item.name}</strong>
              <span>{item.roleOrSpecialty}</span>
            </div>
            <div className="tv-schedule-meta">
              <span>{item.shiftLabel}</span>
              {item.timeLabel ? <span>{item.timeLabel}</span> : null}
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (widget === 1) {
    const media = state.media;
    if (media.length === 0) {
      return (
        <div className="tv-widget-media-empty">
          <div className="tv-media-empty-icon" aria-hidden>🏥</div>
          <h2>{state.display.name}</h2>
          <p>Central de Comunicação Hospitalar</p>
          <span className="tv-media-empty-hint">Conteúdo institucional em breve</span>
        </div>
      );
    }
    const item = media[mediaIndex % media.length];
    if (isTvVideoMedia(item.mediaType)) {
      return (
        <div className="tv-media-frame">
          <video key={item.id} className="tv-media-video" src={item.url} autoPlay muted loop playsInline />
          {item.title ? <div className="tv-media-caption">{item.title}</div> : null}
        </div>
      );
    }
    return (
      <div className="tv-media-frame">
        <img key={item.id} className="tv-media-image" src={item.url} alt={item.title} />
        {item.title ? <div className="tv-media-caption">{item.title}</div> : null}
      </div>
    );
  }

  return <div className="tv-widget-placeholder">{tvWidgetTypeLabels[widget]}</div>;
}

function Zone({ zone, state, mediaIndex }: { zone: TvLayoutZoneDto; state: TvPlayerStateDto; mediaIndex: number }) {
  return (
    <div
      className="tv-zone"
      style={{
        left: `${zone.x}%`,
        top: `${zone.y}%`,
        width: `${zone.w}%`,
        height: `${zone.h}%`,
      }}
    >
      <WidgetContent widget={zone.widget} state={state} mediaIndex={mediaIndex} />
    </div>
  );
}

export function TvPlayerPage() {
  const { slug = '' } = useParams();
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';
  const [state, setState] = useState<TvPlayerStateDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [mediaIndex, setMediaIndex] = useState(0);
  const lastCallId = useRef<string | null>(null);
  const speechAudioRef = useRef<HTMLAudioElement | null>(null);

  const load = useCallback(async () => {
    if (!slug || !token) {
      setError('URL da TV inválida. Informe o token de acesso.');
      return;
    }
    try {
      const next = normalizeTvPlayerState(await api.getTvPlayerState(slug, token));
      setState(next);
      setError(null);
      if (next.activeCall && next.activeCall.id !== lastCallId.current) {
        lastCallId.current = next.activeCall.id;
        playCallSpeech(next, speechAudioRef);
      }
      await api.sendTvHeartbeat(slug, token, {
        resolution: `${window.screen.width}x${window.screen.height}`,
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar TV');
    }
  }, [slug, token]);

  useEffect(() => {
    load().catch(console.error);
    const timer = window.setInterval(() => load().catch(console.error), 4000);
    return () => window.clearInterval(timer);
  }, [load]);

  useEffect(() => {
    if (!slug || !token) return undefined;

    connectTvSignageHub(slug, token).catch(console.error);
    const unsubscribe = subscribeTvSignageRefresh(() => {
      load().catch(console.error);
    });

    return () => {
      unsubscribe();
      disconnectTvSignageHub().catch(console.error);
    };
  }, [slug, token, load]);

  useEffect(() => {
    if (!state?.media.length) return;
    const current = state.media[mediaIndex % state.media.length];
    const duration = (current?.durationSeconds ?? 15) * 1000;
    const timer = window.setInterval(() => setMediaIndex((i) => i + 1), duration);
    return () => window.clearInterval(timer);
  }, [state, mediaIndex]);

  if (error) {
    return <div className="tv-player-error">{error}</div>;
  }

  if (!state) {
    return <div className="tv-player-loading">Carregando painel...</div>;
  }

  if (isHospitalSghLayout(
    state.layout,
    state.display.slug || slug,
    state.display.sector,
    state.display.name,
    state.display.layoutName,
  )) {
    return (
      <div className="tv-player tv-player-hospital-sgh">
        <TvHospitalBoard state={state} mediaIndex={mediaIndex} />
      </div>
    );
  }

  return (
    <div className={`tv-player ${state.display.orientation === 2 ? 'tv-player-vertical' : ''}`}>
      <div className="tv-player-backdrop" aria-hidden />
      <header className="tv-player-chrome">
        <div className="tv-player-brand">
          <HospitalLogo variant="full" height={40} className="tv-player-brand-logo" />
        </div>
        <div className="tv-player-chrome-time">
          {new Date(state.generatedAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
        </div>
      </header>
      {state.layout.zones.map((zone: TvLayoutZoneDto) => (
        <Zone key={zone.id} zone={zone} state={state} mediaIndex={mediaIndex} />
      ))}
    </div>
  );
}

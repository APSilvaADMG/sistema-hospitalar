import { useMemo } from 'react';
import type { TvAnnouncementDto, TvPlayerStateDto, TvQueueCallDto } from '../../api/client';
import { HospitalLogo } from '../../components/HospitalLogo';
import { isTvVideoMedia } from './tvMediaUtils';
import { parseDestination } from './tvLayoutThemes';

const FOOTER_VALUES = [
  { icon: '🤝', label: 'Humanização' },
  { icon: '🛡️', label: 'Segurança' },
  { icon: '👥', label: 'Respeito' },
  { icon: '✅', label: 'Compromisso' },
  { icon: '🏅', label: 'Qualidade' },
] as const;

function CallTable({ calls, activeId }: { calls: TvQueueCallDto[]; activeId?: string }) {
  if (calls.length === 0) {
    return (
      <div className="tv-hospital-queue-empty">
        <p>Aguardando chamadas de pacientes</p>
      </div>
    );
  }

  return (
    <table className="tv-hospital-queue-table">
      <tbody>
        {calls.map((c) => {
          const { guiche, sala } = parseDestination(c.destination);
          const isActive = c.id === activeId || c.isActive;
          return (
            <tr key={c.id} className={isActive ? 'is-active' : ''}>
              <td className="col-senha">
                <span className="tv-hospital-senha">{c.ticketNumber}</span>
                {c.showPatientName && c.patientName ? (
                  <span className="tv-hospital-patient">{c.patientName}</span>
                ) : null}
              </td>
              <td className="col-guiche">{guiche}</td>
              <td className="col-sala">{sala}</td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
}

function MediaPanel({ state, mediaIndex }: { state: TvPlayerStateDto; mediaIndex: number }) {
  const media = state.media;
  if (media.length === 0) {
    return (
      <div className="tv-hospital-ad-placeholder">
        <span className="tv-hospital-ad-icon" aria-hidden>🖼️</span>
        <p>ESPAÇO RESERVADO PARA SUA PUBLICIDADE</p>
      </div>
    );
  }
  const item = media[mediaIndex % media.length];
  if (isTvVideoMedia(item.mediaType)) {
    return <video key={item.id} className="tv-hospital-ad-media" src={item.url} autoPlay muted loop playsInline />;
  }
  return <img key={item.id} className="tv-hospital-ad-media" src={item.url} alt={item.title} />;
}

function AnnouncementsPanel({ items }: { items: TvAnnouncementDto[] }) {
  if (items.length === 0) {
    return (
      <div className="tv-hospital-comunicados-empty">
        <p>Nenhum comunicado no momento.</p>
      </div>
    );
  }
  return (
    <ul className="tv-hospital-comunicados-list">
      {items.map((a) => (
        <li key={a.id}>
          <strong>{a.title}</strong>
          <p>{a.body}</p>
        </li>
      ))}
    </ul>
  );
}

export function TvHospitalBoard({ state, mediaIndex }: { state: TvPlayerStateDto; mediaIndex: number }) {
  const now = useMemo(() => new Date(state.generatedAt), [state.generatedAt]);
  const calls = state.recentCalls;
  const activeId = state.activeCall?.id;
  const displayTitle = state.display.sector ?? state.display.name;

  return (
    <div className="tv-hospital-board">
      <header className="tv-hospital-header">
        <div className="tv-hospital-header-left">
          <div className="tv-hospital-logo">
            <HospitalLogo variant="full" height={56} className="tv-hospital-logo-img" />
          </div>
          <div className="tv-hospital-header-divider" aria-hidden />
          <p className="tv-hospital-mission">
            CUIDAR DE PESSOAS É A NOSSA MISSÃO <span aria-hidden>❤️</span>
          </p>
        </div>
        <div className="tv-hospital-header-right">
          <div className="tv-hospital-clock-block">
            <span className="tv-hospital-clock-icon" aria-hidden>🕐</span>
            <span className="tv-hospital-clock-time">
              {now.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
            </span>
          </div>
          <div className="tv-hospital-date-block">
            <span className="tv-hospital-date-icon" aria-hidden>📅</span>
            <div>
              <div className="tv-hospital-date-line">
                {now.toLocaleDateString('pt-BR')}
              </div>
              <div className="tv-hospital-date-weekday">
                {now.toLocaleDateString('pt-BR', { weekday: 'long' })}
              </div>
            </div>
          </div>
        </div>
        <div className="tv-hospital-header-curve" aria-hidden />
      </header>

      <main className="tv-hospital-main">
        <section className="tv-hospital-queue-panel">
          <div className="tv-hospital-panel-title tv-hospital-panel-title-queue">
            <span className="tv-hospital-panel-icon" aria-hidden>👥</span>
            CHAMADAS DE PACIENTES
          </div>
          <div className="tv-hospital-queue-head">
            <span>SENHA</span>
            <span>GUICHÊ</span>
            <span>SALA</span>
          </div>
          <div className="tv-hospital-queue-body">
            <CallTable calls={calls} activeId={activeId} />
            <div className="tv-hospital-ekg" aria-hidden />
          </div>
          <div className="tv-hospital-sector-tag">{displayTitle}</div>
        </section>

        <aside className="tv-hospital-sidebar">
          <section className="tv-hospital-side-card tv-hospital-ad-card">
            <div className="tv-hospital-panel-title tv-hospital-panel-title-ad">
              <span className="tv-hospital-panel-icon" aria-hidden>📢</span>
              PUBLICIDADE
            </div>
            <div className="tv-hospital-ad-body">
              <MediaPanel state={state} mediaIndex={mediaIndex} />
            </div>
          </section>

          <section className="tv-hospital-side-card tv-hospital-comunicados-card">
            <div className="tv-hospital-panel-title tv-hospital-panel-title-alert">
              <span className="tv-hospital-panel-icon" aria-hidden>🔔</span>
              COMUNICADOS
            </div>
            <div className="tv-hospital-comunicados-body">
              <AnnouncementsPanel items={state.announcements} />
            </div>
          </section>
        </aside>
      </main>

      <footer className="tv-hospital-footer">
        <div className="tv-hospital-footer-left">
          <span className="tv-hospital-footer-icon" aria-hidden>⚕️</span>
          <div>
            <strong>SAÚDE, QUALIDADE E HUMANIZAÇÃO</strong>
            <span>PARA UM FUTURO MELHOR</span>
          </div>
        </div>
        <div className="tv-hospital-footer-values">
          {FOOTER_VALUES.map((v) => (
            <div key={v.label} className="tv-hospital-value-item">
              <span aria-hidden>{v.icon}</span>
              <small>{v.label.toUpperCase()}</small>
            </div>
          ))}
        </div>
        <div className="tv-hospital-footer-curve" aria-hidden />
      </footer>
    </div>
  );
}

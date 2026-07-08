import type { MailListItemDto, MessagePriority } from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';

const priorityLabels: Record<MessagePriority, string> = {
  Baixa: 'Baixa',
  Normal: 'Normal',
  Alta: 'Alta',
  Urgente: 'Urgente',
  Critica: 'Crítica',
};

type Props = {
  items: MailListItemDto[];
  selectedId?: string;
  onSelect: (id: string) => void;
  loading?: boolean;
};

export function MailList({ items, selectedId, onSelect, loading }: Props) {
  if (loading) {
    return <p className="text-muted">Carregando mensagens…</p>;
  }

  if (items.length === 0) {
    return <p className="text-muted">Nenhuma mensagem nesta pasta.</p>;
  }

  return (
    <ul className="connect-mail-list">
      {items.map((item) => (
        <li
          key={item.id}
          className={`connect-mail-item${item.isRead ? '' : ' unread'}${selectedId === item.id ? ' selected' : ''}`}
          onClick={() => onSelect(item.id)}
        >
          <div>
            <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
              <span className={`connect-priority ${item.priority.toLowerCase()}`}>
                {priorityLabels[item.priority]}
              </span>
              <strong>{item.subject}</strong>
            </div>
            <div className="text-muted" style={{ fontSize: '0.85rem' }}>
              {item.senderName} · {formatBrDateTime(item.createdAt)}
            </div>
            <div style={{ fontSize: '0.9rem', marginTop: '0.25rem' }}>{item.preview}</div>
          </div>
          {item.attachmentCount > 0 ? (
            <span className="text-muted" title="Anexos">📎 {item.attachmentCount}</span>
          ) : null}
        </li>
      ))}
    </ul>
  );
}

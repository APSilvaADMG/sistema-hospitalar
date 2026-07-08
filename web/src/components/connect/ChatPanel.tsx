import { useCallback, useEffect, useRef, useState } from 'react';
import {
  api,
  type ChatMessageDto,
  type ChatRoomDto,
  type UserListDto,
} from '../../api/client';
import { formatBrDateTime } from '../../utils/dateUtils';
import {
  subscribeConnectChatRefresh,
  subscribeConnectCommRefresh,
} from '../../offline/connectRealtimeSync';
import { useAuth } from '../../auth/AuthContext';

function messagesFingerprint(list: ChatMessageDto[]): string {
  if (list.length === 0) return '';
  const last = list[list.length - 1];
  return `${list.length}:${last.id}:${last.createdAt}`;
}

export function ChatPanel() {
  const { user } = useAuth();
  const [rooms, setRooms] = useState<ChatRoomDto[]>([]);
  const [messages, setMessages] = useState<ChatMessageDto[]>([]);
  const [selectedRoomId, setSelectedRoomId] = useState<string | null>(null);
  const [draft, setDraft] = useState('');
  const [users, setUsers] = useState<UserListDto[]>([]);
  const [newRoomUserId, setNewRoomUserId] = useState('');
  const [error, setError] = useState('');
  const messagesRef = useRef<HTMLDivElement>(null);
  const stickToBottomRef = useRef(true);
  const prevFingerprintRef = useRef('');
  const selectedRoomIdRef = useRef<string | null>(null);
  const markReadTimerRef = useRef<number | null>(null);

  selectedRoomIdRef.current = selectedRoomId;

  const scrollMessagesToBottom = useCallback((force = false) => {
    const el = messagesRef.current;
    if (!el) return;
    const nearBottom = el.scrollHeight - el.scrollTop - el.clientHeight < 96;
    if (force || stickToBottomRef.current || nearBottom) {
      el.scrollTop = el.scrollHeight;
      stickToBottomRef.current = true;
    }
  }, []);

  const scheduleMarkRead = useCallback((roomId: string) => {
    if (markReadTimerRef.current) window.clearTimeout(markReadTimerRef.current);
    markReadTimerRef.current = window.setTimeout(() => {
      api.markConnectChatRead(roomId).catch(console.error);
    }, 400);
  }, []);

  const loadRooms = useCallback(async () => {
    setError('');
    try {
      const list = await api.getConnectChatRooms();
      setRooms(list);
      setSelectedRoomId((prev) => prev ?? list[0]?.id ?? null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar salas');
    }
  }, []);

  const loadMessages = useCallback(async (roomId: string, options?: { markRead?: boolean; scroll?: boolean }) => {
    try {
      const list = await api.getConnectChatMessages(roomId);
      const fp = messagesFingerprint(list);
      const changed = fp !== prevFingerprintRef.current;
      prevFingerprintRef.current = fp;
      setMessages(list);
      if (options?.scroll !== false && changed) {
        requestAnimationFrame(() => scrollMessagesToBottom(options?.scroll === true));
      }
      if (options?.markRead) scheduleMarkRead(roomId);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar mensagens');
    }
  }, [scheduleMarkRead, scrollMessagesToBottom]);

  useEffect(() => {
    loadRooms().catch(console.error);
    api.getUsers().then(setUsers).catch(console.error);
  }, [loadRooms]);

  useEffect(() => {
    if (!selectedRoomId) return;
    prevFingerprintRef.current = '';
    stickToBottomRef.current = true;
    loadMessages(selectedRoomId, { markRead: true, scroll: true }).catch(console.error);
  }, [selectedRoomId, loadMessages]);

  useEffect(() => {
    return subscribeConnectCommRefresh(() => {
      loadRooms().catch(console.error);
    });
  }, [loadRooms]);

  useEffect(() => {
    return subscribeConnectChatRefresh((roomId) => {
      const active = selectedRoomIdRef.current;
      if (!active || roomId !== active) return;
      loadMessages(active, { markRead: true }).catch(console.error);
    });
  }, [loadMessages]);

  useEffect(() => () => {
    if (markReadTimerRef.current) window.clearTimeout(markReadTimerRef.current);
  }, []);

  async function sendMessage() {
    if (!selectedRoomId || !draft.trim()) return;
    try {
      await api.sendConnectChatMessage(selectedRoomId, { content: draft.trim() });
      setDraft('');
      stickToBottomRef.current = true;
      await loadMessages(selectedRoomId, { markRead: true, scroll: true });
      await loadRooms();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enviar');
    }
  }

  async function startPrivateChat() {
    if (!newRoomUserId) return;
    const other = users.find((u) => u.id === newRoomUserId);
    if (!other) return;
    try {
      const room = await api.createConnectChatRoom({
        name: other.fullName,
        roomType: 'Private',
        sectorId: undefined,
        participantUserIds: [newRoomUserId],
      });
      if (room) {
        setSelectedRoomId(room.id);
        await loadRooms();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao criar conversa');
    }
  }

  return (
    <div className="connect-panel">
      {error ? <p className="text-danger">{error}</p> : null}
      <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '0.75rem', flexWrap: 'wrap' }}>
        <select value={newRoomUserId} onChange={(e) => setNewRoomUserId(e.target.value)}>
          <option value="">Iniciar chat com…</option>
          {users
            .filter((u) => u.isActive && u.id !== user?.userId)
            .map((u) => (
              <option key={u.id} value={u.id}>
                {u.fullName}
              </option>
            ))}
        </select>
        <button type="button" className="btn btn-secondary btn-sm" onClick={startPrivateChat}>
          Nova conversa
        </button>
      </div>
      <div className="connect-chat-layout">
        <div className="connect-chat-rooms">
          {rooms.map((room) => (
            <button
              key={room.id}
              type="button"
              className={`connect-chat-room${selectedRoomId === room.id ? ' active' : ''}`}
              onClick={() => setSelectedRoomId(room.id)}
            >
              <strong>{room.name}</strong>
              {room.unreadCount > 0 ? (
                <span className="feegow-badge-novo" style={{ marginLeft: 6 }}>
                  {room.unreadCount}
                </span>
              ) : null}
              {room.lastMessagePreview ? (
                <div className="text-muted" style={{ fontSize: '0.8rem' }}>
                  {room.lastMessagePreview}
                </div>
              ) : null}
            </button>
          ))}
        </div>
        <div className="connect-chat-main">
          {selectedRoomId ? (
            <>
              <div
                ref={messagesRef}
                className="connect-chat-messages"
                onScroll={() => {
                  const el = messagesRef.current;
                  if (!el) return;
                  stickToBottomRef.current = el.scrollHeight - el.scrollTop - el.clientHeight < 96;
                }}
              >
                {messages.map((m) => (
                  <div
                    key={m.id}
                    className={`connect-chat-bubble${m.senderId === user?.userId ? ' mine' : ''}`}
                  >
                    <div style={{ fontSize: '0.75rem', opacity: 0.8 }}>
                      {m.senderName} · {formatBrDateTime(m.createdAt)}
                    </div>
                    <div>{m.content}</div>
                  </div>
                ))}
              </div>
              <div className="connect-chat-compose">
                <input
                  value={draft}
                  onChange={(e) => setDraft(e.target.value)}
                  placeholder="Digite sua mensagem…"
                  onKeyDown={(e) => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                      e.preventDefault();
                      sendMessage().catch(console.error);
                    }
                  }}
                />
                <button type="button" className="btn btn-primary" onClick={() => sendMessage().catch(console.error)}>
                  Enviar
                </button>
              </div>
            </>
          ) : (
            <p className="text-muted">Selecione ou inicie uma conversa.</p>
          )}
        </div>
      </div>
    </div>
  );
}

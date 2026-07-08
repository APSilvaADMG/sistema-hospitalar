import { useEffect, useState, type FormEvent } from 'react';

import type {

  ConnectTicketListItemDto,

  CreateMailRequest,

  MailAttachmentInputDto,

  MessagePriority,

  PatientDto,

  SusGuideDto,

  TissGuideDto,

  UserListDto,

} from '../../api/client';

import { api } from '../../api/client';



const priorities: MessagePriority[] = ['Baixa', 'Normal', 'Alta', 'Urgente', 'Critica'];

const MAX_ATTACHMENTS = 5;

const MAX_BYTES = 10 * 1024 * 1024;



type Props = {

  open: boolean;

  users: UserListDto[];

  onClose: () => void;

  onSubmit: (payload: CreateMailRequest) => Promise<void>;

};



type PendingAttachment = MailAttachmentInputDto & { id: string };



function readFileAsBase64(file: File): Promise<string> {

  return new Promise((resolve, reject) => {

    const reader = new FileReader();

    reader.onload = () => {

      const result = reader.result as string;

      const base64 = result.includes(',') ? result.split(',')[1] : result;

      resolve(base64);

    };

    reader.onerror = () => reject(reader.error);

    reader.readAsDataURL(file);

  });

}



export function ComposeModal({ open, users, onClose, onSubmit }: Props) {

  const [subject, setSubject] = useState('');

  const [content, setContent] = useState('');

  const [priority, setPriority] = useState<MessagePriority>('Normal');

  const [recipientIds, setRecipientIds] = useState<string[]>([]);

  const [saving, setSaving] = useState(false);

  const [sendNow, setSendNow] = useState(true);

  const [attachError, setAttachError] = useState('');

  const [attachments, setAttachments] = useState<PendingAttachment[]>([]);

  const [patientSearch, setPatientSearch] = useState('');

  const [patients, setPatients] = useState<PatientDto[]>([]);

  const [patientId, setPatientId] = useState('');

  const [guideSearch, setGuideSearch] = useState('');

  const [guides, setGuides] = useState<TissGuideDto[]>([]);

  const [tissGuideId, setTissGuideId] = useState('');

  const [susGuideSearch, setSusGuideSearch] = useState('');

  const [susGuides, setSusGuides] = useState<SusGuideDto[]>([]);

  const [susGuideId, setSusGuideId] = useState('');

  const [ticketSearch, setTicketSearch] = useState('');

  const [tickets, setTickets] = useState<ConnectTicketListItemDto[]>([]);

  const [ticketId, setTicketId] = useState('');



  useEffect(() => {

    if (!open) {

      setSubject('');

      setContent('');

      setPriority('Normal');

      setRecipientIds([]);

      setSendNow(true);

      setAttachments([]);

      setAttachError('');

      setPatientSearch('');

      setPatients([]);

      setPatientId('');

      setGuideSearch('');

      setGuides([]);

      setTissGuideId('');

      setSusGuideSearch('');

      setSusGuides([]);

      setSusGuideId('');

      setTicketSearch('');

      setTickets([]);

      setTicketId('');

    }

  }, [open]);



  useEffect(() => {

    if (!open || patientSearch.trim().length < 2) {

      setPatients([]);

      return;

    }

    const timer = setTimeout(() => {

      api.getPatients(patientSearch.trim(), 1, 20).then((r) => setPatients(r.items)).catch(console.error);

    }, 300);

    return () => clearTimeout(timer);

  }, [open, patientSearch]);



  useEffect(() => {

    if (!open || guideSearch.trim().length < 2) {

      setGuides([]);

      return;

    }

    const timer = setTimeout(() => {

      api.getTissGuides(undefined, undefined, guideSearch.trim()).then(setGuides).catch(console.error);

    }, 300);

    return () => clearTimeout(timer);

  }, [open, guideSearch]);



  useEffect(() => {

    if (!open || susGuideSearch.trim().length < 2) {

      setSusGuides([]);

      return;

    }

    const timer = setTimeout(() => {

      api.getSusGuides({ guideNumber: susGuideSearch.trim(), take: 20 }).then((r) => setSusGuides(r.items)).catch(console.error);

    }, 300);

    return () => clearTimeout(timer);

  }, [open, susGuideSearch]);



  useEffect(() => {

    if (!open || ticketSearch.trim().length < 2) {

      setTickets([]);

      return;

    }

    const timer = setTimeout(() => {

      api.getConnectTickets({ search: ticketSearch.trim() }).then(setTickets).catch(console.error);

    }, 300);

    return () => clearTimeout(timer);

  }, [open, ticketSearch]);



  if (!open) return null;



  async function handleFiles(files: FileList | null) {

    if (!files?.length) return;

    setAttachError('');



    const next = [...attachments];

    for (const file of Array.from(files)) {

      if (next.length >= MAX_ATTACHMENTS) {

        setAttachError(`Máximo de ${MAX_ATTACHMENTS} anexos.`);

        break;

      }

      if (file.size > MAX_BYTES) {

        setAttachError(`"${file.name}" excede 10 MB.`);

        continue;

      }

      const base64 = await readFileAsBase64(file);

      next.push({

        id: crypto.randomUUID(),

        fileName: file.name,

        contentBase64: base64,

        mimeType: file.type || 'application/octet-stream',

        sizeBytes: file.size,

      });

    }

    setAttachments(next);

  }



  async function handleSubmit(e: FormEvent) {

    e.preventDefault();

    if (!subject.trim() || !content.trim()) return;

    setSaving(true);

    try {

      const context =

        patientId || tissGuideId || susGuideId || ticketId

          ? {

              patientId: patientId || undefined,

              tissGuideId: tissGuideId || undefined,

              susGuideId: susGuideId || undefined,

              ticketId: ticketId || undefined,

            }

          : undefined;



      await onSubmit({

        subject: subject.trim(),

        content: content.trim(),

        priority,

        recipients: recipientIds.map((userId) => ({ userId, type: 'To' as const })),

        attachments: attachments.map(({ fileName, contentBase64, mimeType, sizeBytes }) => ({

          fileName,

          contentBase64,

          mimeType,

          sizeBytes,

        })),

        sendNow,

        context,

      });

      onClose();

    } finally {

      setSaving(false);

    }

  }



  return (

    <div className="connect-modal-backdrop" onClick={onClose}>

      <div className="connect-modal" onClick={(e) => e.stopPropagation()}>

        <h3 style={{ marginTop: 0 }}>Nova mensagem</h3>

        <form onSubmit={handleSubmit}>

          <label>

            Assunto

            <input value={subject} onChange={(e) => setSubject(e.target.value)} required />

          </label>

          <label>

            Destinatários

            <select

              multiple

              size={5}

              value={recipientIds}

              onChange={(e) => setRecipientIds(Array.from(e.target.selectedOptions, (o) => o.value))}

            >

              {users.filter((u) => u.isActive).map((u) => (

                <option key={u.id} value={u.id}>

                  {u.fullName} ({u.email})

                </option>

              ))}

            </select>

          </label>

          <label>

            Paciente (opcional)

            <input placeholder="Buscar por nome…" value={patientSearch} onChange={(e) => setPatientSearch(e.target.value)} />

            <select value={patientId} onChange={(e) => setPatientId(e.target.value)}>

              <option value="">— Nenhum —</option>

              {patients.map((p) => (

                <option key={p.id} value={p.id}>{p.fullName} — {p.cpf}</option>

              ))}

            </select>

          </label>

          <label>

            Guia TISS (opcional)

            <input placeholder="Número da guia…" value={guideSearch} onChange={(e) => setGuideSearch(e.target.value)} />

            <select value={tissGuideId} onChange={(e) => setTissGuideId(e.target.value)}>

              <option value="">— Nenhuma —</option>

              {guides.map((g) => (

                <option key={g.id} value={g.id}>{g.guideNumber} — {g.patientName}</option>

              ))}

            </select>

          </label>

          <label>

            Guia SUS (opcional)

            <input placeholder="Número da guia SUS…" value={susGuideSearch} onChange={(e) => setSusGuideSearch(e.target.value)} />

            <select value={susGuideId} onChange={(e) => setSusGuideId(e.target.value)}>

              <option value="">— Nenhuma —</option>

              {susGuides.map((g) => (

                <option key={g.id} value={g.id}>{g.guideNumber} — {g.patientName}</option>

              ))}

            </select>

          </label>

          <label>

            Chamado (opcional)

            <input placeholder="Protocolo ou título…" value={ticketSearch} onChange={(e) => setTicketSearch(e.target.value)} />

            <select value={ticketId} onChange={(e) => setTicketId(e.target.value)}>

              <option value="">— Nenhum —</option>

              {tickets.map((t) => (

                <option key={t.id} value={t.id}>{t.protocolo} — {t.titulo}</option>

              ))}

            </select>

          </label>

          <label>

            Prioridade

            <select value={priority} onChange={(e) => setPriority(e.target.value as MessagePriority)}>

              {priorities.map((p) => (

                <option key={p} value={p}>{p === 'Critica' ? 'Crítica' : p}</option>

              ))}

            </select>

          </label>

          <label>

            Conteúdo

            <textarea rows={6} value={content} onChange={(e) => setContent(e.target.value)} required />

          </label>

          <label>

            Anexos (máx. {MAX_ATTACHMENTS}, 10 MB cada)

            <input

              type="file"

              multiple

              onChange={(e) => handleFiles(e.target.files).catch(console.error)}

            />

          </label>

          {attachError ? <p className="text-danger" style={{ fontSize: '0.85rem' }}>{attachError}</p> : null}

          {attachments.length > 0 ? (

            <ul style={{ margin: '0.25rem 0', paddingLeft: '1.2rem', fontSize: '0.9rem' }}>

              {attachments.map((a) => (

                <li key={a.id} style={{ display: 'flex', gap: '0.5rem', alignItems: 'center' }}>

                  {a.fileName} ({(a.sizeBytes / 1024).toFixed(0)} KB)

                  <button

                    type="button"

                    className="btn btn-secondary btn-sm"

                    onClick={() => setAttachments(attachments.filter((x) => x.id !== a.id))}

                  >

                    Remover

                  </button>

                </li>

              ))}

            </ul>

          ) : null}

          <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>

            <input type="checkbox" checked={sendNow} onChange={(e) => setSendNow(e.target.checked)} />

            Enviar imediatamente (desmarque para salvar rascunho)

          </label>

          <p className="text-muted" style={{ fontSize: '0.85rem' }}>

            Vincular paciente, guia ou chamado permite rastrear a comunicação no PEP, faturamento e suporte.

          </p>

          <div className="connect-modal-actions">

            <button type="button" className="btn btn-secondary" onClick={onClose}>Cancelar</button>

            <button type="submit" className="btn btn-primary" disabled={saving}>

              {saving ? 'Salvando…' : sendNow ? 'Enviar' : 'Salvar rascunho'}

            </button>

          </div>

        </form>

      </div>

    </div>

  );

}



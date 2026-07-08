export type AgendaBlockSlot = {
  id: string;
  startTime: string;
  endTime: string;
  reason: string;
};

function storageKey(professionalId: string, date: string) {
  return `iasgh-agenda-blocks:${professionalId || 'all'}:${date}`;
}

export function loadAgendaBlocks(professionalId: string, date: string): AgendaBlockSlot[] {
  try {
    const raw = localStorage.getItem(storageKey(professionalId, date));
    if (!raw) return [];
    const parsed = JSON.parse(raw) as AgendaBlockSlot[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

export function saveAgendaBlocks(professionalId: string, date: string, blocks: AgendaBlockSlot[]) {
  localStorage.setItem(storageKey(professionalId, date), JSON.stringify(blocks));
}

export function addAgendaBlock(
  professionalId: string,
  date: string,
  block: Omit<AgendaBlockSlot, 'id'>,
): AgendaBlockSlot[] {
  const next: AgendaBlockSlot = {
    ...block,
    id: crypto.randomUUID(),
  };
  const blocks = [...loadAgendaBlocks(professionalId, date), next];
  saveAgendaBlocks(professionalId, date, blocks);
  return blocks;
}

function timeToMinutes(time: string) {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + m;
}

export function isSlotBlocked(blocks: AgendaBlockSlot[], slotTime: string): AgendaBlockSlot | null {
  const slot = timeToMinutes(slotTime);
  for (const block of blocks) {
    const start = timeToMinutes(block.startTime);
    const end = timeToMinutes(block.endTime);
    if (slot >= start && slot < end) return block;
  }
  return null;
}

export function overlapsBlock(blocks: AgendaBlockSlot[], startTime: string, endTime: string): boolean {
  const start = timeToMinutes(startTime);
  const end = timeToMinutes(endTime);
  return blocks.some((block) => {
    const bStart = timeToMinutes(block.startTime);
    const bEnd = timeToMinutes(block.endTime);
    return start < bEnd && end > bStart;
  });
}

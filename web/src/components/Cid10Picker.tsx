import { useCallback, useEffect, useState } from 'react';
import { api, type Cid10CatalogItemDto } from '../api/client';

type Props = {
  value: string;
  onChange: (code: string, description?: string) => void;
  onSuggestFromText?: () => string | undefined;
};

export function Cid10Picker({ value, onChange, onSuggestFromText }: Props) {
  const [search, setSearch] = useState('');
  const [items, setItems] = useState<Cid10CatalogItemDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [open, setOpen] = useState(false);
  const [selectedLabel, setSelectedLabel] = useState('');
  const [parentCode, setParentCode] = useState<string | undefined>(undefined);
  const [parentStack, setParentStack] = useState<Cid10CatalogItemDto[]>([]);

  const load = useCallback(async (term: string, parent?: string) => {
    setLoading(true);
    try {
      const list = term.trim()
        ? await api.getCid10Catalog(term)
        : await api.getCid10Children(parent);
      setItems(list);
    } catch {
      setItems([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!open) return;
    const timer = window.setTimeout(() => void load(search, parentCode), 250);
    return () => window.clearTimeout(timer);
  }, [search, open, load, parentCode]);

  useEffect(() => {
    if (!value) {
      setSelectedLabel('');
      return;
    }
    void api.getCid10Catalog(value).then((list) => {
      const match = list.find((c) => c.code.toLowerCase() === value.toLowerCase());
      setSelectedLabel(match ? `${match.code} — ${match.description}` : value);
    }).catch(() => setSelectedLabel(value));
  }, [value]);

  function select(item: Cid10CatalogItemDto) {
    onChange(item.code, item.description);
    setSelectedLabel(`${item.code} — ${item.description}`);
    setOpen(false);
    setSearch('');
    setParentCode(undefined);
    setParentStack([]);
  }

  async function drillDown(item: Cid10CatalogItemDto) {
    setParentStack((prev) => [...prev, item]);
    setParentCode(item.code);
    setSearch('');
    await load('', item.code);
  }

  function navigateUp() {
    const nextStack = parentStack.slice(0, -1);
    const nextParent = nextStack.length > 0 ? nextStack[nextStack.length - 1].code : undefined;
    setParentStack(nextStack);
    setParentCode(nextParent);
    void load('', nextParent);
  }

  async function handleSuggest() {
    const text = onSuggestFromText?.();
    if (!text?.trim()) return;
    try {
      const suggestions = await api.suggestCid10({ text });
      if (suggestions.length > 0) {
        const top = suggestions[0];
        onChange(top.code, top.description);
        setSelectedLabel(`${top.code} — ${top.description}`);
      }
    } catch {
      /* ignore */
    }
  }

  const grouped = search.trim()
    ? items.reduce<Record<string, Cid10CatalogItemDto[]>>((acc, item) => {
        const key = item.category ?? 'Resultados';
        (acc[key] ??= []).push(item);
        return acc;
      }, {})
    : null;

  return (
    <div className="pep-cid10-picker">
      {value && (
        <div className="pep-cid10-selected">
          <span>{selectedLabel || value}</span>
          <button type="button" className="pep-cid10-clear" onClick={() => onChange('')} aria-label="Remover CID">
            ×
          </button>
        </div>
      )}
      <div className="pep-cid10-search-row">
        <input
          type="text"
          placeholder="Buscar CID-10 por código ou descrição..."
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setParentCode(undefined);
            setParentStack([]);
            setOpen(true);
          }}
          onFocus={() => {
            setOpen(true);
            if (items.length === 0) void load(search, parentCode);
          }}
        />
        {onSuggestFromText && (
          <button type="button" className="btn btn-secondary btn-sm" onClick={() => void handleSuggest()}>
            Sugerir por texto
          </button>
        )}
      </div>
      <div className="pep-cid10-manual">
        <label>Código manual (opcional)</label>
        <input
          type="text"
          placeholder="Ex: J18.9"
          value={value}
          onChange={(e) => onChange(e.target.value.toUpperCase())}
        />
      </div>
      {open && (
        <div className="pep-cid10-dropdown">
          {!search.trim() && parentStack.length > 0 ? (
            <button type="button" className="pep-cid10-back" onClick={navigateUp}>
              ← Voltar para {parentStack.length > 1 ? parentStack[parentStack.length - 2].code : 'capítulos'}
            </button>
          ) : null}
          {loading && <p className="pep-cid10-hint">Carregando...</p>}
          {!loading && items.length === 0 && <p className="pep-cid10-hint">Nenhum CID encontrado.</p>}
          {!loading && grouped && Object.entries(grouped).map(([category, list]) => (
            <div key={category} className="pep-cid10-group">
              <div className="pep-cid10-group-title">{category}</div>
              {list.map((item) => (
                <button
                  key={item.code}
                  type="button"
                  className={`pep-cid10-option${value === item.code ? ' active' : ''}`}
                  onClick={() => select(item)}
                >
                  <strong>{item.code}</strong>
                  <span>{item.description}</span>
                </button>
              ))}
            </div>
          ))}
          {!loading && !search.trim() && !grouped && items.map((item) => (
            <div key={item.code} className="pep-cid10-option-row">
              <button
                type="button"
                className={`pep-cid10-option${value === item.code ? ' active' : ''}`}
                onClick={() => select(item)}
              >
                <strong>{item.code}</strong>
                <span>{item.description}</span>
              </button>
              <button
                type="button"
                className="btn btn-secondary btn-sm pep-cid10-drill"
                onClick={() => void drillDown(item)}
                title="Ver subcategorias"
              >
                ›
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

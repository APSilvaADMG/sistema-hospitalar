import { useEffect, useMemo, useState, type FormEvent } from 'react';
import {
  api,
  type AnvisaBulaSummary,
  type AnvisaMedicationDetailDto,
  type BularioMedicationListItemDto,
  type MedicationCatalogDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { PageHeader } from '../components/PageHeader';
import { extractPosologiaFromPackageInsert, formatMedicationDisplayName, inferFormFromText, inferRouteFromForm, inferStrengthFromName, parseBulaSections } from '../utils/bulaFormat';

function BulaSection({ text }: { text: string }) {
  const sections = useMemo(() => parseBulaSections(text), [text]);

  if (sections.length === 0) {
    return <p className="bula-empty">Bula não disponível para este item.</p>;
  }

  return (
    <div className="bula-content">
      {sections.map((section) => (
        <section key={section.title} className="bula-section">
          <h4>{section.title}</h4>
          <p>{section.body}</p>
        </section>
      ))}
    </div>
  );
}

const MIN_SEARCH_LENGTH = 2;

export function MedicationsBulaPage() {
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [page, setPage] = useState(1);
  const [items, setItems] = useState<BularioMedicationListItemDto[]>([]);
  const [anvisaItems, setAnvisaItems] = useState<AnvisaBulaSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [catalogTotal, setCatalogTotal] = useState(0);
  const [anvisaAvailable, setAnvisaAvailable] = useState(false);
  const [selectedLocal, setSelectedLocal] = useState<MedicationCatalogDto | null>(null);
  const [selectedAnvisa, setSelectedAnvisa] = useState<{
    summary: AnvisaBulaSummary;
    detail?: AnvisaMedicationDetailDto;
  } | null>(null);
  const [selectionMode, setSelectionMode] = useState<'local' | 'anvisa' | null>(null);
  const [loading, setLoading] = useState(false);
  const [loadingDetail, setLoadingDetail] = useState(false);
  const [error, setError] = useState('');
  const [info, setInfo] = useState('');

  useEffect(() => {
    api.getBularioStats().then((stats) => {
      setCatalogTotal(stats.withPackageInsert);
      setAnvisaAvailable(stats.anvisaAvailable);
      if (!stats.anvisaAvailable) {
        setInfo('ANVISA bloqueada externamente. Bulário operando pelo catálogo importado.');
      }
    }).catch(console.error);
  }, []);

  async function load(term?: string, nextPage = 1) {
    const normalized = term?.trim() ?? '';
    if (normalized.length > 0 && normalized.length < MIN_SEARCH_LENGTH) {
      setItems([]);
      setAnvisaItems([]);
      setTotalCount(0);
      setTotalPages(0);
      setSelectedLocal(null);
      setSelectedAnvisa(null);
      setSelectionMode(null);
      return;
    }

    setLoading(true);
    setError('');
    if (normalized) setInfo('');

    try {
      const result = await api.searchBulario(normalized || undefined, nextPage);
      setItems(result.items);
      setTotalCount(result.totalCount);
      setTotalPages(result.totalPages);
      setPage(result.page);
      setCatalogTotal(result.catalogTotal);
      setAnvisaAvailable(result.anvisaAvailable);
      setAnvisaItems(result.anvisa?.content ?? []);

      if (result.items.length > 0) {
        setSelectionMode('local');
        await selectLocalItem(result.items[0]);
      } else {
        setSelectedLocal(null);
        setSelectionMode(null);
      }

      if (!result.anvisaAvailable && normalized) {
        setInfo('ANVISA indisponível. Resultados do catálogo importado (Consulta Remédios + hospital).');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar medicamentos.');
    } finally {
      setLoading(false);
    }
  }

  async function selectLocalItem(item: BularioMedicationListItemDto) {
    setSelectionMode('local');
    setSelectedAnvisa(null);
    setError('');
    try {
      setLoadingDetail(true);
      const detail = await api.getMedication(item.id);
      setSelectedLocal(detail);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar bula.');
    } finally {
      setLoadingDetail(false);
    }
  }

  async function selectAnvisaItem(summary: AnvisaBulaSummary) {
    setSelectionMode('anvisa');
    setSelectedLocal(null);
    setSelectedAnvisa({ summary });
    setError('');
    try {
      setLoadingDetail(true);
      const detail = await api.getAnvisaMedication(summary.numProcesso);
      setSelectedAnvisa({ summary, detail });
    } catch {
      // mantém resumo
    } finally {
      setLoadingDetail(false);
    }
  }

  async function handleSearch(event: FormEvent) {
    event.preventDefault();
    setQuery(search);
    setPage(1);
    await load(search || undefined, 1);
  }

  async function openAnvisaPdf(bulaId?: string) {
    if (!bulaId) return;
    try {
      const blob = await api.getAnvisaPdfBlob(bulaId);
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank', 'noopener,noreferrer');
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao abrir PDF da bula.');
    }
  }

  async function changePage(nextPage: number) {
    if (nextPage < 1 || (totalPages > 0 && nextPage > totalPages)) return;
    await load(query || undefined, nextPage);
  }

  const withBula = items.filter((m) => m.hasPackageInsert).length;
  const showSearchHint = !query && totalCount === 0 && !loading;
  const displayDosage =
    selectedLocal?.defaultDosage
    ?? extractPosologiaFromPackageInsert(selectedLocal?.packageInsert);
  const displayForm =
    selectedLocal?.pharmaceuticalForm
    ?? inferFormFromText(`${selectedLocal?.name ?? ''}\n${selectedLocal?.packageInsert ?? ''}`);
  const displayStrength =
    selectedLocal?.strength
    ?? inferStrengthFromName(selectedLocal?.name);
  const displayRoute =
    selectedLocal?.route
    ?? inferRouteFromForm(displayForm, selectedLocal?.packageInsert);
  const displayName = formatMedicationDisplayName(selectedLocal?.name, displayStrength);

  return (
    <>
      <PageHeader
        eyebrow="Diagnóstico"
        title="Bulário"
        subtitle="Catálogo com milhares de bulas importadas. ANVISA é consultada quando disponível."
      />

      {error && <div className="alert alert-error">{error}</div>}
      {info && <div className="alert alert-info">{info}</div>}

      <div className="kpi-grid">
        <KpiCard label="Catálogo" value={catalogTotal} variant="primary" />
        <KpiCard label="Resultados" value={totalCount} variant="info" />
        <KpiCard label="Com bula" value={withBula} variant="success" />
        <KpiCard label="ANVISA" value={anvisaAvailable ? 'Online' : 'Offline'} variant={anvisaAvailable ? 'success' : 'warning'} />
      </div>

      <div className="card-panel appt-panel">
        <FilterBar onSubmit={handleSearch} actions={<button className="btn" type="submit">Buscar</button>}>
          <div className="filter-field grow-lg">
            <label htmlFor="med-search">Pesquisar</label>
            <input
              id="med-search"
              placeholder="Nome do medicamento ou princípio ativo (mín. 2 letras)..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>

        <div className="bula-layout">
          <div className="bula-list card-panel-body" style={{ padding: 0 }}>
            {loading && <p className="bula-empty">Carregando...</p>}
            {!loading && showSearchHint && (
              <p className="bula-empty">
                Digite pelo menos {MIN_SEARCH_LENGTH} letras para buscar entre {catalogTotal.toLocaleString('pt-BR')} bulas.
              </p>
            )}
            {!loading && !showSearchHint && items.length === 0 && anvisaItems.length === 0 && (
              <p className="bula-empty">Nenhum medicamento encontrado.</p>
            )}

            {items.length > 0 && (
              <>
                <p className="bula-list-heading">Catálogo ({totalCount.toLocaleString('pt-BR')})</p>
                <ul className="bula-med-list">
                  {items.map((med) => (
                    <li key={med.id}>
                      <button
                        type="button"
                        className={`bula-med-item${selectionMode === 'local' && selectedLocal?.id === med.id ? ' active' : ''}`}
                        onClick={() => selectLocalItem(med)}
                      >
                        <strong>{med.name}</strong>
                        <span className="bula-source-tag">
                          {med.source === 'consulta-remedios' ? 'Consulta Remédios' : 'Hospital'}
                        </span>
                        {med.activeIngredient ? <span>{med.activeIngredient}</span> : null}
                      </button>
                    </li>
                  ))}
                </ul>
              </>
            )}

            {anvisaItems.length > 0 && (
              <>
                <p className="bula-list-heading">ANVISA ({anvisaItems.length})</p>
                <ul className="bula-med-list">
                  {anvisaItems.map((med) => (
                    <li key={med.numProcesso}>
                      <button
                        type="button"
                        className={`bula-med-item${selectionMode === 'anvisa' && selectedAnvisa?.summary.numProcesso === med.numProcesso ? ' active' : ''}`}
                        onClick={() => selectAnvisaItem(med)}
                      >
                        <strong>{med.nomeProduto}</strong>
                        <span className="bula-source-tag">ANVISA</span>
                        {med.razaoSocial && <span>{med.razaoSocial}</span>}
                      </button>
                    </li>
                  ))}
                </ul>
              </>
            )}

            {totalPages > 1 && (
              <div className="pagination-bar" style={{ padding: '12px 16px', display: 'flex', gap: 8, alignItems: 'center' }}>
                <button type="button" className="btn btn-sm" disabled={page <= 1 || loading} onClick={() => changePage(page - 1)}>
                  Anterior
                </button>
                <span>Página {page} de {totalPages}</span>
                <button type="button" className="btn btn-sm" disabled={page >= totalPages || loading} onClick={() => changePage(page + 1)}>
                  Próxima
                </button>
              </div>
            )}
          </div>

          <div className="bula-detail card-panel">
            {loadingDetail && (
              <div className="card-panel-body bula-empty">Carregando detalhes...</div>
            )}
            {!loadingDetail && selectionMode === 'local' && selectedLocal && (
              <div className="card-panel-body">
                <h3>{displayName}</h3>
                <div className="bula-meta">
                {selectedLocal.activeIngredient ? (
                  <span><strong>Princípio ativo:</strong> {selectedLocal.activeIngredient}</span>
                ) : null}
                {displayForm ? (
                  <span><strong>Forma:</strong> {displayForm}</span>
                ) : null}
                {displayStrength ? (
                  <span><strong>Concentração:</strong> {displayStrength}</span>
                ) : null}
                {displayRoute ? (
                  <span><strong>Via:</strong> {displayRoute}</span>
                ) : null}
                  {selectedLocal.stockAvailable != null && (
                    <span><strong>Estoque:</strong> {selectedLocal.stockAvailable}</span>
                  )}
                </div>
                {displayDosage && (
                  <div className="bula-highlight">
                    <strong>Posologia padrão:</strong> {displayDosage}
                  </div>
                )}
                {selectedLocal.packageInsert ? (
                  <BulaSection text={selectedLocal.packageInsert} />
                ) : (
                  <p className="bula-empty">Bula não disponível para este item.</p>
                )}
              </div>
            )}
            {!loadingDetail && selectionMode === 'anvisa' && selectedAnvisa && (
              <AnvisaDetailPanel
                summary={selectedAnvisa.summary}
                detail={selectedAnvisa.detail}
                onOpenPdf={openAnvisaPdf}
              />
            )}
            {!loadingDetail && !selectionMode && (
              <div className="card-panel-body bula-empty">Selecione um medicamento na lista.</div>
            )}
          </div>
        </div>
      </div>
    </>
  );
}

function AnvisaDetailPanel({
  summary,
  detail,
  onOpenPdf,
}: {
  summary: AnvisaBulaSummary;
  detail?: AnvisaMedicationDetailDto;
  onOpenPdf: (id?: string) => void;
}) {
  const data = detail ?? summary;
  const patientId = data.idBulaPacienteProtegido ?? summary.idBulaPacienteProtegido;
  const professionalId = data.idBulaProfissionalProtegido ?? summary.idBulaProfissionalProtegido;

  return (
    <div className="card-panel-body">
      <h3>{data.nomeProduto ?? summary.nomeProduto}</h3>
      <div className="bula-meta">
        {data.numProcesso && <span><strong>Processo ANVISA:</strong> {String(data.numProcesso)}</span>}
        {data.numeroRegistro && <span><strong>Registro:</strong> {String(data.numeroRegistro)}</span>}
        {data.razaoSocial && <span><strong>Laboratório:</strong> {String(data.razaoSocial)}</span>}
        {data.principioAtivo && <span><strong>Princípio ativo:</strong> {String(data.principioAtivo)}</span>}
      </div>
      <div className="bula-highlight" style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
        {patientId && (
          <button type="button" className="btn btn-sm" onClick={() => onOpenPdf(patientId)}>
            Bula do paciente (PDF)
          </button>
        )}
        {professionalId && (
          <button type="button" className="btn btn-sm btn-secondary" onClick={() => onOpenPdf(professionalId)}>
            Bula do profissional (PDF)
          </button>
        )}
      </div>
    </div>
  );
}

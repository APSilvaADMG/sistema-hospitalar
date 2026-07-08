import { useState } from 'react';
import type { ProfessionalDto } from '../../../api/client';

type Props = {
  professionals: ProfessionalDto[];
};

function addDays(dateStr: string, days: number) {
  const d = new Date(`${dateStr}T12:00:00`);
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

export function FeegowAgendaMap({ professionals }: Props) {
  const today = new Date().toISOString().slice(0, 10);
  const [specialtyId, setSpecialtyId] = useState('');
  const [unit, setUnit] = useState('all');
  const [dateFrom, setDateFrom] = useState(today);
  const [dateTo, setDateTo] = useState(addDays(today, 7));
  const [searched, setSearched] = useState(false);

  const specialties = Array.from(
    new Map(professionals.map((p) => [p.specialtyId, p.specialtyName])).entries(),
  ).map(([id, name]) => ({ id, name }));

  return (
    <div className="feegow-agenda-map">
      <header className="feegow-map-head">
        <h1 className="feegow-agenda-schedule-title">
          Mapa de agenda
          <span className="feegow-map-crumb">
            <span className="feegow-map-grid-icon" aria-hidden>▦</span>
            <span className="feegow-crumb-sep">/</span>
            ocupação das grades na unidade
          </span>
        </h1>
      </header>

      <div className="feegow-map-filters-card">
        <div className="feegow-map-filters-head">
          <span className="feegow-filter-icon" aria-hidden>☰</span>
          <strong>Filtros</strong>
          <div className="feegow-map-export-btns">
            <button type="button" className="feegow-map-export feegow-map-print" title="Imprimir" onClick={() => window.print()}>🖨</button>
            <button type="button" className="feegow-map-export feegow-map-excel" title="Exportar">📊</button>
          </div>
        </div>

        <div className="feegow-map-filters-row">
          <label className="feegow-field">
            <span>Especialidades</span>
            <select id="map-specialty" value={specialtyId} onChange={(e) => { setSpecialtyId(e.target.value); setSearched(false); }}>
              <option value="">Selecione</option>
              {specialties.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </label>
          <label className="feegow-field">
            <span>Unidades</span>
            <select id="map-unit" value={unit} onChange={(e) => setUnit(e.target.value)}>
              <option value="all">Todos selecionados</option>
              <option value="iasgh">IASGH</option>
            </select>
          </label>
          <label className="feegow-field">
            <span>Data Início</span>
            <input id="map-from" type="date" value={dateFrom} onChange={(e) => setDateFrom(e.target.value)} />
          </label>
          <label className="feegow-field">
            <span>Data Fim</span>
            <input id="map-to" type="date" value={dateTo} onChange={(e) => setDateTo(e.target.value)} />
          </label>
          <button type="button" className="feegow-map-search" onClick={() => setSearched(true)}>Buscar</button>
        </div>

        <div className="feegow-map-legend">
          <div className="feegow-map-legend-col">
            <span><i className="swatch swatch-cyan" />Até 25%</span>
            <span><i className="swatch swatch-green" />Até 50%</span>
            <span><i className="swatch swatch-purple" />Até 75%</span>
            <span><i className="swatch swatch-pink" />Até 100%</span>
          </div>
          <div className="feegow-map-legend-col">
            <span><i className="swatch swatch-gray" />Vazia</span>
            <span><i className="swatch swatch-red" />Lotada</span>
            <span><i className="swatch swatch-black" />Não atende</span>
            <span><i className="swatch swatch-blue" />* Exceção</span>
          </div>
          <div className="feegow-map-legend-col">
            <span><i className="swatch swatch-orange" />Encaixe</span>
          </div>
        </div>
      </div>

      <div className="feegow-map-body">
        {!searched || !specialtyId ? (
          <p className="feegow-agenda-empty-hint">Selecione acima as especialidades que deseja visualizar.</p>
        ) : (
          <p className="feegow-agenda-empty-hint">
            Nenhum dado de ocupação disponível para o período {dateFrom} — {dateTo}.
          </p>
        )}
      </div>
    </div>
  );
}

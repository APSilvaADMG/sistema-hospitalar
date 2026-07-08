import { type ReactNode } from 'react';

type FilterField = {
  id: string;
  label: string;
  children: ReactNode;
};

type Props = {
  fields: FilterField[];
  onFilter: () => void;
  filterLabel?: string;
};

export function FeegowAgendaFilterSidebar({ fields, onFilter, filterLabel = 'FILTRAR' }: Props) {
  return (
    <div className="feegow-agenda-sidebar feegow-agenda-filter-sidebar">
      <div className="feegow-filter-head">
        <span className="feegow-filter-icon" aria-hidden>▾</span>
        <strong>FILTROS</strong>
      </div>

      {fields.map((field) => (
        <div key={field.id} className="feegow-field">
          <span>{field.label}</span>
          {field.children}
        </div>
      ))}

      <button type="button" className="feegow-filter-submit" onClick={onFilter}>
        <span aria-hidden>🔍</span>
        {filterLabel}
      </button>
    </div>
  );
}

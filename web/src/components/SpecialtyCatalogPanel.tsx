import { useMemo } from 'react';
import type { ImagingProcedureDto, LabExamCatalogDto, MedicationCatalogDto } from '../api/client';
import { imagingModalityLabels } from '../api/client';

type SpecialtyCatalogPanelProps = {
  specialtyName?: string;
  labExams: LabExamCatalogDto[];
  imagingProcedures: ImagingProcedureDto[];
  medications: MedicationCatalogDto[];
  selectedLabIds?: string[];
  onLabToggle?: (id: string) => void;
  selectedImagingId?: string;
  onImagingSelect?: (id: string, label: string) => void;
  selectedMedIds?: string[];
  onMedToggle?: (id: string, label: string) => void;
  showLabs?: boolean;
  showImaging?: boolean;
  showMeds?: boolean;
};

function groupByCategory<T extends { category?: string; name: string }>(items: T[]) {
  const map = new Map<string, T[]>();
  for (const item of items) {
    const key = item.category ?? 'Outros';
    const list = map.get(key) ?? [];
    list.push(item);
    map.set(key, list);
  }
  return [...map.entries()].sort(([a], [b]) => a.localeCompare(b));
}

export function SpecialtyCatalogPanel({
  specialtyName,
  labExams,
  imagingProcedures,
  medications,
  selectedLabIds = [],
  onLabToggle,
  selectedImagingId,
  onImagingSelect,
  selectedMedIds = [],
  onMedToggle,
  showLabs = true,
  showImaging = true,
  showMeds = true,
}: SpecialtyCatalogPanelProps) {
  const labGroups = useMemo(() => groupByCategory(labExams), [labExams]);

  if (!specialtyName && labExams.length === 0 && imagingProcedures.length === 0 && medications.length === 0) {
    return (
      <div className="catalog-empty">
        Selecione o médico solicitante para ver exames e medicamentos da especialidade.
      </div>
    );
  }

  return (
    <div className="specialty-catalog">
      {specialtyName && (
        <div className="catalog-specialty-badge">
          Catálogo: <strong>{specialtyName}</strong>
          <span className="catalog-counts">
            {labExams.length} lab · {imagingProcedures.length} imagem · {medications.length} meds
          </span>
        </div>
      )}

      {showLabs && labExams.length > 0 && (
        <div className="catalog-section">
          <h4>Exames laboratoriais</h4>
          {labGroups.map(([category, items]) => (
            <div key={category} className="catalog-group">
              <div className="catalog-group-title">{category}</div>
              <div className="exam-grid">
                {items.map((exam) => (
                  <label key={exam.id} className={`exam-chip${selectedLabIds.includes(exam.id) ? ' selected' : ''}`}>
                    <input
                      type="checkbox"
                      checked={selectedLabIds.includes(exam.id)}
                      onChange={() => onLabToggle?.(exam.id)}
                    />
                    {exam.name}
                    {exam.tussCode && <small>({exam.tussCode})</small>}
                  </label>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}

      {showImaging && imagingProcedures.length > 0 && (
        <div className="catalog-section">
          <h4>Exames de imagem</h4>
          <div className="exam-grid">
            {imagingProcedures.map((proc) => (
              <label key={proc.id} className={`exam-chip${selectedImagingId === proc.id ? ' selected' : ''}`}>
                <input
                  type="radio"
                  name="imaging-proc"
                  checked={selectedImagingId === proc.id}
                  onChange={() => onImagingSelect?.(proc.id, proc.name)}
                />
                {proc.name}
                <small>{imagingModalityLabels[proc.modality]}</small>
              </label>
            ))}
          </div>
        </div>
      )}

      {showMeds && medications.length > 0 && (
        <div className="catalog-section">
          <h4>Medicamentos</h4>
          <div className="exam-grid">
            {medications.map((med) => (
              <label key={med.id} className={`exam-chip${selectedMedIds.includes(med.id) ? ' selected' : ''}`}>
                <input
                  type="checkbox"
                  checked={selectedMedIds.includes(med.id)}
                  onChange={() => onMedToggle?.(med.id, med.name)}
                />
                <span>
                  <strong>{med.name}</strong>
                  {med.defaultDosage && <small>{med.defaultDosage}</small>}
                  {med.stockAvailable != null && <small>Estoque: {med.stockAvailable}</small>}
                </span>
              </label>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

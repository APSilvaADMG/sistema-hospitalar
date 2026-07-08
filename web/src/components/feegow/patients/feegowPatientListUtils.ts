import type { PatientDto } from '../../../api/client';

export function genderLabelFeegow(gender: number): string {
  if (gender === 1) return 'Masculino';
  if (gender === 2) return 'Feminino';
  if (gender === 3) return 'Outro';
  return '';
}

export function filterFeegowPatientList(
  patients: PatientDto[],
  filter: 'active' | 'inactive' | 'chart-search',
  chartSearch: string,
  chartNumbers: Map<string, string>,
): PatientDto[] {
  let rows = patients;

  if (filter === 'inactive') {
    rows = rows.filter((p) => !p.isActive);
  } else {
    rows = rows.filter((p) => p.isActive);
  }

  if (filter === 'chart-search' && chartSearch.trim()) {
    const term = chartSearch.trim();
    rows = rows.filter((p) => (chartNumbers.get(p.id) ?? '').includes(term));
  }

  return [...rows].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );
}

export function buildChartNumberMap(patients: PatientDto[]): Map<string, string> {
  const sorted = [...patients].sort(
    (a, b) => new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime(),
  );
  const map = new Map<string, string>();
  sorted.forEach((p, i) => map.set(p.id, String(i + 1)));
  return map;
}

import type { AdministrationRouteDto, MedicationCatalogDto } from '../api/client';

export type PrescriptionLineItem = {
  medicationId: string;
  medicationName: string;
  administrationRouteCode: string;
  dosage: string;
  frequency: string;
  notes: string;
};

export function resolveDefaultRouteCode(
  medication: Pick<MedicationCatalogDto, 'route'>,
  routes: AdministrationRouteDto[],
): string {
  const hint = medication.route?.trim().toUpperCase();
  if (!hint) return '';
  const byAbbr = routes.find((r) => r.abbreviation?.toUpperCase() === hint);
  if (byAbbr) return byAbbr.code;
  const byName = routes.find((r) => r.name.toLowerCase().includes(hint.toLowerCase()));
  return byName?.code ?? '';
}

export function formatRouteLabel(route: AdministrationRouteDto): string {
  return route.abbreviation ? `${route.name} (${route.abbreviation})` : route.name;
}

export function findAdministrationRoute(
  routes: AdministrationRouteDto[],
  code: string,
): AdministrationRouteDto | undefined {
  return routes.find((r) => r.code === code);
}

export function buildPrescriptionBlock(
  items: PrescriptionLineItem[],
  routes: AdministrationRouteDto[],
): string {
  if (items.length === 0) return '';
  const lines = items.map((item) => {
    const route = findAdministrationRoute(routes, item.administrationRouteCode);
    const routePart = route
      ? route.abbreviation ?? route.name
      : item.administrationRouteCode || 'via não informada';
    const dosage = item.dosage.trim() || 'conforme orientação';
    const freq = item.frequency.trim();
    const notes = item.notes.trim();
    const parts = [`- ${item.medicationName}: ${dosage}`, `via ${routePart}`];
    if (freq) parts.push(freq);
    if (notes) parts.push(`(${notes})`);
    return parts.join(', ');
  });
  return `Prescrição:\n${lines.join('\n')}`;
}

export function emptyPrescriptionLine(
  medication: MedicationCatalogDto,
  routes: AdministrationRouteDto[],
): PrescriptionLineItem {
  return {
    medicationId: medication.id,
    medicationName: medication.name,
    administrationRouteCode: resolveDefaultRouteCode(medication, routes),
    dosage: medication.defaultDosage ?? '',
    frequency: '',
    notes: '',
  };
}

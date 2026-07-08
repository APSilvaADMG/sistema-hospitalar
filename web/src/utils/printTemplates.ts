import type {
  AppointmentDto,
  BiDashboardDto,
  DashboardAppointmentItemDto,
  DashboardEmergencyItemDto,
  EmergencyVisitDto,
  HospitalizationDto,
  HospitalizationHubDashboardDto,
  HospitalizationHubListItemDto,
  ImagingStudyDto,
  LabOrderDto,
  MedicalRecordEntryDto,
  MedicalRecordSummaryDto,
  MiscellaneousReceiptDto,
  OperationalDashboardDto,
  ParkingSessionDto,
  PatientDetailDto,
  VisitorLogDto,
} from '../api/client';
import {
  appointmentStatusLabel,
  bloodTypeLabels,
  emergencyStatusLabels,
  formatEmergencyVisitStatus,
  formatEntryTypeLabel,
  hospitalizationStatusLabel,
  imagingModalityLabels,
  paymentMethodLabels,
  paymentMethodValue,
  triageUrgencyLabels,
} from '../api/client';
import {
  escapeHtml,
  formatCurrency,
  formatDate,
  formatDateTime,
  logoImg,
  logoImgBadge,
  logoImgTicket,
  logoImgWristband,
  maskCpf,
  printDocument,
  shortId,
} from './printDocument';
import { buildProfessionalReportBody } from './professionalReportTemplate';
import { parkingQrDataUrl, parkingQrPayload } from './parkingQr';

/** Crachá de visitante — formato vertical para impressão em impressora de crachá. */
export function printVisitorBadge(visitor: VisitorLogDto, autoPrint = false) {
  const badgeNo = visitor.badgeNumber || shortId(visitor.id);
  const metaRows = [
    visitor.patientName ? `<div class="badge-visitor-meta"><span>Visita</span><strong>${escapeHtml(visitor.patientName)}</strong></div>` : '',
    visitor.documentNumber ? `<div class="badge-visitor-meta"><span>Documento</span><strong>${escapeHtml(visitor.documentNumber)}</strong></div>` : '',
    `<div class="badge-visitor-meta"><span>Entrada</span><strong>${formatDateTime(visitor.enteredAt)}</strong></div>`,
  ].filter(Boolean).join('');

  const photoHtml = visitor.photoData
    ? `<img class="badge-visitor-photo" src="${visitor.photoData}" alt="" />`
    : '';

  const body = `
    <div class="visitor-badge-card">
      <div class="badge-hole" aria-hidden="true"></div>
      <div class="badge-header">
        <div class="badge-logo-wrap">${logoImgBadge(45)}</div>
      </div>
      <div class="badge-body">
        <div class="badge-type-banner">Visitante</div>
        ${photoHtml}
        <div class="badge-visitor-name">${escapeHtml(visitor.visitorName)}</div>
        <div class="badge-visitor-number-wrap">
          <div class="badge-visitor-number-label">Nº do crachá</div>
          <div class="badge-visitor-number">${escapeHtml(badgeNo)}</div>
        </div>
        ${visitor.destination ? `
          <div class="badge-visitor-dest">
            <span class="badge-visitor-dest-label">Destino</span>
            ${escapeHtml(visitor.destination)}
          </div>
        ` : ''}
        <div class="badge-meta-list">${metaRows}</div>
        <div class="barcode">${shortId(visitor.id)}</div>
        <div class="footer-note">Uso exclusivo nas dependências do hospital.<br/>Devolver na portaria ao sair.</div>
      </div>
    </div>
  `;
  printDocument({
    title: `Crachá — ${visitor.visitorName}`,
    body,
    pageSize: 'visitor-badge',
    autoPrint,
  });
}

/** @deprecated Use printVisitorBadge */
export function printVisitorLabel(visitor: VisitorLogDto) {
  printVisitorBadge(visitor);
}

export function printPatientLabel(patient: PatientDetailDto) {
  const photoHtml = patient.photoData
    ? `<img class="photo" src="${patient.photoData}" alt="" />`
    : '';

  const body = `
    <div class="label-header">
      ${photoHtml}
      <div>
        ${logoImg(31)}
        <span class="badge-type badge-patient">Paciente</span>
      </div>
    </div>
    <div class="field"><strong>Nome</strong><span class="field-value lg">${escapeHtml(patient.fullName)}</span></div>
    ${patient.socialName ? `<div class="field"><strong>Nome social</strong><span class="field-value">${escapeHtml(patient.socialName)}</span></div>` : ''}
    ${patient.medicalRecordNumber ? `<div class="field"><strong>Prontuário</strong><span class="field-value xl">${escapeHtml(patient.medicalRecordNumber)}</span></div>` : ''}
    <div class="field"><strong>CPF</strong><span class="field-value">${maskCpf(patient.cpf)}</span></div>
    <div class="field"><strong>Nascimento</strong><span class="field-value">${escapeHtml(formatDate(patient.birthDate))}</span></div>
    ${patient.bloodType ? `<div class="field"><strong>Tipo sanguíneo</strong><span class="field-value">${escapeHtml(patient.bloodType)}</span></div>` : ''}
    <div class="barcode">${patient.medicalRecordNumber ? escapeHtml(patient.medicalRecordNumber) : shortId(patient.id)}</div>
    <div class="footer-note">Identificação hospitalar — uso interno</div>
  `;
  printDocument({ title: `Etiqueta — ${patient.fullName}`, body, pageSize: 'badge' });
}

/** Pulseira de identificação do paciente — faixa horizontal para impressora de pulseiras. */
export function printPatientWristband(patient: PatientDetailDto, autoPrint = false) {
  const mrn = patient.medicalRecordNumber ?? shortId(patient.id);
  const barcode = patient.medicalRecordNumber ? escapeHtml(patient.medicalRecordNumber) : shortId(patient.id);
  const blood = patient.bloodType ? (bloodTypeLabels[patient.bloodType] ?? patient.bloodType) : '';
  const displayName = patient.socialName
    ? `${patient.fullName} (${patient.socialName})`
    : patient.fullName;

  const body = `
    <div class="wristband-strip">
      <div class="wristband-brand">
        ${logoImgWristband(25)}
        <div class="wristband-brand-label">Paciente</div>
      </div>
      <div class="wristband-content">
        <div class="wristband-name-block">
          <div class="wristband-name">${escapeHtml(displayName)}</div>
          ${patient.socialName && patient.socialName !== patient.fullName
    ? `<div class="wristband-social">Nome civil: ${escapeHtml(patient.fullName)}</div>`
    : ''}
        </div>
        <div class="wristband-divider"></div>
        <div class="wristband-meta-col">
          <div class="wristband-meta">Nasc. <strong>${formatDate(patient.birthDate)}</strong></div>
          <div class="wristband-meta">Pront. <strong>${escapeHtml(mrn)}</strong></div>
        </div>
        <div class="wristband-divider"></div>
        <div class="wristband-meta-col">
          <div class="wristband-meta">CPF <strong>${maskCpf(patient.cpf)}</strong></div>
          <div class="wristband-meta">${escapeHtml('Hospital Sistema de Gestão')}</div>
        </div>
        ${blood ? `<div class="wristband-blood">${escapeHtml(blood)}</div>` : ''}
      </div>
      <div class="wristband-barcode-wrap">
        <div class="wristband-barcode">${barcode}</div>
        <div class="wristband-barcode-label">Identificação</div>
      </div>
    </div>
    <div class="wristband-footer">Não remover — identificação obrigatória durante a permanência no hospital</div>
  `;

  printDocument({
    title: `Pulseira — ${patient.fullName}`,
    body,
    pageSize: 'wristband',
    autoPrint,
  });
}

export function printLabReport(order: LabOrderDto) {
  const rows = order.items.map((item) => {
    const result = item.result;
    const valueCell = result
      ? `<span class="${result.isAbnormal ? 'pr-danger' : ''}">${escapeHtml(result.value)}${result.unit ? ` ${escapeHtml(result.unit)}` : ''}${result.referenceRange ? ` (ref: ${escapeHtml(result.referenceRange)})` : ''}</span>`
      : 'Pendente';
    return `<tr><td>${escapeHtml(item.examName)}</td><td>${valueCell}</td><td>${result?.releasedAt ? formatDateTime(result.releasedAt) : '—'}</td></tr>`;
  }).join('');

  const tableHtml = `
    <table class="pr-table">
      <thead><tr><th>Exame</th><th>Resultado</th><th>Liberação</th></tr></thead>
      <tbody>${rows}</tbody>
    </table>`;

  const body = buildProfessionalReportBody({
    title: 'Relatório de exames laboratoriais',
    documentType: 'Laudo laboratorial',
    code: shortId(order.id),
    layoutKind: 'hospitalrun',
    meta: [
      { label: 'Paciente', value: order.patientName },
      { label: 'Solicitante', value: order.requestingProfessionalName },
      { label: 'Pedido', value: shortId(order.id) },
      { label: 'Data do pedido', value: formatDateTime(order.createdAt) },
    ],
    sections: [{ title: 'Resultados', html: tableHtml }],
    showSignature: true,
    generatedAt: order.createdAt,
  });

  printDocument({ title: `Lab — ${order.patientName}`, body, pageSize: 'report' });
}
export function printImagingReport(study: ImagingStudyDto) {
  const meta = [
    { label: 'Paciente', value: study.patientName },
    { label: 'Exame', value: study.studyDescription },
    { label: 'Modalidade', value: imagingModalityLabels[study.modality] ?? study.modality },
    { label: 'Solicitante', value: study.requestingProfessionalName },
  ];
  if (study.accessionNumber) meta.push({ label: 'Accession', value: study.accessionNumber });
  meta.push({
    label: 'Realizado em',
    value: study.completedAt ? formatDateTime(study.completedAt) : formatDateTime(study.scheduledAt),
  });
  if (study.reportedAt) meta.push({ label: 'Laudado em', value: formatDateTime(study.reportedAt) });

  const body = buildProfessionalReportBody({
    title: 'Laudo de diagnóstico por imagem',
    documentType: 'Laudo radiológico',
    code: study.accessionNumber ?? shortId(study.id),
    layoutKind: 'hospitalrun',
    meta,
    sections: [{
      title: 'Laudo',
      html: `<div class="pr-prose">${escapeHtml(study.reportContent ?? 'Laudo pendente.')}</div>`,
    }],
    showSignature: true,
    generatedAt: study.reportedAt ?? study.completedAt ?? study.scheduledAt,
  });

  printDocument({ title: `Laudo — ${study.patientName}`, body, pageSize: 'report' });
}
export type BiPrintSection =
  | ''
  | 'ocupacao'
  | 'permanencia'
  | 'giro-leitos'
  | 'custos'
  | 'inadimplencia'
  | 'producao-medica'
  | 'producao-hospitalar'
  | 'faturamento';

const BI_SECTION_LABELS: Record<BiPrintSection, string> = {
  '': 'Visão Geral',
  ocupacao: 'Ocupação',
  permanencia: 'Permanência',
  'giro-leitos': 'Giro de Leitos',
  custos: 'Custos',
  inadimplencia: 'Inadimplência',
  'producao-medica': 'Produção Médica',
  'producao-hospitalar': 'Produção Hospitalar',
  faturamento: 'Faturamento',
};

function formatBiPercent(value: number) {
  const sign = value > 0 ? '+' : '';
  return `${sign}${value.toFixed(1)}%`;
}

export function printBiReport(data: BiDashboardDto, activeSection: BiPrintSection = '') {
  const table = (headers: string[], rows: string) =>
    `<table class="pr-table"><thead><tr>${headers.map((h) => `<th>${h}</th>`).join('')}</tr></thead><tbody>${rows}</tbody></table>`;

  const kpiTable = (rows: [string, string][]) => {
    if (rows.length === 0) return '<p class="pr-empty">Sem indicadores para esta seção.</p>';
    const body = rows.map(([label, value]) => `<tr><td>${label}</td><td>${value}</td></tr>`).join('');
    return table(['Indicador', 'Valor'], body);
  };

  const sectionLabel = BI_SECTION_LABELS[activeSection];
  const isOverview = !activeSection;
  const isOccupancy = activeSection === 'ocupacao';
  const isPermanencia = activeSection === 'permanencia';
  const isGiro = activeSection === 'giro-leitos';
  const isCustos = activeSection === 'custos';
  const isInadimplencia = activeSection === 'inadimplencia';
  const isProdMedica = activeSection === 'producao-medica';
  const isProdHospitalar = activeSection === 'producao-hospitalar';
  const isFaturamento = activeSection === 'faturamento';

  const showFinancial = isOverview || isCustos || isInadimplencia || isFaturamento;
  const showRevenueKpis = isOverview || isFaturamento;
  const showExpenseKpis = isOverview || isCustos;
  const showDefaultKpis = isOverview || isInadimplencia;
  const showOperational = isOverview || isOccupancy || isPermanencia || isGiro || isProdMedica || isProdHospitalar;
  const showOccupancyKpis = isOverview || isOccupancy || isPermanencia || isGiro || isProdHospitalar;
  const showRevenueCharts = isOverview || isFaturamento || isCustos;
  const showExpenseChart = isOverview || isCustos;
  const showOccupancy = isOverview || isOccupancy || isPermanencia || isGiro || isProdHospitalar;
  const showProduction = isOverview || isProdMedica || isProdHospitalar;
  const showTissPanels = isOverview || isFaturamento;
  const showErPanel = isOverview || isProdHospitalar || isOccupancy;

  const sections: { title: string; html: string }[] = [];

  if (isPermanencia || isGiro) {
    const rows: [string, string][] = [];
    if (isPermanencia) {
      rows.push(['Permanência média (dias)', String(data.averageLengthOfStayDays)]);
      rows.push(['Altas no mês', String(data.dischargesThisMonth)]);
    }
    if (isGiro) {
      rows.push(['Giro (internações/leito)', String(data.bedTurnoverRate)]);
      rows.push(['Giro mensal estimado', String(data.monthlyBedTurnover)]);
      rows.push(['Internações no mês', String(data.monthlyHospitalizations.at(-1)?.count ?? 0)]);
    }
    sections.push({ title: isPermanencia ? 'Permanência' : 'Giro de leitos', html: kpiTable(rows) });
  }

  if (showExpenseKpis && !isOverview) {
    sections.push({
      title: 'Custos',
      html: kpiTable([
        ['Despesas do mês', formatCurrency(data.expenseThisMonth)],
        ['vs mês anterior', formatBiPercent(data.expenseGrowthPercent)],
        ['Despesas mês anterior', formatCurrency(data.expenseLastMonth)],
      ]),
    });
  }

  if (showDefaultKpis && !isOverview) {
    sections.push({
      title: 'Inadimplência',
      html: kpiTable([
        ['Vencido a receber', formatCurrency(data.overdueReceivable)],
        ['Títulos vencidos', String(data.overdueReceivableCount)],
        ['Inadimplência (% do aberto)', `${data.defaultRatePercent}%`],
        ['Total a receber', formatCurrency(data.revenuePending)],
      ]),
    });
  }

  if (isProdMedica || isProdHospitalar) {
    const rows: [string, string][] = [];
    if (isProdMedica) rows.push(['Produção médica (mês)', String(data.medicalProductionThisMonth)]);
    if (isProdHospitalar) rows.push(['Produção hospitalar (mês)', String(data.hospitalProductionThisMonth)]);
    sections.push({
      title: isProdMedica ? 'Produção médica' : 'Produção hospitalar',
      html: kpiTable(rows),
    });
  }

  if (showFinancial) {
    const financeRows: [string, string][] = [];
    if (showRevenueKpis) {
      financeRows.push(['Receita do mês', formatCurrency(data.revenueThisMonth)]);
      financeRows.push(['vs mês anterior', formatBiPercent(data.revenueGrowthPercent)]);
      financeRows.push(['Receita mês anterior', formatCurrency(data.revenueLastMonth)]);
    }
    if (showRevenueKpis || showDefaultKpis) {
      financeRows.push(['A receber', formatCurrency(data.revenuePending)]);
      financeRows.push(['Títulos a receber', String(data.financialAccountsOpen)]);
    }
    if (showRevenueKpis) {
      financeRows.push(['TISS pendente', formatCurrency(data.tissAmountPending)]);
      financeRows.push(['Guias TISS abertas', String(data.tissGuidesPending)]);
    }
    if (showExpenseKpis && isOverview) {
      financeRows.push(['Despesas do mês', formatCurrency(data.expenseThisMonth)]);
      financeRows.push(['Vencido a receber', formatCurrency(data.overdueReceivable)]);
    }
    if (financeRows.length > 0) {
      sections.push({ title: 'Financeiro', html: kpiTable(financeRows) });
    }
  }

  if (showOperational) {
    const operationRows: [string, string][] = [];
    if (isOverview) {
      operationRows.push(['Pacientes', String(data.totalPatients)]);
      operationRows.push(['Internações ativas', String(data.activeHospitalizations)]);
    }
    if (showOccupancyKpis) {
      operationRows.push(['Ocupação de leitos', `${data.bedOccupancyRate}% (${data.occupiedBeds}/${data.totalBeds})`]);
      operationRows.push(['Leitos', `${data.occupiedBeds}/${data.totalBeds}`]);
    }
    if (isOverview) {
      operationRows.push(['Permanência média (dias)', String(data.averageLengthOfStayDays)]);
      operationRows.push(['Giro de leitos', String(data.bedTurnoverRate)]);
    }
    if (isOverview || isProdHospitalar) {
      operationRows.push(['PS aguardando', String(data.emergencyWaiting)]);
      operationRows.push(['PS em atendimento', String(data.emergencyInCare)]);
    }
    if (isOverview || isProdMedica) {
      operationRows.push(['Consultas hoje', String(data.appointmentsToday)]);
      operationRows.push(['Cirurgias hoje', String(data.surgeriesToday)]);
    }
    if (isOverview || isProdMedica || isProdHospitalar) {
      operationRows.push(['Lab pendente', String(data.labOrdersPending)]);
      operationRows.push(['Imagem pendente', String(data.imagingStudiesPending)]);
    }
    if (isOverview) {
      operationRows.push(['Estoque crítico', `${data.lowStockProducts} itens`]);
      operationRows.push(['Compras aguardando', String(data.purchaseOrdersPending)]);
    }
    if (operationRows.length > 0) {
      sections.push({ title: 'Operação', html: kpiTable(operationRows) });
    }
  }

  const monthlyApptRows = data.monthlyAppointments.map((m) =>
    `<tr><td>${escapeHtml(m.label)}</td><td>${m.count}</td></tr>`,
  ).join('') || '<tr><td colspan="2" class="pr-empty">Sem dados</td></tr>';

  const monthlyRevRows = data.monthlyRevenue.map((m) =>
    `<tr><td>${escapeHtml(m.label)}</td><td>${formatCurrency(m.amount ?? 0)}</td></tr>`,
  ).join('') || '<tr><td colspan="2" class="pr-empty">Sem dados</td></tr>';

  const monthlyExpenseRows = data.monthlyExpenses.map((m) =>
    `<tr><td>${escapeHtml(m.label)}</td><td>${formatCurrency(m.amount ?? 0)}</td></tr>`,
  ).join('') || '<tr><td colspan="2" class="pr-empty">Sem dados</td></tr>';

  const monthlyHospRows = data.monthlyHospitalizations.map((m) =>
    `<tr><td>${escapeHtml(m.label)}</td><td>${m.count}</td></tr>`,
  ).join('') || '<tr><td colspan="2" class="pr-empty">Sem dados</td></tr>';

  if (showRevenueCharts) {
    if (isOverview || isFaturamento) {
      sections.push({ title: 'Receita — últimos 6 meses', html: table(['Mês', 'Valor'], monthlyRevRows) });
    }
    if (showExpenseChart) {
      sections.push({ title: 'Despesas — últimos 6 meses', html: table(['Mês', 'Valor'], monthlyExpenseRows) });
    }
    if (isOverview || isProdMedica || isFaturamento) {
      sections.push({ title: 'Agendamentos — últimos 6 meses', html: table(['Mês', 'Quantidade'], monthlyApptRows) });
    }
    if (isOverview || isPermanencia || isGiro || isProdHospitalar) {
      sections.push({ title: 'Internações — últimos 6 meses', html: table(['Mês', 'Quantidade'], monthlyHospRows) });
    }
  }

  const categoryRows = data.revenueByCategory.map((c) =>
    `<tr><td>${escapeHtml(c.label)}</td><td>${c.count}</td><td>${formatCurrency(c.amount)}</td></tr>`,
  ).join('') || '<tr><td colspan="3" class="pr-empty">Sem receita</td></tr>';

  const specialtyRows = data.topSpecialties.map((s) =>
    `<tr><td>${escapeHtml(s.specialtyName)}</td><td>${s.appointmentsThisMonth}</td></tr>`,
  ).join('') || '<tr><td colspan="2" class="pr-empty">Sem dados</td></tr>';

  const wardRows = data.wardOccupancy.map((w) =>
    `<tr><td>${escapeHtml(w.wardName)}</td><td>${w.occupiedBeds}/${w.totalBeds}</td><td>${w.occupancyRate}%</td></tr>`,
  ).join('') || '<tr><td colspan="3" class="pr-empty">—</td></tr>';

  if ((showRevenueKpis && isFaturamento) || showProduction || showOccupancy) {
    if (isOverview || isFaturamento) {
      sections.push({ title: 'Receita por categoria (mês)', html: table(['Categoria', 'Qtd', 'Valor'], categoryRows) });
    }
    if (isOverview || isProdMedica) {
      sections.push({ title: 'Especialidades — volume no mês', html: table(['Especialidade', 'Consultas'], specialtyRows) });
    }
    if (showOccupancy) {
      sections.push({ title: 'Ocupação por ala', html: table(['Ala', 'Leitos', 'Ocupação'], wardRows) });
    }
  }

  const tissRows = data.tissGuidesByStatus.map((s) =>
    `<tr><td>${escapeHtml(s.label)}</td><td>${s.count}</td><td>${s.amount != null ? formatCurrency(s.amount) : '—'}</td></tr>`,
  ).join('') || '<tr><td colspan="3" class="pr-empty">Nenhuma guia</td></tr>';

  const financialStatusRows = data.financialAccountsByStatus.map((s) =>
    `<tr><td>${escapeHtml(s.label)}</td><td>${s.count}</td><td>${s.amount != null ? formatCurrency(s.amount) : '—'}</td></tr>`,
  ).join('') || '<tr><td colspan="3" class="pr-empty">Sem títulos</td></tr>';

  const emergencyRows = data.emergencyByUrgency.map((s) =>
    `<tr><td>${escapeHtml(s.label)}</td><td>${s.count}</td></tr>`,
  ).join('') || '<tr><td colspan="2" class="pr-empty">Fila vazia</td></tr>';

  if (showTissPanels || showDefaultKpis || showErPanel) {
    if (isOverview || isFaturamento) {
      sections.push({ title: 'Guias TISS por status', html: table(['Status', 'Qtd', 'Valor'], tissRows) });
    }
    if (isOverview || isInadimplencia) {
      sections.push({ title: 'Contas financeiras', html: table(['Status', 'Qtd', 'Valor'], financialStatusRows) });
    }
    if (showErPanel) {
      sections.push({ title: 'Fila PS por urgência', html: table(['Urgência', 'Qtd'], emergencyRows) });
    }
  }

  if (isOverview || isProdMedica || isProdHospitalar) {
    const labRows = data.labOrdersByStatus.map((s) =>
      `<tr><td>${escapeHtml(s.label)}</td><td>${s.count}</td></tr>`,
    ).join('') || '<tr><td colspan="2" class="pr-empty">Sem pedidos</td></tr>';
    const imagingRows = data.imagingByStatus.map((s) =>
      `<tr><td>${escapeHtml(s.label)}</td><td>${s.count}</td></tr>`,
    ).join('') || '<tr><td colspan="2" class="pr-empty">Sem exames</td></tr>';
    const stockRows = data.lowStockItems.map((i) =>
      `<tr><td>${escapeHtml(i.productName)}</td><td>${escapeHtml(i.sku)}</td><td>${i.onHand}/${i.minimum} ${escapeHtml(i.unit)}</td></tr>`,
    ).join('') || '<tr><td colspan="3" class="pr-empty">Nenhum item</td></tr>';

    sections.push({ title: 'Laboratório por status', html: table(['Status', 'Qtd'], labRows) });
    sections.push({ title: 'Imagem por status', html: table(['Status', 'Qtd'], imagingRows) });
    sections.push({ title: 'Estoque crítico — top itens', html: table(['Produto', 'SKU', 'Saldo'], stockRows) });
  }

  const meta: { label: string; value: string }[] = [{ label: 'Seção', value: sectionLabel }];
  if (showOccupancyKpis) meta.push({ label: 'Ocupação', value: `${data.bedOccupancyRate}%` });
  if (showRevenueKpis) meta.push({ label: 'Receita do mês', value: formatCurrency(data.revenueThisMonth) });
  if (isOverview) meta.push({ label: 'Pacientes', value: String(data.totalPatients) });

  const body = buildProfessionalReportBody({
    title: `Painel gerencial — ${sectionLabel}`,
    subtitle: isOverview
      ? 'Consolidado operacional, financeiro e assistencial'
      : `Indicadores da seção ${sectionLabel}`,
    documentType: 'Relatório gerencial',
    code: activeSection ? `bi.${activeSection}` : 'bi.dashboard',
    layoutKind: 'bi-managerial',
    meta,
    sections,
    showSignature: true,
    generatedAt: data.generatedAt,
  });

  printDocument({ title: `Relatório BI — ${sectionLabel}`, body, pageSize: 'report' });
}
export async function printParkingEntryTicket(
  session: ParkingSessionDto,
  hourlyRate: number,
  autoPrint = false,
) {
  const payload = parkingQrPayload(session.id, session.qrPayload);
  const qrDataUrl = await parkingQrDataUrl(payload, 180);
  const body = `
    <div class="ticket-brand-header">${logoImgTicket(63)}</div>
    <div class="doc-title">Ticket de entrada</div>
    <div class="field"><strong>Ticket</strong><span class="field-value xl">${shortId(session.id)}</span></div>
    <div class="divider"></div>
    <div class="field"><strong>Placa</strong><span class="field-value xl">${escapeHtml(session.vehiclePlate)}</span></div>
    <div class="field"><strong>Zona</strong><span class="field-value">${escapeHtml(session.zoneName)}</span></div>
    ${session.patientName ? `<div class="field"><strong>Paciente</strong><span class="field-value">${escapeHtml(session.patientName)}</span></div>` : ''}
    <div class="field"><strong>Entrada</strong><span class="field-value">${formatDateTime(session.enteredAt)}</span></div>
    <div class="field"><strong>Tarifa</strong><span class="field-value">${formatCurrency(hourlyRate)} / hora</span></div>
    <div class="ticket-qr-wrap">
      <img class="ticket-qr" src="${qrDataUrl}" alt="QR Code do ticket" />
      <div class="ticket-qr-label">Apresente na cancela de saída</div>
    </div>
    <div class="barcode">${shortId(session.id)}</div>
    <div class="footer-note">1. Pague no caixa antes de sair.<br/>2. A cancela só libera após o pagamento.<br/>Cobrança mínima de 1 hora.</div>
  `;
  printDocument({ title: `Entrada — ${session.vehiclePlate}`, body, pageSize: 'ticket', autoPrint });
}

export async function printParkingPaymentReceipt(
  session: ParkingSessionDto,
  hourlyRate: number,
) {
  const amount = session.amountCharged ?? session.estimatedAmount ?? hourlyRate;
  const body = `
    ${logoImg(31)}
    <div class="doc-title">Comprovante de pagamento</div>
    <div class="field"><strong>Ticket</strong><span class="field-value">${shortId(session.id)}</span></div>
    <div class="divider"></div>
    <div class="field"><strong>Placa</strong><span class="field-value lg">${escapeHtml(session.vehiclePlate)}</span></div>
    <div class="field"><strong>Zona</strong><span class="field-value">${escapeHtml(session.zoneName)}</span></div>
    <div class="row"><span>Pago em</span><span>${session.paidAt ? formatDateTime(session.paidAt) : formatDateTime(new Date().toISOString())}</span></div>
    <div class="divider"></div>
    <div class="amount">Total pago: ${formatCurrency(amount)}</div>
    <div class="footer-note">Pagamento confirmado. Dirija-se à cancela — o QR Code do ticket liberará a saída.</div>
  `;
  printDocument({ title: `Pagamento — ${session.vehiclePlate}`, body, pageSize: 'ticket' });
}

export function printParkingExitReceipt(
  session: ParkingSessionDto,
  hourlyRate: number,
) {
  const entered = new Date(session.enteredAt);
  const exited = session.exitedAt ? new Date(session.exitedAt) : new Date();
  const hours = Math.max(1, Math.ceil((exited.getTime() - entered.getTime()) / 3_600_000));
  const amount = session.amountCharged ?? hours * hourlyRate;

  const durationMs = exited.getTime() - entered.getTime();
  const durationH = Math.floor(durationMs / 3_600_000);
  const durationM = Math.floor((durationMs % 3_600_000) / 60_000);
  const durationLabel = `${durationH}h ${durationM}min`;

  const body = `
    ${logoImg(31)}
    <div class="doc-title">Comprovante de saída</div>
    <div class="field"><strong>Ticket</strong><span class="field-value">${shortId(session.id)}</span></div>
    <div class="divider"></div>
    <div class="field"><strong>Placa</strong><span class="field-value lg">${escapeHtml(session.vehiclePlate)}</span></div>
    <div class="field"><strong>Zona</strong><span class="field-value">${escapeHtml(session.zoneName)}</span></div>
    <div class="row"><span>Entrada</span><span>${formatDateTime(session.enteredAt)}</span></div>
    <div class="row"><span>Saída</span><span>${formatDateTime(session.exitedAt ?? exited.toISOString())}</span></div>
    <div class="row"><span>Permanência</span><span>${durationLabel}</span></div>
    <div class="row"><span>Horas cobradas</span><span>${hours}h</span></div>
    <div class="row"><span>Tarifa</span><span>${formatCurrency(hourlyRate)}/h</span></div>
    <div class="divider"></div>
    <div class="amount">Total: ${formatCurrency(amount)}</div>
    <div class="barcode">${shortId(session.id)}</div>
    <div class="footer-note">Obrigado pela preferência!</div>
  `;
  printDocument({ title: `Saída — ${session.vehiclePlate}`, body, pageSize: 'ticket' });
}

export function printMiscellaneousReceipt(receipt: MiscellaneousReceiptDto, autoPrint = true) {
  const methodLabel = paymentMethodLabels[paymentMethodValue(receipt.paymentMethod)] ?? '—';
  const detailRows = `
    <div class="pr-info-rows">
      <div class="pr-info-row"><strong>Recebemos de</strong><span>${escapeHtml(receipt.payerName)}</span></div>
      <div class="pr-info-row"><strong>Recebedor</strong><span>${escapeHtml(receipt.receiverName)}</span></div>
      <div class="pr-info-row"><strong>Valor</strong><span>${formatCurrency(receipt.amount)}</span></div>
      <div class="pr-info-row"><strong>Referente a</strong><span>${escapeHtml(receipt.description)}</span></div>
      <div class="pr-info-row"><strong>Forma de pagamento</strong><span>${escapeHtml(methodLabel)}</span></div>
      ${receipt.reference ? `<div class="pr-info-row"><strong>Referência</strong><span>${escapeHtml(receipt.reference)}</span></div>` : ''}
      <div class="pr-info-row"><strong>Identificação</strong><span>${shortId(receipt.id)}</span></div>
    </div>`;

  const body = buildProfessionalReportBody({
    title: `Recibo ${receipt.receiptNumber}`,
    documentType: 'Recibo diverso',
    code: receipt.receiptNumber,
    layoutKind: 'default',
    meta: [
      { label: 'Data', value: formatDate(receipt.receiptDate) },
      { label: 'Pagador', value: receipt.payerName },
      { label: 'Valor', value: formatCurrency(receipt.amount) },
    ],
    sections: [{ title: 'Detalhes do recibo', html: detailRows }],
    generatedAt: receipt.receiptDate,
  });

  printDocument({
    title: `Recibo ${receipt.receiptNumber}`,
    body,
    pageSize: 'report',
    autoPrint,
  });
}

const STOCK_MOVEMENT_TYPE_LABELS: Record<number, string> = {
  1: 'Entrada',
  2: 'Saída',
  3: 'Ajuste / transferência',
};

const PAYROLL_STATUS_LABELS: Record<number, string> = {
  1: 'Rascunho',
  2: 'Gerada',
  3: 'Aprovada',
  4: 'Paga',
};

function tableHtml(headers: string[], rows: string) {
  return `<table class="pr-table"><thead><tr>${headers.map((h) => `<th>${h}</th>`).join('')}</tr></thead><tbody>${rows}</tbody></table>`;
}

function moneyRows(rows: [string, string][]) {
  if (rows.length === 0) return '<p class="pr-empty">Sem dados.</p>';
  const body = rows.map(([label, value]) => `<tr><td>${label}</td><td>${value}</td></tr>`).join('');
  return tableHtml(['Indicador', 'Valor'], body);
}

export function printPayrollSlip(slip: import('../api/client').PayrollSlipDto) {
  const period = `${String(slip.month).padStart(2, '0')}/${slip.year}`;
  const earningsRows = slip.earnings.map((line) =>
    `<tr><td>${escapeHtml(line.code)}</td><td>${escapeHtml(line.description)}</td><td>${formatCurrency(line.amount)}</td></tr>`,
  ).join('') || '<tr><td colspan="3" class="pr-empty">Nenhum provento</td></tr>';
  const discountRows = slip.discounts.map((line) =>
    `<tr><td>${escapeHtml(line.code)}</td><td>${escapeHtml(line.description)}</td><td>${formatCurrency(line.amount)}</td></tr>`,
  ).join('') || '<tr><td colspan="3" class="pr-empty">Nenhum desconto</td></tr>';

  const body = buildProfessionalReportBody({
    title: 'Holerite / Demonstrativo de pagamento',
    subtitle: `${escapeHtml(slip.item.employeeName)}${slip.item.jobTitle ? ` — ${escapeHtml(slip.item.jobTitle)}` : ''}`,
    documentType: 'Folha de pagamento',
    code: `holerite.${period}.${shortId(slip.item.employeeId)}`,
    layoutKind: 'default',
    meta: [
      { label: 'Competência', value: period },
      { label: 'Departamento', value: slip.item.departmentName },
      { label: 'Status', value: PAYROLL_STATUS_LABELS[slip.status] ?? String(slip.status) },
    ],
    sections: [
      { title: 'Proventos', html: tableHtml(['Cód.', 'Descrição', 'Valor'], earningsRows) },
      { title: 'Descontos', html: tableHtml(['Cód.', 'Descrição', 'Valor'], discountRows) },
      {
        title: 'Totais',
        html: moneyRows([
          ['Salário base', formatCurrency(slip.item.baseSalary)],
          ['Bruto', formatCurrency(slip.item.grossAmount)],
          ['Descontos', formatCurrency(slip.item.discountAmount)],
          ['Líquido', formatCurrency(slip.item.netAmount)],
          ['FGTS empregador (8%)', formatCurrency(slip.totalFgtsEmployer)],
        ]),
      },
    ],
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: `Holerite — ${slip.item.employeeName}`, body, pageSize: 'report' });
}

export function printPayrollMonthlySummary(
  summary: import('../api/client').PayrollMonthlySummaryDto,
  run?: import('../api/client').PayrollRunDto | null,
) {
  const period = `${String(summary.month).padStart(2, '0')}/${summary.year}`;
  const deptRows = summary.byDepartment.map((d) =>
    `<tr><td>${escapeHtml(d.departmentName)}</td><td>${d.employeeCount}</td><td>${formatCurrency(d.totalGross)}</td><td>${formatCurrency(d.totalNet)}</td></tr>`,
  ).join('') || '<tr><td colspan="4" class="pr-empty">Nenhum colaborador na competência.</td></tr>';

  const employeeRows = run?.items.map((item) =>
    `<tr><td>${escapeHtml(item.employeeName)}</td><td>${escapeHtml(item.jobTitle ?? '—')}</td><td>${escapeHtml(item.departmentName)}</td><td>${formatCurrency(item.grossAmount)}</td><td>${formatCurrency(item.discountAmount)}</td><td>${formatCurrency(item.netAmount)}</td></tr>`,
  ).join('') ?? '';

  const sections: { title: string; html: string }[] = [
    {
      title: 'Indicadores da competência',
      html: moneyRows([
        ['Colaboradores', String(summary.employeeCount)],
        ['Bruto total', formatCurrency(summary.totalGross)],
        ['Descontos', formatCurrency(summary.totalDiscounts)],
        ['Líquido total', formatCurrency(summary.totalNet)],
        ['FGTS empregador', formatCurrency(summary.totalFgtsEmployer)],
        ['Colaboradores de férias no mês', String(summary.employeesOnVacation)],
        ['Plantões noturnos no mês', String(summary.nightShiftsInMonth)],
      ]),
    },
    { title: 'Resumo por departamento', html: tableHtml(['Departamento', 'Colab.', 'Bruto', 'Líquido'], deptRows) },
  ];

  if (employeeRows) {
    sections.push({
      title: 'Colaboradores',
      html: tableHtml(['Nome', 'Cargo', 'Departamento', 'Bruto', 'Descontos', 'Líquido'], employeeRows),
    });
  }

  const body = buildProfessionalReportBody({
    title: `Resumo mensal — Folha ${period}`,
    documentType: 'Relatório de folha de pagamento',
    code: `folha.resumo.${period}`,
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Competência', value: period },
      { label: 'Status', value: summary.status ? (PAYROLL_STATUS_LABELS[summary.status] ?? String(summary.status)) : 'Não gerada' },
      { label: 'Líquido total', value: formatCurrency(summary.totalNet) },
    ],
    sections,
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: `Folha ${period} — Resumo`, body, pageSize: 'report' });
}

export function printStockPositionReport(products: import('../api/client').ProductDto[]) {
  const rows = products
    .filter((p) => p.quantityOnHand > 0 || p.isLowStock)
    .sort((a, b) => a.name.localeCompare(b.name, 'pt-BR'))
    .map((p) =>
      `<tr><td>${escapeHtml(p.sku)}</td><td>${escapeHtml(p.name)}</td><td>${p.quantityOnHand} ${escapeHtml(p.unit)}</td><td>${p.minimumStock}</td><td>${p.isLowStock ? 'Abaixo do mínimo' : 'OK'}</td></tr>`,
    ).join('') || '<tr><td colspan="5" class="pr-empty">Nenhum produto com saldo.</td></tr>';

  const lowCount = products.filter((p) => p.isLowStock).length;
  const totalUnits = products.reduce((s, p) => s + p.quantityOnHand, 0);

  const body = buildProfessionalReportBody({
    title: 'Posição de estoque — Almoxarifado',
    documentType: 'Relatório de estoque',
    code: 'estoque.posicao',
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Produtos listados', value: String(products.length) },
      { label: 'Unidades em saldo', value: String(totalUnits) },
      { label: 'Abaixo do mínimo', value: String(lowCount) },
    ],
    sections: [{
      title: 'Saldo por produto',
      html: tableHtml(['SKU', 'Produto', 'Saldo', 'Mínimo', 'Situação'], rows),
    }],
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: 'Posição de estoque', body, pageSize: 'report' });
}

export function printStockMovementsReport(
  movements: import('../api/client').StockMovementDto[],
  from: string,
  to: string,
  summary: { inbound: number; outbound: number; total: number },
) {
  const rows = movements.map((m) =>
    `<tr><td>${formatDateTime(m.createdAt)}</td><td>${escapeHtml(STOCK_MOVEMENT_TYPE_LABELS[m.type] ?? String(m.type))}</td><td>${escapeHtml(m.productName)}</td><td>${m.quantity}</td><td>${escapeHtml(m.reason)}</td><td>${escapeHtml(m.batchNumber ?? '—')}</td></tr>`,
  ).join('') || '<tr><td colspan="6" class="pr-empty">Nenhuma movimentação no período.</td></tr>';

  const body = buildProfessionalReportBody({
    title: 'Movimentações de estoque',
    documentType: 'Relatório de almoxarifado',
    code: 'estoque.movimentacoes',
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Período', value: `${formatDate(from)} a ${formatDate(to)}` },
      { label: 'Registros', value: String(summary.total) },
      { label: 'Entradas (un.)', value: String(summary.inbound) },
      { label: 'Saídas (un.)', value: String(summary.outbound) },
    ],
    sections: [{
      title: 'Detalhamento',
      html: tableHtml(['Data', 'Tipo', 'Produto', 'Qtd', 'Motivo', 'Lote'], rows),
    }],
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: 'Movimentações de estoque', body, pageSize: 'report' });
}

export function printExpiringLotsReport(
  lots: import('../api/client').ProductLotDto[],
  days: number,
) {
  const rows = lots
    .sort((a, b) => (a.expiryDate ?? '').localeCompare(b.expiryDate ?? ''))
    .map((lot) =>
      `<tr><td>${escapeHtml(lot.productName)}</td><td>${escapeHtml(lot.productSku)}</td><td>${escapeHtml(lot.batchNumber)}</td><td>${lot.expiryDate ? formatDate(lot.expiryDate) : '—'}</td><td>${lot.quantityOnHand}</td><td>${escapeHtml(lot.locationName ?? '—')}</td><td>${lot.isExpiringSoon ? 'Crítico' : 'Atenção'}</td></tr>`,
    ).join('') || '<tr><td colspan="7" class="pr-empty">Nenhum lote a vencer no período.</td></tr>';

  const body = buildProfessionalReportBody({
    title: `Lotes a vencer — próximos ${days} dias`,
    documentType: 'Relatório de validade',
    code: 'estoque.lotes-vencimento',
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Janela (dias)', value: String(days) },
      { label: 'Lotes listados', value: String(lots.length) },
      { label: 'Unidades em risco', value: String(lots.reduce((s, l) => s + l.quantityOnHand, 0)) },
    ],
    sections: [{
      title: 'Lotes próximos do vencimento',
      html: tableHtml(['Produto', 'SKU', 'Lote', 'Validade', 'Saldo', 'Local', 'Alerta'], rows),
    }],
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: 'Lotes a vencer', body, pageSize: 'report' });
}

function clinicalPatientDisplayName(patient: PatientDetailDto) {
  return patient.socialName && patient.socialName !== patient.fullName
    ? `${patient.fullName} (${patient.socialName})`
    : patient.fullName;
}

function clinicalPatientMeta(
  patient: PatientDetailDto,
  recordNumber?: string,
  generatedAt?: string,
) {
  const meta = [
    { label: 'Paciente', value: clinicalPatientDisplayName(patient) },
    { label: 'CPF', value: maskCpf(patient.cpf) },
  ];
  if (recordNumber) meta.push({ label: 'Prontuário', value: recordNumber });
  meta.push({ label: 'Data', value: formatDate(generatedAt ?? new Date().toISOString()) });
  return meta;
}

function clinicalProseHtml(text: string) {
  return `<div class="pr-prose">${escapeHtml(text).replace(/\n/g, '<br/>')}</div>`;
}

function clinicalEntriesHtml(entries: MedicalRecordEntryDto[]) {
  if (entries.length === 0) {
    return '<p class="pr-empty">Nenhum registro clínico para imprimir.</p>';
  }

  return entries.map((entry) => {
    const head = [
      escapeHtml(formatEntryTypeLabel(entry.entryType)),
      formatDateTime(entry.createdAt),
      entry.professionalName ? escapeHtml(entry.professionalName) : '',
    ].filter(Boolean).join(' · ');

    const signed = entry.isSigned
      ? `<p class="pr-muted">Assinado digitalmente${entry.signedByProfessionalName ? ` por ${escapeHtml(entry.signedByProfessionalName)}` : ''}${entry.signedAt ? ` · ${formatDateTime(entry.signedAt)}` : ''}</p>`
      : '';

    return `
      <article class="pr-clinical-entry">
        <header class="pr-clinical-entry-head"><strong>${head}</strong></header>
        ${entry.cid10Code ? `<p class="pr-muted">CID-10: ${escapeHtml(entry.cid10Code)}</p>` : ''}
        ${clinicalProseHtml(entry.content)}
        ${signed}
      </article>`;
  }).join('');
}

function hospitalizationStayDays(hospitalization: HospitalizationDto) {
  const end = hospitalization.dischargedAt ? new Date(hospitalization.dischargedAt).getTime() : Date.now();
  const start = new Date(hospitalization.admittedAt).getTime();
  return Math.max(1, Math.ceil((end - start) / 86_400_000));
}

/** Relatório completo ou parcial do prontuário clínico (PEP). */
export function printMedicalRecordReport(
  patient: PatientDetailDto,
  record: MedicalRecordSummaryDto,
  entries?: MedicalRecordEntryDto[],
) {
  const list = entries ?? record.entries;
  const sorted = [...list].sort(
    (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime(),
  );

  const typeCounts = sorted.reduce<Record<string, number>>((acc, entry) => {
    const label = formatEntryTypeLabel(entry.entryType);
    acc[label] = (acc[label] ?? 0) + 1;
    return acc;
  }, {});
  const summaryRows = Object.entries(typeCounts)
    .map(([label, count]) => `<tr><td>${escapeHtml(label)}</td><td>${count}</td></tr>`)
    .join('') || '<tr><td colspan="2" class="pr-empty">Sem registros</td></tr>';

  const body = buildProfessionalReportBody({
    title: 'Prontuário eletrônico do paciente',
    documentType: 'Relatório clínico — PEP',
    code: record.recordNumber || shortId(record.id),
    layoutKind: 'hospitalrun',
    meta: clinicalPatientMeta(patient, record.recordNumber),
    sections: [
      {
        title: 'Resumo',
        html: tableHtml(['Tipo de registro', 'Quantidade'], summaryRows),
      },
      {
        title: 'Registros clínicos',
        html: clinicalEntriesHtml(sorted),
      },
    ],
    showSignature: true,
    generatedAt: sorted[0]?.createdAt ?? new Date().toISOString(),
  });

  printDocument({
    title: `Prontuário — ${patient.fullName}`,
    body,
    pageSize: 'report',
  });
}

/** Receituário / prescrição médica individual. */
export function printPrescriptionReport(
  patient: PatientDetailDto,
  entry: MedicalRecordEntryDto,
  recordNumber?: string,
) {
  const meta = clinicalPatientMeta(patient, recordNumber, entry.createdAt);
  if (entry.professionalName) {
    meta.push({ label: 'Prescritor', value: entry.professionalName });
  }
  if (entry.cid10Code) {
    meta.push({ label: 'CID-10', value: entry.cid10Code });
  }
  if (entry.isSigned) {
    meta.push({
      label: 'Assinatura',
      value: entry.signedByProfessionalName
        ? `Digital — ${entry.signedByProfessionalName}${entry.signedAt ? ` (${formatDateTime(entry.signedAt)})` : ''}`
        : 'Digital',
    });
  }

  const body = buildProfessionalReportBody({
    title: 'Prescrição médica',
    documentType: 'Receituário',
    code: shortId(entry.id),
    layoutKind: 'hospitalrun',
    meta,
    sections: [{
      title: 'Medicamentos e orientações',
      html: clinicalProseHtml(entry.content),
      avoidBreak: true,
    }],
    showSignature: true,
    generatedAt: entry.createdAt,
  });

  printDocument({
    title: `Prescrição — ${patient.fullName}`,
    body,
    pageSize: 'report',
  });
}

/** Sumário de alta hospitalar (internação ativa ou histórico). */
export function printDischargeSummary(
  patient: PatientDetailDto,
  hospitalization: HospitalizationDto,
  options: { recordNumber?: string; dischargeNotes?: string } = {},
) {
  const stayDays = hospitalizationStayDays(hospitalization);
  const isDischarged = Boolean(hospitalization.dischargedAt);

  const internacaoRows: [string, string][] = [
    ['Ala / setor', hospitalization.wardName],
    ['Leito', hospitalization.bedNumber],
    ['Médico responsável', hospitalization.professionalName],
    ['Admissão', formatDateTime(hospitalization.admittedAt)],
    ['Permanência (dias)', String(stayDays)],
    ['Motivo', hospitalization.reason],
  ];
  if (hospitalization.diagnosis) {
    internacaoRows.push(['Diagnóstico', hospitalization.diagnosis]);
  }
  if (isDischarged && hospitalization.dischargedAt) {
    internacaoRows.push(['Alta em', formatDateTime(hospitalization.dischargedAt)]);
  }
  internacaoRows.push(['Situação', hospitalizationStatusLabel(hospitalization.status)]);

  const sections: { title: string; html: string; avoidBreak?: boolean }[] = [
    { title: 'Dados da internação', html: moneyRows(internacaoRows) },
  ];

  const notes = options.dischargeNotes?.trim();
  if (notes) {
    sections.push({
      title: 'Orientações de alta',
      html: clinicalProseHtml(notes),
      avoidBreak: true,
    });
  }

  if (!isDischarged && !notes) {
    sections.push({
      title: 'Orientações de alta',
      html: '<p class="pr-empty">Preencha as orientações antes de imprimir o comprovante de alta.</p>',
    });
  }

  const body = buildProfessionalReportBody({
    title: isDischarged ? 'Comprovante de alta hospitalar' : 'Sumário de alta hospitalar',
    documentType: 'Alta hospitalar',
    code: shortId(hospitalization.id),
    layoutKind: 'hospitalrun',
    meta: clinicalPatientMeta(
      patient,
      options.recordNumber,
      hospitalization.dischargedAt ?? new Date().toISOString(),
    ),
    sections,
    showSignature: true,
    generatedAt: hospitalization.dischargedAt ?? hospitalization.admittedAt,
  });

  printDocument({
    title: `Alta — ${patient.fullName}`,
    body,
    pageSize: 'report',
  });
}

export type DailyAgendaPrintRow = Pick<
  AppointmentDto,
  'scheduledAt' | 'patientName' | 'professionalName' | 'specialtyName' | 'status' | 'room'
>;

export type EmergencyQueuePrintVisit = Pick<
  EmergencyVisitDto,
  'patientName' | 'chiefComplaint' | 'urgency' | 'status' | 'arrivedAt' | 'professionalName'
>;

function normalizeAgendaRows(
  appointments: DailyAgendaPrintRow[] | DashboardAppointmentItemDto[],
): DailyAgendaPrintRow[] {
  return appointments.map((a) => ({
    scheduledAt: a.scheduledAt,
    patientName: a.patientName,
    professionalName: a.professionalName,
    specialtyName: ('specialtyName' in a ? a.specialtyName : undefined) ?? '—',
    status: a.status,
    room: 'room' in a ? a.room : undefined,
  }));
}

function normalizeEmergencyRows(
  visits: EmergencyQueuePrintVisit[] | DashboardEmergencyItemDto[],
): EmergencyQueuePrintVisit[] {
  return visits.map((v) => ({
    patientName: v.patientName,
    chiefComplaint: v.chiefComplaint,
    urgency: v.urgency,
    status: v.status,
    arrivedAt: v.arrivedAt,
    professionalName: 'professionalName' in v ? v.professionalName : undefined,
  }));
}

function urgencyPrintLabel(urgency: string) {
  return triageUrgencyLabels[urgency] ?? urgency;
}

function emergencyStatusPrintLabel(status: string) {
  return emergencyStatusLabels[status] ?? formatEmergencyVisitStatus(status);
}

/** Relatório gerencial — agenda do dia (ambulatorial). */
export function printDailyAgendaReport(
  date: string,
  appointments: DailyAgendaPrintRow[] | DashboardAppointmentItemDto[],
  stats?: { total?: number; confirmed?: number; waiting?: number; inProgress?: number; done?: number },
) {
  const rows = normalizeAgendaRows(appointments);
  const dateLabel = formatDate(date);
  const sorted = [...rows].sort(
    (a, b) => new Date(a.scheduledAt).getTime() - new Date(b.scheduledAt).getTime(),
  );

  const tableRows = sorted.map((a) =>
    `<tr>
      <td>${formatBrTimeCell(a.scheduledAt)}</td>
      <td>${escapeHtml(a.patientName)}</td>
      <td>${escapeHtml(a.professionalName)}</td>
      <td>${escapeHtml(a.specialtyName ?? '—')}</td>
      <td>${escapeHtml(a.room ?? '—')}</td>
      <td>${escapeHtml(appointmentStatusLabel(a.status))}</td>
    </tr>`,
  ).join('') || '<tr><td colspan="6" class="pr-empty">Nenhum agendamento para esta data.</td></tr>';

  const kpiRows: [string, string][] = [
    ['Data', dateLabel],
    ['Total do dia', String(stats?.total ?? rows.length)],
  ];
  if (stats?.confirmed != null) kpiRows.push(['Confirmados', String(stats.confirmed)]);
  if (stats?.waiting != null) kpiRows.push(['Aguardando', String(stats.waiting)]);
  if (stats?.inProgress != null) kpiRows.push(['Em atendimento', String(stats.inProgress)]);
  if (stats?.done != null) kpiRows.push(['Concluídos', String(stats.done)]);

  const body = buildProfessionalReportBody({
    title: `Agenda do dia — ${dateLabel}`,
    documentType: 'Relatório gerencial ambulatorial',
    code: `agenda.${date}`,
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Data', value: dateLabel },
      { label: 'Agendamentos', value: String(rows.length) },
    ],
    sections: [
      { title: 'Indicadores', html: moneyRows(kpiRows) },
      {
        title: 'Agendamentos',
        html: tableHtml(
          ['Horário', 'Paciente', 'Profissional', 'Especialidade', 'Sala', 'Status'],
          tableRows,
        ),
      },
    ],
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: `Agenda ${dateLabel}`, body, pageSize: 'report' });
}

function formatBrTimeCell(iso: string) {
  try {
    return new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
  } catch {
    return '—';
  }
}

/** Relatório gerencial — fila do Pronto-Socorro. */
export function printEmergencyQueueSummary(
  visits: EmergencyQueuePrintVisit[] | DashboardEmergencyItemDto[],
  stats?: {
    waiting?: number;
    inCare?: number;
    discharged?: number;
    critical?: number;
    total?: number;
    avgWaitMinutes?: number;
    slaViolations?: number;
  },
) {
  const rows = normalizeEmergencyRows(visits);
  const sorted = [...rows].sort(
    (a, b) => new Date(a.arrivedAt).getTime() - new Date(b.arrivedAt).getTime(),
  );

  const tableRows = sorted.map((v) =>
    `<tr>
      <td>${formatDateTime(v.arrivedAt)}</td>
      <td>${escapeHtml(v.patientName)}</td>
      <td>${escapeHtml(v.chiefComplaint)}</td>
      <td>${escapeHtml(urgencyPrintLabel(v.urgency))}</td>
      <td>${escapeHtml(emergencyStatusPrintLabel(v.status))}</td>
      <td>${escapeHtml(v.professionalName ?? '—')}</td>
    </tr>`,
  ).join('') || '<tr><td colspan="6" class="pr-empty">Nenhum paciente na fila do PS.</td></tr>';

  const kpiRows: [string, string][] = [
    ['Total na fila', String(stats?.total ?? rows.length)],
    ['Aguardando', String(stats?.waiting ?? rows.filter((v) => v.status === 'Waiting').length)],
  ];
  if (stats?.inCare != null) kpiRows.push(['Em atendimento', String(stats.inCare)]);
  if (stats?.discharged != null) kpiRows.push(['Altas', String(stats.discharged)]);
  if (stats?.critical != null) kpiRows.push(['Urgência alta / emergência', String(stats.critical)]);
  if (stats?.avgWaitMinutes != null) kpiRows.push(['Tempo médio de espera (min)', String(stats.avgWaitMinutes)]);
  if (stats?.slaViolations != null) kpiRows.push(['SLA ultrapassado', String(stats.slaViolations)]);

  const body = buildProfessionalReportBody({
    title: 'Resumo da fila — Pronto-Socorro',
    documentType: 'Relatório gerencial PS',
    code: `ps.fila.${new Date().toISOString().slice(0, 10)}`,
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Pacientes listados', value: String(rows.length) },
      { label: 'Aguardando triagem/atendimento', value: String(stats?.waiting ?? '—') },
    ],
    sections: [
      { title: 'Indicadores', html: moneyRows(kpiRows) },
      {
        title: 'Fila ordenada por chegada',
        html: tableHtml(
          ['Chegada', 'Paciente', 'Queixa', 'Urgência', 'Status', 'Profissional'],
          tableRows,
        ),
      },
    ],
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: 'Fila PS — Resumo', body, pageSize: 'report' });
}

/** Relatório gerencial — internação hospitalar (hub ou painel operacional). */
export function printHospitalizationSummary(
  dashboard: HospitalizationHubDashboardDto,
  options: {
    dateFrom?: string;
    dateTo?: string;
    items?: HospitalizationHubListItemDto[];
  } = {},
) {
  const period = options.dateFrom && options.dateTo
    ? `${formatDate(options.dateFrom)} a ${formatDate(options.dateTo)}`
    : 'Situação atual';

  const sliceTable = (title: string, slices: { label: string; count: number }[]) => {
    if (!slices.length) return null;
    const rows = slices.map((s) =>
      `<tr><td>${escapeHtml(s.label)}</td><td>${s.count}</td></tr>`,
    ).join('');
    return { title, html: tableHtml(['Grupo', 'Quantidade'], rows) };
  };

  const sections: { title: string; html: string }[] = [
    {
      title: 'Indicadores',
      html: moneyRows([
        ['Internações ativas', String(dashboard.activeCount)],
        ['Leitos disponíveis', String(dashboard.availableBeds)],
        ['Leitos ocupados', String(dashboard.occupiedBeds)],
        ['Leitos bloqueados', String(dashboard.blockedBeds)],
        ['Solicitações pendentes', String(dashboard.pendingRequests)],
        ['Altas no período', String(dashboard.dischargedInPeriod)],
        ['Permanência média (dias)', dashboard.avgLengthOfStayDays != null ? String(dashboard.avgLengthOfStayDays) : '—'],
      ]),
    },
  ];

  const wardSection = sliceTable('Por ala / setor', dashboard.byWard);
  if (wardSection) sections.push(wardSection);
  const modalitySection = sliceTable('Por modalidade', dashboard.byModality);
  if (modalitySection) sections.push(modalitySection);
  const profSection = sliceTable('Por médico responsável', dashboard.byProfessional);
  if (profSection) sections.push(profSection);

  if (options.items && options.items.length > 0) {
    const itemRows = options.items.slice(0, 40).map((item) =>
      `<tr>
        <td>${escapeHtml(item.patientName)}</td>
        <td>${escapeHtml(item.wardName ?? '—')}</td>
        <td>${escapeHtml(item.bedNumber ?? '—')}</td>
        <td>${escapeHtml(item.professionalName ?? '—')}</td>
        <td>${formatDateTime(item.eventAt)}</td>
        <td>${escapeHtml(item.statusLabel)}</td>
      </tr>`,
    ).join('');
    sections.push({
      title: `Internações e solicitações (${Math.min(options.items.length, 40)} de ${options.items.length})`,
      html: tableHtml(
        ['Paciente', 'Ala', 'Leito', 'Profissional', 'Data', 'Status'],
        itemRows,
      ),
    });
  }

  const body = buildProfessionalReportBody({
    title: 'Resumo de internação hospitalar',
    documentType: 'Relatório gerencial de internação',
    code: `internacao.${options.dateTo ?? new Date().toISOString().slice(0, 10)}`,
    layoutKind: 'bi-managerial',
    meta: [
      { label: 'Período', value: period },
      { label: 'Internações ativas', value: String(dashboard.activeCount) },
      { label: 'Ocupação', value: `${dashboard.occupiedBeds} / ${dashboard.occupiedBeds + dashboard.availableBeds} leitos` },
    ],
    sections,
    showSignature: true,
    generatedAt: new Date().toISOString(),
  });

  printDocument({ title: 'Internação — Resumo', body, pageSize: 'report' });
}

/** Resumo de internação a partir do dashboard executivo operacional. */
export function printOperationalHospitalizationSummary(data: OperationalDashboardDto) {
  printHospitalizationSummary({
    activeCount: data.activeHospitalizations,
    availableBeds: data.availableBeds,
    occupiedBeds: data.occupiedBeds,
    blockedBeds: data.maintenanceBeds + data.cleaningBeds,
    pendingRequests: 0,
    dischargedInPeriod: 0,
    avgLengthOfStayDays: null,
    byWard: [],
    byModality: [],
    byProfessional: [],
  });
}

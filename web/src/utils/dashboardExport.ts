import type { OperationalDashboardDto } from '../api/client';

function escapeCsv(value: string | number) {
  const text = String(value);
  if (text.includes(',') || text.includes('"') || text.includes('\n')) {
    return `"${text.replace(/"/g, '""')}"`;
  }
  return text;
}

export function exportDashboardCsv(data: OperationalDashboardDto) {
  const rows: (string | number)[][] = [
    ['Indicador', 'Valor'],
    ['Pacientes cadastrados', data.totalPatients],
    ['Atendimentos hoje', data.attendancesToday],
    ['Agendamentos hoje', data.appointmentsToday],
    ['Internações ativas', data.activeHospitalizations],
    ['Cirurgias hoje', data.surgeriesToday],
    ['Leitos ocupados', `${data.occupiedBeds}/${data.totalBeds}`],
    ['Ocupação (%)', data.bedOccupancyRate],
    ['Receita do dia', data.revenueToday],
    ['Receita do mês', data.revenueThisMonth],
    ['A receber em aberto', data.revenuePending],
    ['Títulos a receber', data.financialAccountsOpen],
    ['A pagar em aberto', data.payablePending],
    ['Títulos a pagar', data.payableAccountsOpen],
    ['Pago no mês', data.expenseThisMonth],
    ['Vencidos a receber', data.overdueReceivable],
    ['Vencidos a pagar', data.overduePayable],
    ['PS aguardando', data.emergencyWaiting],
    ['SLA violado (PS)', data.emergencySlaViolations],
    ['Estoque baixo', data.lowStockProducts],
    ['Falhas integração', data.integrationFailures],
    ['Gerado em', data.generatedAt],
  ];

  const csv = rows.map((row) => row.map(escapeCsv).join(',')).join('\n');
  const blob = new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `dashboard-executivo-${new Date().toISOString().slice(0, 10)}.csv`;
  link.click();
  URL.revokeObjectURL(url);
}

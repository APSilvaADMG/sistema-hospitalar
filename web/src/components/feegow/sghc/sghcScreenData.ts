import {
  api,
  bedStatusLabel,
  bloodComponentLabels,
  bloodTypeLabels,
  bloodUnitStatusLabels,
  financialStatusLabel,
  formatAppointmentStatus,
  hospitalizationStatusLabel,
  isBedAvailable,
  productTypeLabels,
  roleLabels,
} from '../../../api/client';
import { formatBrDate, formatBrDateTime } from '../../../utils/dateUtils';

export type SghcDataModule =
  | 'financial'
  | 'beds'
  | 'hospitalizations'
  | 'blood'
  | 'patients'
  | 'appointments'
  | 'medicines'
  | 'dispensings'
  | 'staff'
  | 'wards'
  | 'audit';

export type SghcColumn = { key: string; label: string };

export type SghcRow = {
  id: string;
  cells: Record<string, string>;
  link?: string;
};

export type SghcDataResult = {
  module: SghcDataModule | null;
  columns: SghcColumn[];
  rows: SghcRow[];
  summary?: string;
  moduleLink?: string | null;
};

const MODULE_LINKS: Record<SghcDataModule, string> = {
  financial: '/financeiro',
  beds: '/internacao/leitos',
  hospitalizations: '/internacao',
  blood: '/hemoterapia',
  patients: '/recepcao/pacientes',
  appointments: '/recepcao/agendamentos',
  medicines: '/farmacia',
  dispensings: '/farmacia',
  staff: '/usuarios',
  wards: '/internacao/leitos',
  audit: '/auditoria',
};

const MODULE_COLUMNS: Record<SghcDataModule, SghcColumn[]> = {
  financial: [
    { key: 'invoice', label: 'Fatura' },
    { key: 'patient', label: 'Paciente' },
    { key: 'amount', label: 'Valor' },
    { key: 'balance', label: 'Saldo' },
    { key: 'due', label: 'Vencimento' },
    { key: 'status', label: 'Status' },
  ],
  beds: [
    { key: 'ward', label: 'Ala' },
    { key: 'bed', label: 'Leito' },
    { key: 'status', label: 'Status' },
    { key: 'patient', label: 'Paciente' },
    { key: 'professional', label: 'Profissional' },
  ],
  hospitalizations: [
    { key: 'patient', label: 'Paciente' },
    { key: 'bed', label: 'Leito' },
    { key: 'ward', label: 'Ala' },
    { key: 'admitted', label: 'Internação' },
    { key: 'status', label: 'Status' },
  ],
  blood: [
    { key: 'code', label: 'Unidade' },
    { key: 'type', label: 'Tipo sanguíneo' },
    { key: 'component', label: 'Componente' },
    { key: 'expires', label: 'Validade' },
    { key: 'status', label: 'Status' },
  ],
  patients: [
    { key: 'name', label: 'Paciente' },
    { key: 'cpf', label: 'CPF' },
    { key: 'phone', label: 'Telefone' },
    { key: 'birth', label: 'Nascimento' },
  ],
  appointments: [
    { key: 'patient', label: 'Paciente' },
    { key: 'professional', label: 'Profissional' },
    { key: 'specialty', label: 'Especialidade' },
    { key: 'when', label: 'Data/hora' },
    { key: 'status', label: 'Status' },
  ],
  medicines: [
    { key: 'name', label: 'Medicamento' },
    { key: 'sku', label: 'SKU' },
    { key: 'type', label: 'Tipo' },
    { key: 'stock', label: 'Estoque' },
  ],
  dispensings: [
    { key: 'patient', label: 'Paciente' },
    { key: 'product', label: 'Medicamento' },
    { key: 'qty', label: 'Qtd' },
    { key: 'when', label: 'Dispensado em' },
    { key: 'professional', label: 'Profissional' },
  ],
  staff: [
    { key: 'name', label: 'Nome' },
    { key: 'email', label: 'E-mail' },
    { key: 'role', label: 'Perfil' },
    { key: 'status', label: 'Status' },
  ],
  wards: [
    { key: 'name', label: 'Departamento' },
    { key: 'code', label: 'Código' },
    { key: 'beds', label: 'Leitos' },
    { key: 'available', label: 'Disponíveis' },
  ],
  audit: [
    { key: 'when', label: 'Data' },
    { key: 'user', label: 'Usuário' },
    { key: 'action', label: 'Ação' },
    { key: 'entity', label: 'Entidade' },
  ],
};

function money(value: number): string {
  return value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function resolveSghcDataModule(route: string): SghcDataModule | null {
  const r = route.toLowerCase();

  if (r.includes('invoice') || r.includes('payment') || r.includes('cash_payment')) {
    return 'financial';
  }
  if (r.includes('bed_allotment') || r.includes('admit_history') || r.includes('operation_history')) {
    return 'hospitalizations';
  }
  if (r.includes('manage_bed') || r.includes('view_bed') || r.includes('bed_list')) {
    return 'beds';
  }
  if (r.includes('blood')) {
    return 'blood';
  }
  if (r.includes('manage_patient') || r.includes('view_doctor')) {
    return 'patients';
  }
  if (r.includes('appointment')) {
    return 'appointments';
  }
  if (r.includes('medicine_category')) {
    return 'medicines';
  }
  if (r.includes('medicine') || r.includes('view_medicine')) {
    return 'medicines';
  }
  if (r.includes('prescription')) {
    return 'dispensings';
  }
  if (r.includes('manage_department')) {
    return 'wards';
  }
  if (r.includes('view_log')) {
    return 'audit';
  }
  if (
    r.includes('manage_doctor')
    || r.includes('manage_nurse')
    || r.includes('manage_pharmacist')
    || r.includes('manage_laboratorist')
    || r.includes('manage_accountant')
  ) {
    return 'staff';
  }

  return null;
}

function staffRoleFilter(route: string): string[] | null {
  const r = route.toLowerCase();
  if (r.includes('manage_doctor')) return ['Doctor'];
  if (r.includes('manage_nurse')) return ['Nurse', 'NursingTechnician'];
  if (r.includes('manage_pharmacist')) return ['Pharmacy'];
  if (r.includes('manage_laboratorist')) return ['Reception', 'IT'];
  if (r.includes('manage_accountant')) return ['Billing'];
  return null;
}

export async function loadSghcScreenData(route: string): Promise<SghcDataResult> {
  const module = resolveSghcDataModule(route);
  if (!module) {
    return { module: null, columns: [], rows: [] };
  }

  const columns = MODULE_COLUMNS[module];
  const moduleLink = MODULE_LINKS[module];

  switch (module) {
    case 'financial': {
      const page = await api.getFinancialAccounts(undefined, undefined, 1);
      const rows = page.items.map((item, index) => ({
        id: item.id,
        link: `/financeiro?conta=${item.id}`,
        cells: {
          invoice: item.invoiceNumber ?? `#${index + 1}`,
          patient: item.counterpartyDisplay,
          amount: money(item.amount),
          balance: money(item.balance),
          due: item.dueDate ? formatBrDate(item.dueDate) : '—',
          status: financialStatusLabel(item.status),
        },
      }));
      const summary = await api.getFinancialSummary().catch(() => null);
      return {
        module,
        columns,
        rows,
        moduleLink,
        summary: summary
          ? `A receber em aberto: ${money(summary.receivableOpen)} · A pagar: ${money(summary.payableOpen)}`
          : `${rows.length} conta(s) carregada(s)`,
      };
    }
    case 'beds': {
      const beds = await api.getBeds();
      return {
        module,
        columns,
        moduleLink,
        rows: beds.map((bed) => ({
          id: bed.id,
          link: '/internacao/leitos',
          cells: {
            ward: bed.wardName,
            bed: bed.bedNumber,
            status: bedStatusLabel(bed.status),
            patient: bed.occupantPatientName ?? '—',
            professional: bed.occupantProfessionalName ?? '—',
          },
        })),
        summary: `${beds.filter((b) => isBedAvailable(b.status)).length} leito(s) disponível(is)`,
      };
    }
    case 'hospitalizations': {
      const list = await api.getHospitalizations(undefined, 'active');
      return {
        module,
        columns,
        moduleLink,
        rows: list.map((item) => ({
          id: item.id,
          link: `/internacao?internacao=${item.id}`,
          cells: {
            patient: item.patientName,
            bed: item.bedNumber,
            ward: item.wardName,
            admitted: formatBrDateTime(item.admittedAt),
            status: hospitalizationStatusLabel(item.status),
          },
        })),
        summary: `${list.length} internação(ões) ativa(s)`,
      };
    }
    case 'blood': {
      const units = await api.getBloodUnits();
      return {
        module,
        columns,
        moduleLink,
        rows: units.map((unit) => ({
          id: unit.id,
          link: '/hemoterapia',
          cells: {
            code: unit.unitCode,
            type: bloodTypeLabels[unit.bloodType] ?? unit.bloodType,
            component: bloodComponentLabels[unit.component] ?? unit.component,
            expires: formatBrDate(unit.expiresAt),
            status: bloodUnitStatusLabels[unit.status] ?? unit.status,
          },
        })),
        summary: `${units.filter((u) => u.status === 'Available').length} unidade(s) disponível(is)`,
      };
    }
    case 'patients': {
      const page = await api.getPatients('', 1, 80);
      return {
        module,
        columns,
        moduleLink,
        rows: page.items.map((patient) => ({
          id: patient.id,
          link: `/recepcao/pacientes/${patient.id}/prontuario/dados-principais`,
          cells: {
            name: patient.fullName,
            cpf: patient.cpf,
            phone: patient.mobilePhone ?? patient.phone ?? '—',
            birth: formatBrDate(patient.birthDate),
          },
        })),
        summary: `${page.totalCount} paciente(s) no cadastro`,
      };
    }
    case 'appointments': {
      const list = await api.getAppointments();
      return {
        module,
        columns,
        moduleLink,
        rows: list.slice(0, 80).map((item) => ({
          id: item.id,
          link: '/recepcao/agendamentos',
          cells: {
            patient: item.patientName,
            professional: item.professionalName,
            specialty: item.specialtyName,
            when: formatBrDateTime(item.scheduledAt),
            status: formatAppointmentStatus(item.status),
          },
        })),
        summary: `${list.length} agendamento(s)`,
      };
    }
    case 'medicines': {
      const products = await api.getProducts('', false, 1);
      return {
        module,
        columns,
        moduleLink,
        rows: products.slice(0, 80).map((product) => ({
          id: product.id,
          link: '/farmacia',
          cells: {
            name: product.name,
            sku: product.sku ?? '—',
            type: productTypeLabels[product.type] ?? '—',
            stock: String(product.quantityOnHand),
          },
        })),
        summary: `${products.length} item(ns) de estoque/farmácia`,
      };
    }
    case 'dispensings': {
      const list = await api.getDispensings();
      return {
        module,
        columns,
        moduleLink,
        rows: list.slice(0, 80).map((item) => ({
          id: item.id,
          link: '/farmacia',
          cells: {
            patient: item.patientName,
            product: item.productName,
            qty: String(item.quantity),
            when: formatBrDateTime(item.dispensedAt),
            professional: item.professionalName ?? '—',
          },
        })),
        summary: `${list.length} dispensação(ões)`,
      };
    }
    case 'staff': {
      const roles = staffRoleFilter(route);
      const users = await api.getUsers();
      const filtered = roles
        ? users.filter((user) => roles.includes(user.role))
        : users;
      return {
        module,
        columns,
        moduleLink,
        rows: filtered.map((user) => ({
          id: user.id,
          link: '/usuarios',
          cells: {
            name: user.fullName,
            email: user.email,
            role: roleLabels[user.role] ?? user.role,
            status: user.isActive ? 'Ativo' : 'Inativo',
          },
        })),
        summary: `${filtered.length} usuário(s)`,
      };
    }
    case 'wards': {
      const wards = await api.getWards();
      return {
        module,
        columns,
        moduleLink,
        rows: wards.map((ward) => ({
          id: ward.id,
          link: '/internacao/leitos',
          cells: {
            name: ward.name,
            code: ward.code ?? '—',
            beds: String(ward.totalBeds),
            available: String(ward.availableBeds),
          },
        })),
        summary: `${wards.length} departamento(s)/ala(s)`,
      };
    }
    case 'audit': {
      const logs = await api.getAuditLogs(60);
      return {
        module,
        columns,
        moduleLink,
        rows: logs.map((log) => ({
          id: log.id,
          link: '/auditoria',
          cells: {
            when: formatBrDateTime(log.createdAt),
            user: log.userEmail ?? '—',
            action: log.action,
            entity: log.entityType,
          },
        })),
        summary: `${logs.length} evento(s) recentes`,
      };
    }
    default:
      return { module: null, columns: [], rows: [] };
  }
}

export function mergeSghcColumns(
  module: SghcDataModule,
  bayannoColumns: { label: string; labelKey: string }[],
): SghcColumn[] {
  const defaults = MODULE_COLUMNS[module];
  if (bayannoColumns.length === 0) return defaults;
  return bayannoColumns
    .filter((col) => col.labelKey !== 'option')
    .map((col, index) => ({
      key: defaults[index]?.key ?? col.labelKey,
      label: col.label !== '#' ? col.label : defaults[index]?.label ?? col.label,
    }));
}

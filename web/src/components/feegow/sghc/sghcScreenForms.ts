import {
  api,
  bloodComponentLabels,
  bloodTypeLabels,
  roleLabels,
  type UserRoleName,
} from '../../../api/client';
import { addDaysIso } from '../../../utils/dateUtils';
import {
  resolveSghcDataModule,
  type SghcDataModule,
} from './sghcScreenData';

export type SghcFormMode = 'add' | 'edit';

export type SghcFormFieldType =
  | 'text'
  | 'email'
  | 'password'
  | 'number'
  | 'date'
  | 'datetime-local'
  | 'textarea'
  | 'select';

export type SghcDynamicOptionSource = 'patients' | 'wards' | 'products' | 'professionals';

export type SghcFormField = {
  key: string;
  label: string;
  type: SghcFormFieldType;
  required?: boolean;
  placeholder?: string;
  defaultValue?: string;
  options?: { value: string; label: string }[];
  optionSource?: SghcDynamicOptionSource;
};

export type SghcFormDefinition = {
  module: SghcDataModule;
  mode: SghcFormMode;
  title: string;
  hint?: string;
  redirectOnly?: boolean;
  fields: SghcFormField[];
};

const MODULE_BASE_LINKS: Record<SghcDataModule, string> = {
  financial: '/financeiro',
  beds: '/internacao/leitos',
  hospitalizations: '/internacao',
  blood: '/hemoterapia',
  patients: '/recepcao/pacientes',
  appointments: '/recepcao/agendamentos',
  medicines: '/estoque/insumos/listar',
  dispensings: '/farmacia',
  staff: '/usuarios',
  wards: '/internacao/leitos',
  audit: '/auditoria',
};

function financialRoutePaths(route: string) {
  const r = route.toLowerCase();
  if (r.includes('payment') || r.includes('cash')) {
    return {
      insert: '/financeiro/contas-a-pagar/inserir',
      list: '/financeiro/contas-a-pagar/listar',
    };
  }
  return {
    insert: '/financeiro/contas-a-receber/inserir',
    list: '/financeiro/contas-a-receber/listar',
  };
}

function staffDefaultRole(route: string): UserRoleName {
  const r = route.toLowerCase();
  if (r.includes('manage_doctor')) return 'Doctor';
  if (r.includes('manage_nurse')) return 'Nurse';
  if (r.includes('manage_pharmacist')) return 'Pharmacy';
  if (r.includes('manage_laboratorist')) return 'Reception';
  if (r.includes('manage_accountant')) return 'Billing';
  return 'Reception';
}

export function resolveSghcFormMode(tabId: string): SghcFormMode | null {
  const id = tabId.toLowerCase();
  if (id === 'add' || id.includes('add')) return 'add';
  if (id === 'edit' || id.includes('edit')) return 'edit';
  return null;
}

export function resolveSghcModuleDeepLink(
  module: SghcDataModule,
  mode: SghcFormMode,
  route: string,
  recordId?: string,
): string {
  if (recordId) {
    switch (module) {
      case 'financial': {
        const { list } = financialRoutePaths(route);
        return `${list}?conta=${encodeURIComponent(recordId)}`;
      }
      case 'patients':
        return `/recepcao/pacientes/${recordId}/prontuario/dados-principais`;
      case 'appointments':
        return `${MODULE_BASE_LINKS.appointments}?agendamento=${encodeURIComponent(recordId)}`;
      case 'beds':
        return `${MODULE_BASE_LINKS.beds}?leito=${encodeURIComponent(recordId)}`;
      case 'hospitalizations':
        return `${MODULE_BASE_LINKS.hospitalizations}?internacao=${encodeURIComponent(recordId)}`;
      case 'blood':
        return `${MODULE_BASE_LINKS.blood}?unidade=${encodeURIComponent(recordId)}`;
      case 'medicines':
        return `/estoque/insumos/inserir?id=${encodeURIComponent(recordId)}`;
      case 'staff':
        return `${MODULE_BASE_LINKS.staff}?usuario=${encodeURIComponent(recordId)}`;
      case 'wards':
        return `${MODULE_BASE_LINKS.wards}?ala=${encodeURIComponent(recordId)}`;
      case 'dispensings':
        return `${MODULE_BASE_LINKS.dispensings}?dispensar=1`;
      case 'audit':
        return MODULE_BASE_LINKS.audit;
      default:
        return MODULE_BASE_LINKS[module];
    }
  }

  if (mode === 'add') {
    switch (module) {
      case 'financial':
        return financialRoutePaths(route).insert;
      case 'patients':
        return `${MODULE_BASE_LINKS.patients}?novo=1`;
      case 'appointments':
        return `${MODULE_BASE_LINKS.appointments}?novo=1`;
      case 'beds':
        return `${MODULE_BASE_LINKS.beds}?novo=leito`;
      case 'wards':
        return `${MODULE_BASE_LINKS.wards}?novo=ala`;
      case 'hospitalizations':
        return `${MODULE_BASE_LINKS.hospitalizations}?novo=1`;
      case 'blood':
        return `${MODULE_BASE_LINKS.blood}?novo=1`;
      case 'medicines':
        return '/estoque/insumos/inserir?tipo=1';
      case 'dispensings':
        return `${MODULE_BASE_LINKS.dispensings}?dispensar=1`;
      case 'staff':
        return `${MODULE_BASE_LINKS.staff}?novo=1&role=${staffDefaultRole(route)}`;
      case 'audit':
        return MODULE_BASE_LINKS.audit;
      default:
        return MODULE_BASE_LINKS[module];
    }
  }

  return MODULE_BASE_LINKS[module];
}

export function getSghcFormDefinition(
  module: SghcDataModule,
  mode: SghcFormMode,
  route: string,
): SghcFormDefinition {
  if (mode === 'edit') {
    return {
      module,
      mode,
      title: 'Editar no módulo Feegow',
      hint: 'Informe o identificador do registro (copie da aba Listagem → Abrir) ou continue no cadastro completo.',
      fields: [
        {
          key: 'recordId',
          label: 'ID do registro',
          type: 'text',
          placeholder: 'UUID ou código exibido na listagem',
        },
      ],
    };
  }

  switch (module) {
    case 'patients':
      return {
        module,
        mode,
        title: 'Novo paciente',
        hint: 'Cadastro rápido. Dados completos no prontuário Feegow.',
        fields: [
          { key: 'fullName', label: 'Nome completo', type: 'text', required: true },
          { key: 'cpf', label: 'CPF', type: 'text', required: true, placeholder: '000.000.000-00' },
          { key: 'birthDate', label: 'Nascimento', type: 'date', required: true },
          {
            key: 'gender',
            label: 'Sexo',
            type: 'select',
            required: true,
            defaultValue: '1',
            options: [{ value: '1', label: 'Masculino' }, { value: '2', label: 'Feminino' }],
          },
          { key: 'mobilePhone', label: 'Celular', type: 'text' },
        ],
      };
    case 'appointments':
      return {
        module,
        mode,
        title: 'Novo agendamento',
        fields: [
          { key: 'patientId', label: 'Paciente', type: 'select', required: true, optionSource: 'patients' },
          { key: 'professionalId', label: 'Profissional', type: 'select', required: true, optionSource: 'professionals' },
          { key: 'scheduledAt', label: 'Data e hora', type: 'datetime-local', required: true },
          { key: 'durationMinutes', label: 'Duração (min)', type: 'number', required: true, defaultValue: '30' },
          { key: 'reason', label: 'Motivo', type: 'text' },
        ],
      };
    case 'financial':
      return {
        module,
        mode,
        title: route.toLowerCase().includes('payment') ? 'Nova conta a pagar' : 'Nova conta a receber',
        fields: [
          { key: 'patientId', label: 'Paciente', type: 'select', required: true, optionSource: 'patients' },
          { key: 'description', label: 'Descrição', type: 'text', required: true },
          { key: 'amount', label: 'Valor (R$)', type: 'number', required: true },
          { key: 'dueDate', label: 'Vencimento', type: 'date' },
          { key: 'invoiceNumber', label: 'Nº fatura', type: 'text' },
        ],
      };
    case 'beds':
      return {
        module,
        mode,
        title: 'Novo leito',
        fields: [
          { key: 'wardId', label: 'Ala / departamento', type: 'select', required: true, optionSource: 'wards' },
          { key: 'bedNumber', label: 'Número do leito', type: 'text', required: true },
        ],
      };
    case 'wards':
      return {
        module,
        mode,
        title: 'Novo departamento / ala',
        fields: [
          { key: 'name', label: 'Nome', type: 'text', required: true },
          { key: 'code', label: 'Código', type: 'text' },
          {
            key: 'category',
            label: 'Categoria',
            type: 'select',
            required: true,
            defaultValue: '1',
            options: Object.entries({
              1: 'Enfermaria',
              2: 'Apartamento',
              3: 'UTI',
              4: 'Pediatria',
              5: 'Maternidade',
            }).map(([value, label]) => ({ value, label })),
          },
        ],
      };
    case 'blood':
      return {
        module,
        mode,
        title: 'Nova unidade de sangue',
        fields: [
          { key: 'unitCode', label: 'Código da unidade', type: 'text', required: true },
          {
            key: 'bloodType',
            label: 'Tipo sanguíneo',
            type: 'select',
            required: true,
            options: Object.entries(bloodTypeLabels).map(([value, label]) => ({ value, label })),
          },
          {
            key: 'component',
            label: 'Componente',
            type: 'select',
            required: true,
            options: Object.entries(bloodComponentLabels).map(([value, label]) => ({ value, label })),
          },
          { key: 'volumeMl', label: 'Volume (ml)', type: 'number', required: true, defaultValue: '450' },
          { key: 'collectedAt', label: 'Coleta', type: 'date', required: true, defaultValue: new Date().toISOString().slice(0, 10) },
          { key: 'expiresAt', label: 'Validade', type: 'date', required: true, defaultValue: addDaysIso(35) },
        ],
      };
    case 'medicines':
      return {
        module,
        mode,
        title: 'Novo medicamento',
        fields: [
          { key: 'name', label: 'Nome', type: 'text', required: true },
          { key: 'sku', label: 'SKU', type: 'text', required: true },
          { key: 'unit', label: 'Unidade', type: 'text', required: true, defaultValue: 'UN' },
          { key: 'minimumStock', label: 'Estoque mínimo', type: 'number', required: true, defaultValue: '10' },
        ],
      };
    case 'dispensings':
      return {
        module,
        mode,
        title: 'Dispensar medicamento',
        fields: [
          { key: 'patientId', label: 'Paciente', type: 'select', required: true, optionSource: 'patients' },
          { key: 'productId', label: 'Medicamento', type: 'select', required: true, optionSource: 'products' },
          { key: 'quantity', label: 'Quantidade', type: 'number', required: true, defaultValue: '1' },
          { key: 'notes', label: 'Observações', type: 'textarea' },
        ],
      };
    case 'staff': {
      const role = staffDefaultRole(route);
      return {
        module,
        mode,
        title: `Novo usuário (${roleLabels[role] ?? role})`,
        fields: [
          { key: 'fullName', label: 'Nome', type: 'text', required: true },
          { key: 'email', label: 'E-mail', type: 'email', required: true },
          { key: 'password', label: 'Senha inicial', type: 'password', required: true },
          {
            key: 'role',
            label: 'Perfil',
            type: 'select',
            required: true,
            defaultValue: role,
            options: [{ value: role, label: roleLabels[role] ?? role }],
          },
        ],
      };
    }
    case 'hospitalizations':
      return {
        module,
        mode,
        title: 'Nova internação',
        redirectOnly: true,
        hint: 'A internação exige leito, médico e fluxo clínico. Continue no módulo de Internação.',
        fields: [],
      };
    case 'audit':
      return {
        module,
        mode,
        title: 'Auditoria',
        redirectOnly: true,
        hint: 'Logs de auditoria são somente leitura.',
        fields: [],
      };
    default:
      return {
        module,
        mode,
        title: 'Cadastro',
        redirectOnly: true,
        fields: [],
      };
  }
}

export function buildSghcFormDefaults(definition: SghcFormDefinition): Record<string, string> {
  const values: Record<string, string> = {};
  for (const field of definition.fields) {
    if (field.defaultValue != null) values[field.key] = field.defaultValue;
    else values[field.key] = '';
  }
  return values;
}

export async function submitSghcForm(
  module: SghcDataModule,
  route: string,
  values: Record<string, string>,
): Promise<{ message: string; recordId?: string; navigateTo?: string }> {
  switch (module) {
    case 'patients': {
      const created = await api.createPatient({
        fullName: values.fullName.trim(),
        cpf: values.cpf.replace(/\D/g, ''),
        birthDate: values.birthDate,
        gender: Number(values.gender) || 1,
        mobilePhone: values.mobilePhone || undefined,
      });
      return {
        message: 'Paciente cadastrado com sucesso.',
        recordId: created.patient.id,
        navigateTo: `/recepcao/pacientes/${created.patient.id}/prontuario/dados-principais`,
      };
    }
    case 'appointments': {
      const created = await api.createAppointment({
        patientId: values.patientId,
        professionalId: values.professionalId,
        scheduledAt: new Date(values.scheduledAt).toISOString(),
        durationMinutes: Number(values.durationMinutes) || 30,
        reason: values.reason || undefined,
      });
      return {
        message: 'Agendamento criado.',
        recordId: created.appointment.id,
        navigateTo: `${MODULE_BASE_LINKS.appointments}?agendamento=${created.appointment.id}`,
      };
    }
    case 'financial': {
      const payable = route.toLowerCase().includes('payment') || route.toLowerCase().includes('cash');
      const created = await api.createFinancialAccount({
        direction: payable ? 2 : 1,
        patientId: values.patientId,
        category: payable ? 7 : 1,
        description: values.description.trim(),
        amount: Number(values.amount),
        dueDate: values.dueDate || undefined,
        invoiceNumber: values.invoiceNumber || undefined,
      });
      const { list } = financialRoutePaths(route);
      return {
        message: 'Conta financeira registrada.',
        recordId: created.id,
        navigateTo: `${list}?conta=${created.id}`,
      };
    }
    case 'beds': {
      const created = await api.createBed({
        wardId: values.wardId,
        bedNumber: values.bedNumber.trim(),
      });
      return {
        message: 'Leito cadastrado.',
        recordId: created.id,
        navigateTo: `${MODULE_BASE_LINKS.beds}?leito=${created.id}`,
      };
    }
    case 'wards': {
      const created = await api.createWard({
        name: values.name.trim(),
        code: values.code || undefined,
        coverageModality: 1,
        category: Number(values.category) || 1,
      });
      return {
        message: 'Departamento/ala cadastrado.',
        recordId: created.id,
        navigateTo: `${MODULE_BASE_LINKS.wards}?ala=${created.id}`,
      };
    }
    case 'blood': {
      const created = await api.createBloodUnit({
        unitCode: values.unitCode.trim(),
        bloodType: values.bloodType,
        component: values.component,
        volumeMl: Number(values.volumeMl) || 450,
        collectedAt: values.collectedAt,
        expiresAt: values.expiresAt,
      });
      return {
        message: 'Unidade de sangue registrada.',
        recordId: created.id,
        navigateTo: `${MODULE_BASE_LINKS.blood}?unidade=${created.id}`,
      };
    }
    case 'medicines': {
      const created = await api.createProduct({
        name: values.name.trim(),
        sku: values.sku.trim(),
        type: 1,
        unit: values.unit.trim() || 'UN',
        minimumStock: Number(values.minimumStock) || 0,
      });
      return {
        message: 'Medicamento cadastrado no estoque.',
        recordId: created.id,
        navigateTo: `/estoque/insumos/inserir?id=${created.id}`,
      };
    }
    case 'dispensings': {
      await api.dispenseMedication({
        patientId: values.patientId,
        productId: values.productId,
        quantity: Number(values.quantity) || 1,
        notes: values.notes || undefined,
      });
      return {
        message: 'Dispensação registrada.',
        navigateTo: `${MODULE_BASE_LINKS.dispensings}?dispensar=1`,
      };
    }
    case 'staff': {
      const created = await api.createUser({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        password: values.password,
        role: (values.role || staffDefaultRole(route)) as UserRoleName,
      });
      return {
        message: 'Usuário criado.',
        recordId: created.id,
        navigateTo: `${MODULE_BASE_LINKS.staff}?usuario=${created.id}`,
      };
    }
    default:
      throw new Error('Este cadastro deve ser feito no módulo Feegow completo.');
  }
}

export function resolveSghcFormContext(route: string, tabId: string) {
  const module = resolveSghcDataModule(route);
  const mode = resolveSghcFormMode(tabId);
  if (!module || !mode) return null;
  return { module, mode, definition: getSghcFormDefinition(module, mode, route) };
}

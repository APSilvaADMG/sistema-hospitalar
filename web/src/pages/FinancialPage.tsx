import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { useAppearance } from '../theme/AppearanceProvider';
import { isFeegowBrand } from '../theme/appearanceConfig';
import { FeegowFinanceWorkspacePage } from './FeegowFinanceWorkspacePage';
import {
  api,
  financialCategoryLabel,
  financialCategoryValue,
  financialDirectionLabel,
  financialDirectionValue,
  financialStatusLabel,
  isFinancialOpen,
  isFinancialReceivable,
  paymentMethodLabel,
  paymentMethodLabels,
  paymentMethodValue,
  paymentModalityLabels,
  pixChargeStatusLabel,
  pixChargeStatusValue,
  type FinancialAccountCreateSuggestionsDto,
  type FinancialAccountSourceOptionDto,
  type FinancialPaymentDto,
  type FinancialSummaryDto,
  type PayableCategoryPresetDto,
  type PatientDto,
  type PixChargeDto,
  type FinancialAccountDto,
  type SupplierDto,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { MiscellaneousReceiptsPanel } from '../components/finance/MiscellaneousReceiptsPanel';
import { Modal } from '../components/Modal';
import { ModuleNav } from '../components/ModuleNav';
import { PageHeader } from '../components/PageHeader';
import { financialTabs } from '../navigation/moduleSections';
import { useModuleSection } from '../navigation/useModuleSection';
import { addDaysIso, formatBrDate, formatBrDateTime } from '../utils/dateUtils';
import {
  buildInstallmentRows,
  installmentRowsTotal,
  type PaymentInstallmentFormRow,
} from '../utils/creditCardInstallments';
import { formatPhone } from '../utils/pepUtils';

function PaymentMethodSelect({
  id,
  value,
  onChange,
  hint,
}: {
  id: string;
  value: number;
  onChange: (value: number) => void;
  hint?: string;
}) {
  return (
    <div className="form-field">
      <label htmlFor={id}>Forma de pagamento *</label>
      <select id={id} required value={value} onChange={(e) => onChange(Number(e.target.value))}>
        {Object.entries(paymentMethodLabels).map(([methodValue, label]) => (
          <option key={methodValue} value={methodValue}>{label}</option>
        ))}
      </select>
      {hint && <span className="field-hint">{hint}</span>}
    </div>
  );
}

function formatCpf(cpf: string): string {
  const digits = cpf.replace(/\D/g, '');
  if (digits.length !== 11) return cpf;
  return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9)}`;
}

const emptyCreateForm = {
  direction: 1 as 1 | 2,
  patientId: '',
  patientSearch: '',
  supplierId: '',
  counterpartyName: '',
  category: 1,
  sourceKey: '',
  appointmentId: '',
  hospitalizationId: '',
  description: '',
  amount: '',
  dueDate: addDaysIso(7),
  notes: '',
  paymentMethod: 2,
  autoGeneratePix: true,
};

const emptyPaymentForm = {
  amount: '',
  method: 2,
  paidAt: new Date().toISOString().slice(0, 10),
  notes: '',
  useInstallments: false,
  installmentCount: 2,
  installments: [] as PaymentInstallmentFormRow[],
};

export function FinancialPage() {
  const { appearance } = useAppearance();
  if (isFeegowBrand(appearance.brand)) {
    return <FeegowFinanceWorkspacePage />;
  }
  return <HospitalFinancialPage />;
}

function HospitalFinancialPage() {
  const [accounts, setAccounts] = useState<FinancialAccountDto[]>([]);
  const [patients, setPatients] = useState<PatientDto[]>([]);
  const [suppliers, setSuppliers] = useState<SupplierDto[]>([]);
  const [payablePresets, setPayablePresets] = useState<PayableCategoryPresetDto[]>([]);
  const { section } = useModuleSection('/financeiro');
  const [directionTab, setDirectionTab] = useState<'all' | 1 | 2>('all');

  useEffect(() => {
    if (section.startsWith('receber') || section === 'cobrancas' || section === 'recibos-diversos' || section === 'boletos') {
      setDirectionTab(1);
    } else if (section.startsWith('pagar') || section.startsWith('fiscal') || section.startsWith('tesouraria')) {
      setDirectionTab(2);
    }
  }, [section]);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<number | ''>('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [summary, setSummary] = useState<FinancialSummaryDto | null>(null);
  const [paymentAccount, setPaymentAccount] = useState<FinancialAccountDto | null>(null);
  const [paymentForm, setPaymentForm] = useState(emptyPaymentForm);
  const [paymentSubmitting, setPaymentSubmitting] = useState(false);
  const [historyAccount, setHistoryAccount] = useState<FinancialAccountDto | null>(null);
  const [paymentHistory, setPaymentHistory] = useState<FinancialPaymentDto[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [pixCharge, setPixCharge] = useState<PixChargeDto | null>(null);
  const [pixLoading, setPixLoading] = useState(false);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [createForm, setCreateForm] = useState(emptyCreateForm);
  const [creating, setCreating] = useState(false);
  const [suggestions, setSuggestions] = useState<FinancialAccountCreateSuggestionsDto | null>(null);
  const [suggestionsLoading, setSuggestionsLoading] = useState(false);
  const [patientSearchLoading, setPatientSearchLoading] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();

  async function load() {
    const [result, summaryData] = await Promise.all([
      api.getFinancialAccounts(statusFilter === '' ? undefined : statusFilter, search),
      api.getFinancialSummary(),
    ]);
    setAccounts(result.items);
    setSummary(summaryData);
  }

  useEffect(() => {
    load().catch((err) => setError(err instanceof Error ? err.message : 'Erro ao carregar contas'));
  }, [statusFilter]);

  useEffect(() => {
    Promise.all([api.getSuppliers(), api.getPayableCategoryPresets()])
      .then(([supplierList, presets]) => {
        setSuppliers(supplierList);
        setPayablePresets(presets);
      })
      .catch(console.error);
  }, []);

  const searchPatients = useCallback(async (term: string) => {
    setPatientSearchLoading(true);
    try {
      const result = await api.getPatients(term, 1);
      setPatients(result.items);
    } catch (err) {
      console.error(err);
    } finally {
      setPatientSearchLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!showCreateModal) return;
    const timer = window.setTimeout(() => {
      void searchPatients(createForm.patientSearch);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [createForm.patientSearch, showCreateModal, searchPatients]);

  const loadSuggestions = useCallback(async (patientId: string) => {
    setSuggestionsLoading(true);
    setSuggestions(null);
    try {
      const data = await api.getFinancialAccountSuggestions(patientId);
      setSuggestions(data);
      const defaultPreset = data.categoryPresets[0];
      setCreateForm((prev) => ({
        ...prev,
        patientId,
        category: defaultPreset ? financialCategoryValue(defaultPreset.category) : 1,
        description: defaultPreset?.descriptionTemplate ?? prev.description,
        amount: defaultPreset?.suggestedAmount ? String(defaultPreset.suggestedAmount) : prev.amount,
        dueDate: addDaysIso(data.suggestedDueDays),
        sourceKey: '',
        appointmentId: '',
        hospitalizationId: '',
      }));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar sugestões');
    } finally {
      setSuggestionsLoading(false);
    }
  }, []);

  function openCreateModal(direction: 1 | 2 = 1) {
    const defaultCategory = direction === 2
      ? (payablePresets[0] ? financialCategoryValue(payablePresets[0].category) : 7)
      : 1;
    const defaultPreset = direction === 2 ? payablePresets[0] : null;
    setCreateForm({
      ...emptyCreateForm,
      direction,
      category: defaultCategory,
      description: defaultPreset?.descriptionTemplate ?? '',
      amount: defaultPreset && defaultPreset.suggestedAmount > 0 ? String(defaultPreset.suggestedAmount) : '',
      dueDate: addDaysIso(defaultPreset?.suggestedDueDays ?? 7),
      paymentMethod: direction === 2 ? 5 : 2,
      autoGeneratePix: direction === 1,
    });
    setSuggestions(null);
    setShowCreateModal(true);
    if (direction === 1) void searchPatients('');
  }

  useEffect(() => {
    if (accounts.length === 0) return;
    const contaId = searchParams.get('conta');
    if (contaId) {
      const account = accounts.find((a) => a.id === contaId);
      if (account) setSearch(account.counterpartyDisplay || account.description);
      const next = new URLSearchParams(searchParams);
      next.delete('conta');
      setSearchParams(next, { replace: true });
      return;
    }
    const novo = searchParams.get('novo');
    if (novo === 'pagar' || novo === 'receber') {
      openCreateModal(novo === 'pagar' ? 2 : 1);
      const next = new URLSearchParams(searchParams);
      next.delete('novo');
      setSearchParams(next, { replace: true });
    }
  }, [accounts, searchParams, setSearchParams]);

  function applyPayableCategoryPreset(category: number) {
    const preset = payablePresets.find((p) => financialCategoryValue(p.category) === category);
    if (!preset) {
      setCreateForm((prev) => ({ ...prev, category }));
      return;
    }
    setCreateForm((prev) => ({
      ...prev,
      category,
      description: preset.descriptionTemplate,
      amount: preset.suggestedAmount > 0 ? String(preset.suggestedAmount) : prev.amount,
      dueDate: addDaysIso(preset.suggestedDueDays),
    }));
  }

  function applyCategoryPreset(category: number) {
    const preset = suggestions?.categoryPresets.find(
      (p) => financialCategoryValue(p.category) === category,
    );
    if (!preset) {
      setCreateForm((prev) => ({ ...prev, category, sourceKey: '', appointmentId: '', hospitalizationId: '' }));
      return;
    }
    setCreateForm((prev) => ({
      ...prev,
      category,
      sourceKey: '',
      appointmentId: '',
      hospitalizationId: '',
      description: preset.descriptionTemplate,
      amount: preset.suggestedAmount > 0 ? String(preset.suggestedAmount) : prev.amount,
      dueDate: addDaysIso(preset.suggestedDueDays),
    }));
  }

  function applySourceOption(option: FinancialAccountSourceOptionDto) {
    if (option.alreadyBilled) return;
    const sourceKey = `${option.sourceType}:${option.sourceId}`;
    setCreateForm((prev) => ({
      ...prev,
      sourceKey,
      category: financialCategoryValue(option.suggestedCategory),
      description: option.suggestedDescription,
      amount: String(option.suggestedAmount),
      appointmentId: option.sourceType === 'appointment' ? option.sourceId : '',
      hospitalizationId: option.sourceType === 'hospitalization' ? option.sourceId : '',
    }));
  }

  const availableSources = useMemo(() => {
    if (!suggestions) return [];
    return suggestions.sourceOptions.filter((o) => !o.alreadyBilled);
  }, [suggestions]);

  const selectedPatientLabel = useMemo(() => {
    if (suggestions?.patientName) return suggestions.patientName;
    const p = patients.find((item) => item.id === createForm.patientId);
    return p?.fullName;
  }, [suggestions, patients, createForm.patientId]);

  async function handleSearch(event: React.FormEvent) {
    event.preventDefault();
    await load();
  }

  function rebuildPaymentInstallments(
    amount: string,
    count: number,
    firstDueDate: string,
  ): PaymentInstallmentFormRow[] {
    const total = Number(amount);
    if (!total || count < 2) return [];
    return buildInstallmentRows(total, count, firstDueDate || new Date().toISOString().slice(0, 10));
  }

  function openPaymentModal(account: FinancialAccountDto) {
    setPaymentAccount(account);
    const defaultMethod = account.lastPaymentMethod
      ? paymentMethodValue(account.lastPaymentMethod)
      : account.expectedPaymentMethod
        ? paymentMethodValue(account.expectedPaymentMethod)
        : emptyPaymentForm.method;
    setPaymentForm({
      ...emptyPaymentForm,
      amount: account.balance > 0 ? String(account.balance) : '',
      method: defaultMethod,
    });
    setError('');
    setSuccess('');
  }

  async function handlePaymentSubmit(event: FormEvent) {
    event.preventDefault();
    if (!paymentAccount) return;

    const amount = Number(paymentForm.amount);
    if (!amount || amount <= 0) {
      setError('Informe um valor válido para pagamento.');
      return;
    }
    if (amount > paymentAccount.balance) {
      setError(`Valor excede o saldo em aberto (R$ ${paymentAccount.balance.toFixed(2)}).`);
      return;
    }

    const isCreditCard = paymentForm.method === 4;
    if (isCreditCard && paymentForm.useInstallments) {
      if (paymentForm.installments.length < 2) {
        setError('Informe ao menos 2 parcelas.');
        return;
      }
      const installmentsTotal = installmentRowsTotal(paymentForm.installments);
      if (Math.abs(installmentsTotal - amount) > 0.009) {
        setError('A soma das parcelas deve ser igual ao valor do pagamento.');
        return;
      }
      if (paymentForm.installments.some((row) => !row.dueDate || Number(row.amount) <= 0)) {
        setError('Preencha data e valor de todas as parcelas.');
        return;
      }
    }

    setPaymentSubmitting(true);
    setError('');
    setSuccess('');
    try {
      await api.registerPayment(paymentAccount.id, {
        amount,
        method: paymentForm.method,
        paidAt: paymentForm.paidAt
          ? new Date(`${paymentForm.paidAt}T12:00:00`).toISOString()
          : undefined,
        notes: paymentForm.notes.trim() || undefined,
        installments:
          isCreditCard && paymentForm.useInstallments
            ? paymentForm.installments.map((row) => ({
                installmentNumber: row.installmentNumber,
                amount: Number(row.amount),
                dueDate: new Date(`${row.dueDate}T12:00:00`).toISOString(),
              }))
            : undefined,
      });
      setSuccess(
        isCreditCard && paymentForm.useInstallments
          ? 'Pagamento parcelado registrado. As parcelas foram incluídas no financeiro.'
          : 'Pagamento registrado com sucesso.',
      );
      setPaymentAccount(null);
      setPaymentForm(emptyPaymentForm);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao registrar pagamento');
    } finally {
      setPaymentSubmitting(false);
    }
  }

  async function openPaymentHistory(account: FinancialAccountDto) {
    setHistoryAccount(account);
    setHistoryLoading(true);
    setPaymentHistory([]);
    try {
      const items = await api.getFinancialPayments(account.id);
      setPaymentHistory(items);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar histórico');
      setHistoryAccount(null);
    } finally {
      setHistoryLoading(false);
    }
  }

  async function handleGeneratePix(accountId: string) {
    setError('');
    setSuccess('');
    setPixLoading(true);
    try {
      const charge = await api.createPixCharge(accountId);
      setPixCharge(charge);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar PIX');
    } finally {
      setPixLoading(false);
    }
  }

  async function handleSimulatePix() {
    if (!pixCharge) return;
    setPixLoading(true);
    setError('');
    try {
      const updated = await api.simulatePixPayment(pixCharge.id);
      setPixCharge(updated);
      setSuccess('Pagamento PIX simulado — conta baixada automaticamente.');
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao simular pagamento PIX');
    } finally {
      setPixLoading(false);
    }
  }

  async function handleCreateAccount(event: FormEvent) {
    event.preventDefault();
    const amount = Number(createForm.amount);
    const isReceivable = createForm.direction === 1;

    if (isReceivable && !createForm.patientId) {
      setError('Selecione o paciente.');
      return;
    }
    if (!isReceivable && !createForm.supplierId && !createForm.counterpartyName.trim()) {
      setError('Informe o fornecedor ou o nome do favorecido.');
      return;
    }
    if (!createForm.description.trim()) {
      setError('Informe a descrição da conta.');
      return;
    }
    if (!amount || amount <= 0) {
      setError('Informe um valor válido.');
      return;
    }

    setCreating(true);
    setError('');
    setSuccess('');
    try {
      const created = await api.createFinancialAccount({
        direction: createForm.direction,
        patientId: isReceivable ? createForm.patientId : undefined,
        supplierId: !isReceivable ? (createForm.supplierId || undefined) : undefined,
        counterpartyName: !isReceivable ? (createForm.counterpartyName.trim() || undefined) : undefined,
        appointmentId: isReceivable ? (createForm.appointmentId || undefined) : undefined,
        hospitalizationId: isReceivable ? (createForm.hospitalizationId || undefined) : undefined,
        category: createForm.category,
        description: createForm.description.trim(),
        amount,
        dueDate: createForm.dueDate
          ? new Date(`${createForm.dueDate}T12:00:00`).toISOString()
          : undefined,
        notes: createForm.notes.trim() || undefined,
        expectedPaymentMethod: createForm.paymentMethod,
      });

      if (isReceivable && createForm.autoGeneratePix && createForm.paymentMethod === 2) {
        try {
          const charge = await api.createPixCharge(created.id);
          setPixCharge(charge);
          setSuccess('Conta cadastrada e PIX gerado automaticamente.');
        } catch {
          setSuccess('Conta cadastrada. Não foi possível gerar o PIX automaticamente — use o botão na listagem.');
        }
      } else {
        setSuccess(isReceivable ? 'Conta cadastrada com sucesso.' : 'Conta a pagar cadastrada com sucesso.');
      }

      setCreateForm(emptyCreateForm);
      setSuggestions(null);
      setShowCreateModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao cadastrar conta');
    } finally {
      setCreating(false);
    }
  }

  const filteredAccounts = useMemo(() => {
    if (directionTab === 'all') return accounts;
    return accounts.filter((a) => financialDirectionValue(a.direction) === directionTab);
  }, [accounts, directionTab]);

  const receivableOpen = summary?.receivableOpen ?? 0;
  const payableOpen = summary?.payableOpen ?? 0;
  const totalReceived = summary?.totalReceived ?? 0;
  const totalPaidOut = summary?.totalPaidOut ?? 0;
  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title="Financeiro"
        subtitle="Controle de entradas (contas a receber) e saídas (contas a pagar) com PIX, baixa manual e cobrança via Connect."
      >
        <button className="btn btn-secondary" type="button" onClick={() => openCreateModal(2)}>
          + Conta a pagar
        </button>
        <button className="btn" type="button" onClick={() => openCreateModal(1)}>
          + Conta a receber
        </button>
      </PageHeader>

      <ModuleNav basePath="/financeiro" tabs={financialTabs} contextId="financial" />

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      {section === 'recibos-diversos' ? (
        <MiscellaneousReceiptsPanel variant="hospital" />
      ) : (
      <>
      <div className="kpi-grid">
        <KpiCard label="A receber (aberto)" value={`R$ ${receivableOpen.toFixed(2)}`} variant="primary" />
        <KpiCard label="A pagar (aberto)" value={`R$ ${payableOpen.toFixed(2)}`} variant="warning" />
        <KpiCard label="Total recebido" value={`R$ ${totalReceived.toFixed(2)}`} variant="success" />
        <KpiCard label="Total pago" value={`R$ ${totalPaidOut.toFixed(2)}`} variant="danger" />
      </div>

      <div className="fin-direction-tabs" style={{ marginTop: 24 }}>
        <button
          type="button"
          className={`fin-direction-tab${directionTab === 'all' ? ' active' : ''}`}
          onClick={() => setDirectionTab('all')}
        >
          Todas ({accounts.length})
        </button>
        <button
          type="button"
          className={`fin-direction-tab${directionTab === 1 ? ' active' : ''}`}
          onClick={() => setDirectionTab(1)}
        >
          Entradas ({accounts.filter((a) => isFinancialReceivable(a.direction)).length})
        </button>
        <button
          type="button"
          className={`fin-direction-tab${directionTab === 2 ? ' active' : ''}`}
          onClick={() => setDirectionTab(2)}
        >
          Saídas ({accounts.filter((a) => !isFinancialReceivable(a.direction)).length})
        </button>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">
          {directionTab === 2 ? 'Contas a pagar' : directionTab === 1 ? 'Contas a receber' : 'Lançamentos financeiros'}
          {' — '}{filteredAccounts.length} conta(s)
        </div>
        <FilterBar>
          <div className="filter-field grow">
            <label htmlFor="finSearch">Buscar</label>
            <input
              id="finSearch"
              placeholder="Paciente, fornecedor ou descrição..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="filter-field w-lg">
            <label htmlFor="finStatus">Status</label>
            <select id="finStatus" value={statusFilter} onChange={(e) => setStatusFilter(e.target.value === '' ? '' : Number(e.target.value))}>
              <option value="">Todos</option>
              <option value="1">Em aberto</option>
              <option value="2">Parcial</option>
              <option value="3">Pago</option>
              <option value="4">Cancelado</option>
            </select>
          </div>
          <div className="filter-field align-end">
            <button className="btn btn-secondary" type="button" onClick={handleSearch}>Buscar</button>
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Movimento</th>
                <th>Favorecido / Paciente</th>
                <th>Categoria</th>
                <th>Descrição</th>
                <th>Valor</th>
                <th>Pago</th>
                <th>Saldo</th>
                <th>Vencimento</th>
                <th>Último pagamento</th>
                <th>Forma</th>
                <th>Status</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filteredAccounts.map((account) => (
                <tr key={account.id}>
                  <td>
                    <span className={`badge ${isFinancialReceivable(account.direction) ? 'badge-success' : 'badge-danger'}`}>
                      {financialDirectionLabel(account.direction)}
                    </span>
                  </td>
                  <td>{account.counterpartyDisplay}</td>
                  <td><span className="badge badge-muted">{financialCategoryLabel(account.category)}</span></td>
                  <td>
                    <div>{account.description}</div>
                    {account.installmentNumber && account.installmentCount ? (
                      <span className="badge badge-muted" style={{ marginTop: 4, display: 'inline-block' }}>
                        Parcela {account.installmentNumber}/{account.installmentCount}
                      </span>
                    ) : null}
                    {account.notes && (
                      <div style={{ fontSize: 12, color: 'var(--muted)', marginTop: 2 }}>{account.notes}</div>
                    )}
                  </td>
                  <td>R$ {account.amount.toFixed(2)}</td>
                  <td>R$ {account.paidAmount.toFixed(2)}</td>
                  <td>R$ {account.balance.toFixed(2)}</td>
                  <td>
                    {account.dueDate ? formatBrDate(account.dueDate) : '—'}
                  </td>
                  <td>
                    {account.paidAt ? formatBrDateTime(account.paidAt) : '—'}
                  </td>
                  <td>
                    {account.paymentCount > 0 ? (
                      <span className="badge badge-muted">{paymentMethodLabel(account.lastPaymentMethod)}</span>
                    ) : account.expectedPaymentMethod ? (
                      <span className="badge badge-muted" title="Forma prevista no cadastro">
                        {paymentMethodLabel(account.expectedPaymentMethod)}
                      </span>
                    ) : (
                      '—'
                    )}
                  </td>
                  <td><span className="badge">{financialStatusLabel(account.status)}</span></td>
                  <td>
                    <div className="payment-row" style={{ flexWrap: 'wrap', gap: 8 }}>
                      {account.paymentCount > 0 && (
                        <button
                          className="btn btn-secondary btn-sm"
                          type="button"
                          onClick={() => openPaymentHistory(account)}
                        >
                          Histórico ({account.paymentCount})
                        </button>
                      )}
                      {isFinancialOpen(account.status) && (
                        <>
                          {isFinancialReceivable(account.direction) && (
                            <button
                              className="btn btn-sm"
                              type="button"
                              disabled={pixLoading}
                              onClick={() => handleGeneratePix(account.id)}
                            >
                              Gerar PIX
                            </button>
                          )}
                          <button
                            className="btn btn-secondary btn-sm"
                            type="button"
                            onClick={() => openPaymentModal(account)}
                          >
                            {isFinancialReceivable(account.direction) ? 'Baixa manual' : 'Registrar pagamento'}
                          </button>
                        </>
                      )}
                      {!isFinancialOpen(account.status) && account.paymentCount === 0 && '—'}
                    </div>
                  </td>
                </tr>
              ))}
              {filteredAccounts.length === 0 && (
                <tr>
                  <td colSpan={12} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    <div className="appt-empty" style={{ padding: '12px 0' }}>
                      <div className="appt-empty-icon">💳</div>
                      <h3>Nenhuma conta cadastrada</h3>
                      <p>Cadastre entradas (a receber) ou saídas (a pagar) para controlar o fluxo financeiro.</p>
                      <div style={{ display: 'flex', gap: 8, justifyContent: 'center', marginTop: 12 }}>
                        <button className="btn btn-secondary" type="button" onClick={() => openCreateModal(2)}>
                          + Conta a pagar
                        </button>
                        <button className="btn" type="button" onClick={() => openCreateModal(1)}>
                          + Conta a receber
                        </button>
                      </div>
                    </div>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={showCreateModal}
        onClose={() => setShowCreateModal(false)}
        title={createForm.direction === 2 ? 'Nova conta a pagar' : 'Nova conta a receber'}
        subtitle={createForm.direction === 2
          ? 'Registre despesas, fornecedores e obrigações do hospital.'
          : 'Selecione o paciente — o sistema preenche valor, vencimento e vínculos automaticamente.'}
        width="lg"
      >
        <form className="form-grid" onSubmit={handleCreateAccount}>
          <div className="form-field full fin-direction-switch">
            <label>Tipo de lançamento *</label>
            <div className="fin-direction-switch-row">
              <button
                type="button"
                className={`fin-direction-tab${createForm.direction === 1 ? ' active' : ''}`}
                onClick={() => openCreateModal(1)}
              >
                Entrada (a receber)
              </button>
              <button
                type="button"
                className={`fin-direction-tab${createForm.direction === 2 ? ' active' : ''}`}
                onClick={() => openCreateModal(2)}
              >
                Saída (a pagar)
              </button>
            </div>
          </div>

          {createForm.direction === 2 ? (
            <>
              <div className="form-field">
                <label htmlFor="finSupplier">Fornecedor</label>
                <select
                  id="finSupplier"
                  value={createForm.supplierId}
                  onChange={(e) => setCreateForm({ ...createForm, supplierId: e.target.value })}
                >
                  <option value="">Selecione (opcional)</option>
                  {suppliers.map((s) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
              </div>
              <div className="form-field">
                <label htmlFor="finCounterparty">Favorecido</label>
                <input
                  id="finCounterparty"
                  placeholder="Ex.: Companhia de Energia, folha..."
                  value={createForm.counterpartyName}
                  onChange={(e) => setCreateForm({ ...createForm, counterpartyName: e.target.value })}
                />
                <span className="field-hint">Informe fornecedor ou favorecido (pelo menos um).</span>
              </div>
              <div className="form-field">
                <label htmlFor="finPayCategory">Categoria da despesa *</label>
                <select
                  id="finPayCategory"
                  required
                  value={createForm.category}
                  onChange={(e) => applyPayableCategoryPreset(Number(e.target.value))}
                >
                  {payablePresets.map((preset) => (
                    <option key={financialCategoryValue(preset.category)} value={financialCategoryValue(preset.category)}>
                      {preset.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="form-field">
                <label htmlFor="finPayDueDate">Vencimento</label>
                <input
                  id="finPayDueDate"
                  type="date"
                  value={createForm.dueDate}
                  onChange={(e) => setCreateForm({ ...createForm, dueDate: e.target.value })}
                />
              </div>
              <div className="form-field full">
                <label htmlFor="finPayDescription">Descrição *</label>
                <input
                  id="finPayDescription"
                  required
                  placeholder="Ex.: Pedido de compras, conta de luz..."
                  value={createForm.description}
                  onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
                />
              </div>
              <div className="form-field">
                <label htmlFor="finPayAmount">Valor (R$) *</label>
                <input
                  id="finPayAmount"
                  type="number"
                  min="0.01"
                  step="0.01"
                  required
                  value={createForm.amount}
                  onChange={(e) => setCreateForm({ ...createForm, amount: e.target.value })}
                />
              </div>
              <PaymentMethodSelect
                id="finPayMethod"
                value={createForm.paymentMethod}
                onChange={(paymentMethod) => setCreateForm({ ...createForm, paymentMethod })}
                hint="Forma prevista para quitação da despesa."
              />
              <div className="form-field full">
                <label htmlFor="finPayNotes">Observações</label>
                <textarea
                  id="finPayNotes"
                  rows={2}
                  value={createForm.notes}
                  onChange={(e) => setCreateForm({ ...createForm, notes: e.target.value })}
                />
              </div>
            </>
          ) : (
            <>
          <div className="form-field full">
            <label htmlFor="finPatientSearch">Buscar paciente *</label>
            <input
              id="finPatientSearch"
              placeholder="Nome, CPF ou convênio..."
              value={createForm.patientSearch}
              onChange={(e) => setCreateForm({ ...createForm, patientSearch: e.target.value })}
              autoComplete="off"
            />
            {patientSearchLoading && (
              <span className="field-hint">Buscando pacientes...</span>
            )}
          </div>

          <div className="form-field full">
            <label htmlFor="finPatient">Paciente *</label>
            <select
              id="finPatient"
              required
              value={createForm.patientId}
              onChange={(e) => {
                const patientId = e.target.value;
                setCreateForm({ ...createForm, patientId });
                if (patientId) void loadSuggestions(patientId);
              }}
            >
              <option value="">Selecione o paciente</option>
              {patients.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName}
                  {p.cpf ? ` · ${formatCpf(p.cpf)}` : ''}
                  {p.primaryInsuranceName ? ` · ${p.primaryInsuranceName}` : ''}
                </option>
              ))}
            </select>
          </div>

          {suggestions && (
            <div className="form-field full fin-patient-summary">
              <div className="fin-summary-grid">
                <div>
                  <span className="fin-summary-label">Paciente</span>
                  <strong>{suggestions.patientName}</strong>
                </div>
                <div>
                  <span className="fin-summary-label">CPF</span>
                  <strong>{formatCpf(suggestions.cpf)}</strong>
                </div>
                <div>
                  <span className="fin-summary-label">Convênio</span>
                  <strong>{suggestions.insuranceName ?? 'Particular'}</strong>
                </div>
                <div>
                  <span className="fin-summary-label">Modalidade</span>
                  <strong>{paymentModalityLabels[suggestions.paymentModality] ?? '—'}</strong>
                </div>
                <div>
                  <span className="fin-summary-label">Telefone</span>
                  <strong>{formatPhone(suggestions.phone) ?? '—'}</strong>
                </div>
                <div>
                  <span className="fin-summary-label">Saldo em aberto</span>
                  <strong className={suggestions.outstandingBalance > 0 ? 'fin-warning' : ''}>
                    R$ {suggestions.outstandingBalance.toFixed(2)}
                  </strong>
                </div>
              </div>
            </div>
          )}

          {suggestionsLoading && (
            <div className="form-field full">
              <span className="field-hint">Carregando atendimentos e sugestões...</span>
            </div>
          )}

          {availableSources.length > 0 && (
            <div className="form-field full">
              <label htmlFor="finSource">Vincular a atendimento (opcional)</label>
              <select
                id="finSource"
                value={createForm.sourceKey}
                onChange={(e) => {
                  const key = e.target.value;
                  if (!key) {
                    setCreateForm((prev) => ({
                      ...prev,
                      sourceKey: '',
                      appointmentId: '',
                      hospitalizationId: '',
                    }));
                    return;
                  }
                  const option = availableSources.find((o) => `${o.sourceType}:${o.sourceId}` === key);
                  if (option) applySourceOption(option);
                }}
              >
                <option value="">Sem vínculo — preencher manualmente</option>
                {availableSources.map((o) => (
                  <option key={`${o.sourceType}:${o.sourceId}`} value={`${o.sourceType}:${o.sourceId}`}>
                    {o.label} — R$ {o.suggestedAmount.toFixed(2)} ({o.detail})
                  </option>
                ))}
              </select>
              <span className="field-hint">
                Consultas, internações e estacionamento pendentes são detectados automaticamente.
              </span>
            </div>
          )}

          <div className="form-field">
            <label htmlFor="finCategory">Tipo de cobrança *</label>
            <select
              id="finCategory"
              required
              value={createForm.category}
              onChange={(e) => applyCategoryPreset(Number(e.target.value))}
            >
              {(suggestions?.categoryPresets ?? [
                { category: 1, label: 'Consulta ambulatorial' },
                { category: 2, label: 'Internação / diárias' },
                { category: 3, label: 'Exames laboratoriais' },
                { category: 4, label: 'Coparticipação convênio' },
                { category: 5, label: 'Estacionamento' },
                { category: 6, label: 'Outros serviços' },
              ]).map((preset) => (
                <option key={financialCategoryValue(preset.category)} value={financialCategoryValue(preset.category)}>
                  {preset.label}
                </option>
              ))}
            </select>
          </div>

          <div className="form-field">
            <label htmlFor="finDueDate">Vencimento</label>
            <input
              id="finDueDate"
              type="date"
              value={createForm.dueDate}
              onChange={(e) => setCreateForm({ ...createForm, dueDate: e.target.value })}
            />
            {suggestions && (
              <span className="field-hint">
                Sugerido: {suggestions.suggestedDueDays} dias ({paymentModalityLabels[suggestions.paymentModality]})
              </span>
            )}
          </div>

          <div className="form-field full">
            <label htmlFor="finDescription">Descrição *</label>
            <input
              id="finDescription"
              required
              placeholder="Ex.: Consulta cardiologia, coparticipação internação..."
              value={createForm.description}
              onChange={(e) => setCreateForm({ ...createForm, description: e.target.value })}
            />
          </div>

          <div className="form-field">
            <label htmlFor="finAmount">Valor (R$) *</label>
            <input
              id="finAmount"
              type="number"
              min="0.01"
              step="0.01"
              required
              placeholder="0,00"
              value={createForm.amount}
              onChange={(e) => setCreateForm({ ...createForm, amount: e.target.value })}
            />
          </div>

          <PaymentMethodSelect
            id="finReceiveMethod"
            value={createForm.paymentMethod}
            onChange={(paymentMethod) => setCreateForm({
              ...createForm,
              paymentMethod,
              autoGeneratePix: paymentMethod === 2 ? createForm.autoGeneratePix : false,
            })}
            hint="Forma prevista de recebimento do paciente."
          />

          <div className="form-field full">
            <label htmlFor="finNotes">Observações internas</label>
            <textarea
              id="finNotes"
              rows={2}
              placeholder="Ex.: Autorização convênio nº 12345, parcelamento acordado..."
              value={createForm.notes}
              onChange={(e) => setCreateForm({ ...createForm, notes: e.target.value })}
            />
          </div>

          <div className="form-field full">
            <label className="checkbox-label">
              <input
                type="checkbox"
                disabled={createForm.paymentMethod !== 2}
                checked={createForm.autoGeneratePix}
                onChange={(e) => setCreateForm({ ...createForm, autoGeneratePix: e.target.checked })}
              />
              Gerar PIX automaticamente ao salvar
              {selectedPatientLabel ? ` para ${selectedPatientLabel}` : ''}
            </label>
            <span className="field-hint">
              Lembrete de cobrança via WhatsApp Connect será agendado para a data de vencimento.
            </span>
          </div>
            </>
          )}

          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowCreateModal(false)}>
              Cancelar
            </button>
            <button className="btn" type="submit" disabled={creating || suggestionsLoading}>
              {creating
                ? 'Salvando...'
                : createForm.direction === 2
                  ? 'Cadastrar conta a pagar'
                  : createForm.autoGeneratePix
                    ? 'Cadastrar e gerar PIX'
                    : 'Cadastrar conta a receber'}
            </button>
          </div>
        </form>
      </Modal>

      <Modal
        open={!!paymentAccount}
        onClose={() => setPaymentAccount(null)}
        title={paymentAccount && isFinancialReceivable(paymentAccount.direction) ? 'Registrar recebimento' : 'Registrar pagamento'}
        subtitle={paymentAccount
          ? `${paymentAccount.counterpartyDisplay} — saldo R$ ${paymentAccount.balance.toFixed(2)}`
          : undefined}
        width={paymentForm.useInstallments && paymentForm.method === 4 ? 'lg' : 'md'}
      >
        {paymentAccount && (
          <form className="form-grid" onSubmit={handlePaymentSubmit}>
            <div className="form-field">
              <label htmlFor="payMethod">Forma de pagamento *</label>
              <select
                id="payMethod"
                required
                value={paymentForm.method}
                onChange={(e) => {
                  const method = Number(e.target.value);
                  const nextForm = {
                    ...paymentForm,
                    method,
                    useInstallments: method === 4 ? paymentForm.useInstallments : false,
                    installments: method === 4 && paymentForm.useInstallments
                      ? rebuildPaymentInstallments(paymentForm.amount, paymentForm.installmentCount, paymentForm.paidAt)
                      : [],
                  };
                  setPaymentForm(nextForm);
                }}
              >
                <option value={1}>Dinheiro</option>
                <option value={2}>PIX</option>
                <option value={3}>Cartão débito</option>
                <option value={4}>Cartão crédito</option>
                <option value={5}>Transferência bancária</option>
              </select>
            </div>
            {paymentForm.method === 4 ? (
              <div className="form-field full">
                <label className="feegow-check-pill">
                  <input
                    type="checkbox"
                    checked={paymentForm.useInstallments}
                    onChange={(e) => {
                      const useInstallments = e.target.checked;
                      setPaymentForm({
                        ...paymentForm,
                        useInstallments,
                        installments: useInstallments
                          ? rebuildPaymentInstallments(
                              paymentForm.amount,
                              paymentForm.installmentCount,
                              paymentForm.paidAt,
                            )
                          : [],
                      });
                    }}
                  />
                  <span>Parcelar no cartão de crédito</span>
                </label>
              </div>
            ) : null}
            {paymentForm.method === 4 && paymentForm.useInstallments ? (
              <div className="form-field full payment-installments-panel">
                <div className="payment-installments-head">
                  <label htmlFor="installmentCount">Quantidade de parcelas</label>
                  <select
                    id="installmentCount"
                    value={paymentForm.installmentCount}
                    onChange={(e) => {
                      const installmentCount = Number(e.target.value);
                      setPaymentForm({
                        ...paymentForm,
                        installmentCount,
                        installments: rebuildPaymentInstallments(
                          paymentForm.amount,
                          installmentCount,
                          paymentForm.paidAt,
                        ),
                      });
                    }}
                  >
                    {Array.from({ length: 11 }, (_, index) => index + 2).map((count) => (
                      <option key={count} value={count}>{count}x</option>
                    ))}
                  </select>
                </div>
                <table className="data-table payment-installments-table">
                  <thead>
                    <tr>
                      <th>Parcela</th>
                      <th>Vencimento</th>
                      <th>Valor (R$)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {paymentForm.installments.map((row, index) => (
                      <tr key={row.installmentNumber}>
                        <td>{row.installmentNumber}</td>
                        <td>
                          <input
                            type="date"
                            required
                            value={row.dueDate}
                            onChange={(e) => {
                              const installments = [...paymentForm.installments];
                              installments[index] = { ...row, dueDate: e.target.value };
                              setPaymentForm({ ...paymentForm, installments });
                            }}
                          />
                        </td>
                        <td>
                          <input
                            type="number"
                            min="0.01"
                            step="0.01"
                            required
                            value={row.amount}
                            onChange={(e) => {
                              const installments = [...paymentForm.installments];
                              installments[index] = { ...row, amount: e.target.value };
                              setPaymentForm({ ...paymentForm, installments });
                            }}
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <p className="field-hint">
                  Total das parcelas: R$ {installmentRowsTotal(paymentForm.installments).toFixed(2)}
                  {' · '}
                  Cada parcela será lançada em contas a {isFinancialReceivable(paymentAccount.direction) ? 'receber' : 'pagar'}.
                </p>
              </div>
            ) : null}
            <div className="form-field">
              <label htmlFor="payAmount">Valor (R$) *</label>
              <input
                id="payAmount"
                type="number"
                min="0.01"
                step="0.01"
                max={paymentAccount.balance}
                required
                value={paymentForm.amount}
                onChange={(e) => {
                  const amount = e.target.value;
                  setPaymentForm({
                    ...paymentForm,
                    amount,
                    installments: paymentForm.useInstallments && paymentForm.method === 4
                      ? rebuildPaymentInstallments(amount, paymentForm.installmentCount, paymentForm.paidAt)
                      : paymentForm.installments,
                  });
                }}
              />
            </div>
            <div className="form-field">
              <label htmlFor="payDate">Data do pagamento *</label>
              <input
                id="payDate"
                type="date"
                required
                value={paymentForm.paidAt}
                onChange={(e) => {
                  const paidAt = e.target.value;
                  setPaymentForm({
                    ...paymentForm,
                    paidAt,
                    installments: paymentForm.useInstallments && paymentForm.method === 4
                      ? rebuildPaymentInstallments(paymentForm.amount, paymentForm.installmentCount, paidAt)
                      : paymentForm.installments,
                  });
                }}
              />
            </div>
            <div className="form-field full">
              <label htmlFor="payNotes">Observações</label>
              <textarea
                id="payNotes"
                rows={2}
                placeholder="Ex.: comprovante, parcela, autorização..."
                value={paymentForm.notes}
                onChange={(e) => setPaymentForm({ ...paymentForm, notes: e.target.value })}
              />
            </div>
            <div className="form-field full modal-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setPaymentAccount(null)}>
                Cancelar
              </button>
              <button className="btn" type="submit" disabled={paymentSubmitting}>
                {paymentSubmitting ? 'Salvando...' : 'Confirmar pagamento'}
              </button>
            </div>
          </form>
        )}
      </Modal>

      <Modal
        open={!!historyAccount}
        onClose={() => setHistoryAccount(null)}
        title="Histórico de pagamentos"
        subtitle={historyAccount?.description}
        width="lg"
      >
        {historyLoading ? (
          <p className="field-hint">Carregando...</p>
        ) : paymentHistory.length === 0 ? (
          <p className="field-hint">Nenhum pagamento registrado.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Data</th>
                <th>Valor</th>
                <th>Forma</th>
                <th>Observações</th>
              </tr>
            </thead>
            <tbody>
              {paymentHistory.map((payment) => (
                <tr key={payment.id}>
                  <td>{formatBrDateTime(payment.paidAt)}</td>
                  <td>R$ {payment.amount.toFixed(2)}</td>
                  <td>{paymentMethodLabel(payment.method)}</td>
                  <td>
                    {payment.notes ?? '—'}
                    {payment.installments?.length ? (
                      <div className="payment-history-installments">
                        {payment.installments.map((installment) => (
                          <div key={installment.installmentNumber}>
                            Parcela {installment.installmentNumber}/{installment.installmentCount}: R$ {installment.amount.toFixed(2)} em {formatBrDate(installment.dueDate)}
                          </div>
                        ))}
                      </div>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Modal>

      <Modal
        open={!!pixCharge}
        onClose={() => setPixCharge(null)}
        title="Cobrança PIX"
        subtitle="Copia e cola — confirmação automática via webhook"
        width="lg"
      >
        {pixCharge && (
          <div className="pix-charge-panel">
            <p><strong>Paciente:</strong> {pixCharge.patientName}</p>
            <p><strong>Valor:</strong> R$ {pixCharge.amount.toFixed(2)}</p>
            <p><strong>Status:</strong> {pixChargeStatusLabel(pixCharge.status)}</p>
            <p><strong>ID transação:</strong> <code>{pixCharge.txId}</code></p>
            <p><strong>Válido até:</strong> {formatBrDateTime(pixCharge.expiresAt)}</p>
            <div className="pix-copy-box">
              <label>PIX copia e cola</label>
              <textarea readOnly rows={4} value={pixCharge.copyPasteCode} />
            </div>
            {pixChargeStatusValue(pixCharge.status) === 1 && (
              <div className="modal-actions" style={{ marginTop: 16 }}>
                <button className="btn btn-secondary" type="button" onClick={() => setPixCharge(null)}>Fechar</button>
                <button className="btn" type="button" disabled={pixLoading} onClick={handleSimulatePix}>
                  Simular pagamento (demo)
                </button>
              </div>
            )}
          </div>
        )}
      </Modal>
      </>
      )}
    </>
  );
}

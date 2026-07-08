const API_BASE = import.meta.env.VITE_API_URL ?? '/api';

function handleUnauthorized(): void {
  localStorage.removeItem('hospital_token');
  localStorage.removeItem('hospital_user');
  localStorage.removeItem('hospital_mfa_token');
  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
}

function authHeaders(): HeadersInit {
  const token = localStorage.getItem('hospital_token');
  return {
    'Content-Type': 'application/json',
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
}

async function downloadFile(path: string, fallbackName: string): Promise<void> {
  const token = localStorage.getItem('hospital_token');
  const response = await fetch(`${API_BASE}${path}`, {
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });

  if (response.status === 401) {
    handleUnauthorized();
    throw new Error('Sessão expirada');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Erro ao baixar arquivo' }));
    throw new Error(error.message ?? 'Falha no download');
  }

  const blob = await response.blob();
  const disposition = response.headers.get('Content-Disposition') ?? '';
  const match = disposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
  const fileName = match?.[1]?.replace(/['"]/g, '') || fallbackName;
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      ...authHeaders(),
      ...(options?.headers ?? {}),
    },
  });

  if (response.status === 401) {
    handleUnauthorized();
    throw new Error('Sessão expirada');
  }

  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Erro inesperado' }));
    throw new Error(error.message ?? 'Falha na requisição');
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

export const api = {
  getPatients: (search?: string, page = 1, pageSize = 50, isActive?: boolean | null) => {
    const params = new URLSearchParams({
      search: search ?? '',
      page: String(page),
      pageSize: String(pageSize),
    });
    if (isActive !== undefined && isActive !== null) {
      params.set('isActive', String(isActive));
    }
    return request<PagedResult<PatientDto>>(`/patients?${params.toString()}`);
  },
  quickSearchPatients: (search?: string, take = 10) =>
    request<PatientDto[]>(`/patients/quick-search?search=${encodeURIComponent(search ?? '')}&take=${take}`),
  getPatient: (id: string) => request<PatientDetailDto>(`/patients/${id}`),
  getPatientTimeline: (id: string) => request<PatientTimelineDto>(`/patients/${id}/timeline`),
  checkPatientCpf: (cpf: string, excludePatientId?: string) =>
    request<CpfAvailabilityResult>(
      `/patients/check-cpf?cpf=${encodeURIComponent(cpf)}${excludePatientId ? `&excludePatientId=${excludePatientId}` : ''}`,
    ),
  createPatient: (data: CreatePatientRequest) =>
    request<CreatePatientResult>('/patients', { method: 'POST', body: JSON.stringify(data) }),
  updatePatient: (id: string, data: UpdatePatientRequest) =>
    request<PatientDetailDto>(`/patients/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getAppointments: (date?: string) =>
    request<AppointmentDto[]>(`/appointments${date ? `?date=${date}` : ''}`),
  createAppointment: (data: CreateAppointmentRequest) =>
    request<CreateAppointmentResultDto>('/appointments', { method: 'POST', body: JSON.stringify(data) }),
  updateAppointmentStatus: (id: string, status: number) =>
    request<AppointmentDto>(`/appointments/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getProfessionals: () => request<ProfessionalDto[]>('/catalog/professionals'),
  getProfessionalList: () => request<ProfessionalListDto[]>('/professionals'),
  getProfessional: (id: string) => request<ProfessionalDetailDto>(`/professionals/${id}`),
  createProfessional: (data: CreateProfessionalRequest) =>
    request<ProfessionalDetailDto>('/professionals', { method: 'POST', body: JSON.stringify(data) }),
  updateProfessional: (id: string, data: UpdateProfessionalRequest) =>
    request<ProfessionalDetailDto>(`/professionals/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getMedicalRecord: (patientId: string) =>
    request<MedicalRecordSummaryDto>(`/patients/${patientId}/medical-record`),
  getDigitalRecord: (patientId: string) =>
    request<DigitalRecordSummaryDto>(`/patients/${patientId}/digital-record`),
  addMedicalRecordEntry: (patientId: string, data: CreateMedicalRecordEntryRequest) =>
    request<MedicalRecordEntryDto>(`/patients/${patientId}/medical-record/entries`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateMedicalRecordEntry: (patientId: string, entryId: string, data: UpdateMedicalRecordEntryRequest) =>
    request<MedicalRecordEntryDto>(`/patients/${patientId}/medical-record/entries/${entryId}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  signMedicalRecordEntry: (patientId: string, entryId: string, data: SignMedicalRecordEntryRequest) =>
    request<MedicalRecordEntryDto>(`/patients/${patientId}/medical-record/entries/${entryId}/sign`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getPendingSignatures: (limit = 50) =>
    request<PendingSignatureEntryDto[]>(`/pep/pending-signatures?limit=${limit}`),
  getSignatureAudit: (limit = 50) =>
    request<AuditLogDto[]>(`/pep/signature-audit?limit=${limit}`),
  resolvePatientIdentity: (code: string) =>
    request<PatientIdentityResolveDto>(`/patient-identity/resolve/${encodeURIComponent(code.trim())}`),
  listPatientIdentities: (patientId: string) =>
    request<PatientIdentityDto[]>(`/patients/${patientId}/identity`),
  generatePatientBracelet: (patientId: string, data?: GenerateBraceletRequest) =>
    request<PatientIdentityDto>(`/patients/${patientId}/identity/bracelet`, {
      method: 'POST',
      body: JSON.stringify(data ?? {}),
    }),
  generatePatientLabel: (patientId: string, data: GenerateLabelRequest) =>
    request<PatientIdentityDto>(`/patients/${patientId}/identity/labels`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  registerBedsideVitals: (patientId: string, data: BedsideVitalsRequest) =>
    request<BedsideCareResultDto>(`/bedside/patients/${patientId}/vitals`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  administerBedsideMedication: (patientId: string, data: BedsideMedicationRequest) =>
    request<BedsideCareResultDto>(`/bedside/patients/${patientId}/administer-medication`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getFinancialAccounts: (status?: number, search?: string, page = 1, direction?: number, pageSize = 50) =>
    request<PagedResult<FinancialAccountDto>>(
      `/financial-accounts?${status ? `status=${status}&` : ''}${direction ? `direction=${direction}&` : ''}search=${encodeURIComponent(search ?? '')}&page=${page}&pageSize=${pageSize}`,
    ),
  getPayableCategoryPresets: () =>
    request<PayableCategoryPresetDto[]>('/financial-accounts/payable-presets'),
  getFinancialAccountsByPatient: (patientId: string) =>
    request<FinancialAccountDto[]>(`/financial-accounts/patient/${patientId}`),
  getFinancialAccountSuggestions: (patientId: string) =>
    request<FinancialAccountCreateSuggestionsDto>(`/financial-accounts/suggestions/${patientId}`),
  createFinancialAccount: (data: CreateFinancialAccountRequest) =>
    request<FinancialAccountDto>('/financial-accounts', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getFinancialSummary: () => request<FinancialSummaryDto>('/financial-accounts/summary'),
  getBillingDashboard: () => request<BillingDashboardDto>('/billing/dashboard'),
  getFinancialPayments: (accountId: string) =>
    request<FinancialPaymentDto[]>(`/financial-accounts/${accountId}/payments`),
  registerPayment: (id: string, data: RegisterPaymentRequest) =>
    request<FinancialAccountDto>(`/financial-accounts/${id}/payments`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  cancelFinancialAccount: (id: string) =>
    request<void>(`/financial-accounts/${id}/cancel`, { method: 'POST' }),
  convertFinancialProposal: (id: string) =>
    request<FinancialAccountDto>(`/financial-accounts/${id}/convert-to-billing`, { method: 'POST' }),
  getFinancialCashSessions: (limit = 30) =>
    request<FinancialCashSessionDto[]>(`/financial-cash-sessions?limit=${limit}`),
  getOpenFinancialCashSession: () =>
    request<FinancialCashSessionDto | null>('/financial-cash-sessions/open'),
  openFinancialCashSession: (data: OpenFinancialCashSessionRequest) =>
    request<FinancialCashSessionDto>('/financial-cash-sessions/open', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  closeFinancialCashSession: (id: string, data: CloseFinancialCashSessionRequest) =>
    request<FinancialCashSessionDto>(`/financial-cash-sessions/${id}/close`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getMiscellaneousReceipts: (search?: string, page = 1, pageSize = 50) =>
    request<PagedResult<MiscellaneousReceiptDto>>(
      `/miscellaneous-receipts?search=${encodeURIComponent(search ?? '')}&page=${page}&pageSize=${pageSize}`,
    ),
  getMiscellaneousReceipt: (id: string) =>
    request<MiscellaneousReceiptDto>(`/miscellaneous-receipts/${id}`),
  createMiscellaneousReceipt: (data: CreateMiscellaneousReceiptRequest) =>
    request<MiscellaneousReceiptDto>('/miscellaneous-receipts', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateMiscellaneousReceipt: (id: string, data: UpdateMiscellaneousReceiptRequest) =>
    request<MiscellaneousReceiptDto>(`/miscellaneous-receipts/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  deleteMiscellaneousReceipt: (id: string) =>
    request<void>(`/miscellaneous-receipts/${id}`, { method: 'DELETE' }),
  getVaccineCatalog: (scheduleType?: number) =>
    request<VaccineCatalogDto[]>(
      `/vaccinations/catalog${scheduleType ? `?scheduleType=${scheduleType}` : ''}`,
    ),
  getPatientVaccinations: (patientId: string) =>
    request<PatientVaccinationDto[]>(`/vaccinations/patient/${patientId}`),
  createPatientVaccination: (data: CreatePatientVaccinationRequest) =>
    request<PatientVaccinationDto>('/vaccinations', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getEpidemicDiseaseCatalog: (search?: string) =>
    request<EpidemicDiseaseCatalogDto[]>(
      `/vaccinations/epidemic-diseases?search=${encodeURIComponent(search ?? '')}`,
    ),
  getWardPharmacyBalances: (wardId?: string, lowStockOnly = false) => {
    const params = new URLSearchParams();
    if (wardId) params.set('wardId', wardId);
    if (lowStockOnly) params.set('lowStockOnly', 'true');
    const qs = params.toString();
    return request<WardStockBalanceDto[]>(`/ward-pharmacy/balances${qs ? `?${qs}` : ''}`);
  },
  getWardPharmacyMovements: (options?: { wardId?: string; from?: string; to?: string }) => {
    const params = new URLSearchParams();
    if (options?.wardId) params.set('wardId', options.wardId);
    if (options?.from) params.set('from', options.from);
    if (options?.to) params.set('to', options.to);
    const qs = params.toString();
    return request<WardStockMovementDto[]>(`/ward-pharmacy/movements${qs ? `?${qs}` : ''}`);
  },
  transferWardPharmacyStock: (data: WardStockTransferRequest) =>
    request<WardStockBalanceDto>('/ward-pharmacy/transfer', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  dispenseWardPharmacyStock: (data: WardStockDispenseRequest) =>
    request<WardStockMovementDto>('/ward-pharmacy/dispense', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  createPixCharge: (accountId: string) =>
    request<PixChargeDto>(`/pix/charges/account/${accountId}`, { method: 'POST' }),
  getPixCharge: (id: string) => request<PixChargeDto>(`/pix/charges/${id}`),
  getActivePixCharge: (accountId: string) =>
    request<PixChargeDto>(`/pix/charges/account/${accountId}/active`),
  simulatePixPayment: (chargeId: string) =>
    request<PixChargeDto>(`/pix/charges/${chargeId}/simulate-payment`, { method: 'POST' }),
  getWards: (modality?: number, category?: number) => {
    const params = new URLSearchParams();
    if (modality) params.set('modality', String(modality));
    if (category) params.set('category', String(category));
    const qs = params.toString();
    return request<WardDto[]>(`/hospitalizations/wards${qs ? `?${qs}` : ''}`);
  },
  getBeds: (options?: { wardId?: string; modality?: number; category?: number; status?: number }) => {
    const params = new URLSearchParams();
    if (options?.wardId) params.set('wardId', options.wardId);
    if (options?.modality != null) params.set('modality', String(options.modality));
    if (options?.category != null) params.set('category', String(options.category));
    if (options?.status != null) params.set('status', String(options.status));
    const qs = params.toString();
    return request<BedDto[]>(`/hospitalizations/beds${qs ? `?${qs}` : ''}`);
  },
  getAvailableBedsForPatient: (patientId: string) =>
    request<BedDto[]>(`/hospitalizations/beds/available-for-patient/${patientId}`),
  getHospitalizations: (patientId?: string, scope?: 'active' | 'discharged' | 'deceased' | 'all') => {
    const params = new URLSearchParams();
    if (patientId) params.set('patientId', patientId);
    if (scope && scope !== 'active') {
      const scopeMap = { discharged: 'Discharged', deceased: 'Deceased', all: 'All' } as const;
      params.set('scope', scopeMap[scope]);
    }
    const qs = params.toString();
    return request<HospitalizationDto[]>(`/hospitalizations${qs ? `?${qs}` : ''}`);
  },
  getHospitalizationHubDashboard: (dateFrom?: string, dateTo?: string) => {
    const params = new URLSearchParams();
    if (dateFrom) params.set('dateFrom', dateFrom);
    if (dateTo) params.set('dateTo', dateTo);
    const qs = params.toString();
    return request<HospitalizationHubDashboardDto>(`/hospitalizations/hub/dashboard${qs ? `?${qs}` : ''}`);
  },
  getHospitalizationHubList: (params: HospitalizationHubFilterParams) => {
    const qs = new URLSearchParams();
    if (params.dateFrom) qs.set('dateFrom', params.dateFrom);
    if (params.dateTo) qs.set('dateTo', params.dateTo);
    if (params.patientId) qs.set('patientId', params.patientId);
    if (params.wardId) qs.set('wardId', params.wardId);
    if (params.professionalId) qs.set('professionalId', params.professionalId);
    if (params.modality != null) qs.set('modality', String(params.modality));
    if (params.category != null) qs.set('category', String(params.category));
    if (params.status != null) qs.set('status', String(params.status));
    if (params.search) qs.set('search', params.search);
    if (params.groupId) qs.set('groupId', params.groupId);
    if (params.skip != null) qs.set('skip', String(params.skip));
    if (params.take != null) qs.set('take', String(params.take));
    const q = qs.toString();
    return request<HospitalizationHubListResultDto>(`/hospitalizations/hub${q ? `?${q}` : ''}`);
  },
  closeHospitalizationBillingAccount: (id: string) =>
    request<HospitalizationDto>(`/hospitalizations/${id}/close-billing-account`, { method: 'POST' }),
  admitPatient: (data: AdmitPatientRequest) =>
    request<HospitalizationDto>('/hospitalizations/admit', { method: 'POST', body: JSON.stringify(data) }),
  dischargePatient: (id: string, notes?: string) =>
    request<HospitalizationDto>(`/hospitalizations/${id}/discharge`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    }),
  transferBed: (id: string, data: TransferBedRequest) =>
    request<HospitalizationDto>(`/hospitalizations/${id}/transfer`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getBedTransfers: (limit?: number) =>
    request<BedTransferDto[]>(`/hospitalizations/transfers${limit ? `?limit=${limit}` : ''}`),
  createWard: (data: CreateWardRequest) =>
    request<WardDto>('/hospitalizations/wards', { method: 'POST', body: JSON.stringify(data) }),
  updateWard: (id: string, data: UpdateWardRequest) =>
    request<WardDto>(`/hospitalizations/wards/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deactivateWard: (id: string) =>
    request<void>(`/hospitalizations/wards/${id}`, { method: 'DELETE' }),
  createBed: (data: CreateBedRequest) =>
    request<BedDto>('/hospitalizations/beds', { method: 'POST', body: JSON.stringify(data) }),
  updateBed: (id: string, data: UpdateBedRequest) =>
    request<BedDto>(`/hospitalizations/beds/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  updateBedStatus: (id: string, data: UpdateBedStatusRequest) =>
    request<BedDto>(`/hospitalizations/beds/${id}/status`, { method: 'PATCH', body: JSON.stringify(data) }),
  deactivateBed: (id: string) =>
    request<void>(`/hospitalizations/beds/${id}`, { method: 'DELETE' }),
  getHospitalizationRequests: (options?: { status?: number; patientId?: string }) => {
    const params = new URLSearchParams();
    if (options?.status != null) params.set('status', String(options.status));
    if (options?.patientId) params.set('patientId', options.patientId);
    const qs = params.toString();
    return request<HospitalizationRequestDto[]>(`/hospitalizations/requests${qs ? `?${qs}` : ''}`);
  },
  createHospitalizationRequest: (data: CreateHospitalizationRequestRequest) =>
    request<HospitalizationRequestDto>('/hospitalizations/requests', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  reviewHospitalizationRequest: (id: string, data: ReviewHospitalizationRequestRequest) =>
    request<HospitalizationRequestDto>(`/hospitalizations/requests/${id}/review`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    }),
  cancelHospitalizationRequest: (id: string) =>
    request<HospitalizationRequestDto>(`/hospitalizations/requests/${id}/cancel`, { method: 'POST' }),
  admitFromHospitalizationRequest: (id: string, data: AdmitFromHospitalizationRequestRequest) =>
    request<HospitalizationDto>(`/hospitalizations/requests/${id}/admit`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateHospitalizationSusData: (id: string, data: UpdateHospitalizationSusDataRequest) =>
    request<HospitalizationDto>(`/hospitalizations/${id}/sus-data`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    }),
  getHospitalizationSnippets: (type: 'Reason' | 'Diagnosis' | 1 | 2) =>
    request<HospitalizationSnippetDto[]>(`/hospitalizations/snippets?type=${type}`),
  registerHospitalizationSnippet: (data: { type: 1 | 2; text: string }) =>
    request<HospitalizationSnippetDto>('/hospitalizations/snippets', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getGovIntegrationProfiles: () => request<GovIntegrationProfileDto[]>('/gov-integrations/profiles'),
  lookupCns: (cns: string) => request<CnsLookupResultDto>(`/gov-integrations/cns/${encodeURIComponent(cns)}`),
  lookupCnes: (code: string) => request<CnesEstablishmentDto>(`/gov-integrations/cnes/${encodeURIComponent(code)}`),
  applyCnsToPatient: (patientId: string, cns: string) =>
    request<GovIntegrationActionResultDto>(`/gov-integrations/patients/${patientId}/cns`, {
      method: 'POST',
      body: JSON.stringify({ cns }),
    }),
  previewSihAih: (hospitalizationId: string) =>
    request<SihAihPreviewDto>(`/gov-integrations/sih/aih/${hospitalizationId}`),
  previewSiaDocument: (documentType: string, competence?: string) => {
    const qs = new URLSearchParams({ documentType });
    if (competence) qs.set('competence', competence);
    return request<SiaDocumentPreviewDto>(`/gov-integrations/sia/preview?${qs}`);
  },
  exportSiaDocument: (documentType: string, competence?: string) => {
    const qs = new URLSearchParams({ documentType });
    if (competence) qs.set('competence', competence);
    return downloadFile(`/gov-integrations/sia/export?${qs}`, `SIA_${documentType}_${competence ?? 'export'}.txt`);
  },
  exportSihAihBatch: (competence?: string) => {
    const qs = competence ? `?competence=${encodeURIComponent(competence)}` : '';
    return downloadFile(`/gov-integrations/sih/export${qs}`, `SIH_AIH_${competence ?? 'export'}.txt`);
  },
  exportCihaDocument: (competence?: string) => {
    const qs = competence ? `?competence=${encodeURIComponent(competence)}` : '';
    return downloadFile(`/gov-integrations/ciha/export${qs}`, `CIHA_${competence ?? 'export'}.txt`);
  },
  queryRndsPatient: (patientId: string) =>
    request<RndsPatientSummaryDto>(`/gov-integrations/rnds/patients/${patientId}`),
  getOperatingRooms: () => request<OperatingRoomDto[]>('/surgeries/operating-rooms'),
  getSurgeries: (date?: string) =>
    request<SurgeryDto[]>(`/surgeries${date ? `?date=${date}` : ''}`),
  createSurgery: (data: CreateSurgeryRequest) =>
    request<SurgeryDto>('/surgeries', { method: 'POST', body: JSON.stringify(data) }),
  updateSurgeryStatus: (id: string, status: number) =>
    request<SurgeryDto>(`/surgeries/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }),
  updateSurgerySafetyChecklist: (id: string, data: UpdateSurgerySafetyChecklistRequest) =>
    request<SurgeryDto>(`/surgeries/${id}/safety-checklist`, { method: 'PATCH', body: JSON.stringify(data) }),
  registerPatientDeath: (id: string, data: RegisterPatientDeathRequest) =>
    request<HospitalizationDto>(`/hospitalizations/${id}/register-death`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getProducts: (search?: string, lowStockOnly?: boolean, type?: number) =>
    request<ProductDto[]>(`/inventory/products?search=${encodeURIComponent(search ?? '')}${lowStockOnly ? '&lowStockOnly=true' : ''}${type ? `&type=${type}` : ''}`),
  getProduct: (id: string) => request<ProductDetailDto>(`/inventory/products/${id}`),
  createProduct: (data: CreateProductRequest) =>
    request<ProductDetailDto>('/inventory/products', { method: 'POST', body: JSON.stringify(data) }),
  updateProduct: (id: string, data: UpdateProductRequest) =>
    request<ProductDetailDto>(`/inventory/products/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  registerStockInbound: (data: StockInboundRequest) =>
    request<StockMovementDto>('/inventory/inbound', { method: 'POST', body: JSON.stringify(data) }),
  registerStockOutbound: (data: StockOutboundRequest) =>
    request<StockMovementDto>('/inventory/outbound', { method: 'POST', body: JSON.stringify(data) }),
  getStockMovements: (params?: {
    productId?: string;
    search?: string;
    type?: number;
    from?: string;
    to?: string;
    limit?: number;
  }) => {
    const qs = new URLSearchParams();
    if (params?.productId) qs.set('productId', params.productId);
    if (params?.search) qs.set('search', params.search);
    if (params?.type != null) qs.set('type', String(params.type));
    if (params?.from) qs.set('from', params.from);
    if (params?.to) qs.set('to', params.to);
    if (params?.limit != null) qs.set('limit', String(params.limit));
    const query = qs.toString();
    return request<StockMovementDto[]>(`/inventory/movements${query ? `?${query}` : ''}`);
  },
  getProductBillingRules: (productId: string, priceTable?: string, isActive?: boolean) => {
    const params = new URLSearchParams();
    if (priceTable) params.set('priceTable', priceTable);
    if (isActive != null) params.set('isActive', String(isActive));
    const query = params.toString();
    return request<ProductBillingRuleDto[]>(`/inventory/products/${productId}/billing-rules${query ? `?${query}` : ''}`);
  },
  createProductBillingRule: (productId: string, data: CreateProductBillingRuleRequest) =>
    request<ProductBillingRuleDto>(`/inventory/products/${productId}/billing-rules`, { method: 'POST', body: JSON.stringify(data) }),
  updateProductBillingRule: (ruleId: string, data: UpdateProductBillingRuleRequest) =>
    request<ProductBillingRuleDto>(`/inventory/billing-rules/${ruleId}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteProductBillingRule: (ruleId: string) =>
    request<void>(`/inventory/billing-rules/${ruleId}`, { method: 'DELETE' }),
  getProductKits: (search?: string) =>
    request<ProductKitDto[]>(`/inventory/kits?search=${encodeURIComponent(search ?? '')}`),
  getProductKit: (id: string) => request<ProductKitDetailDto>(`/inventory/kits/${id}`),
  createProductKit: (data: CreateProductKitRequest) =>
    request<ProductKitDetailDto>('/inventory/kits', { method: 'POST', body: JSON.stringify(data) }),
  updateProductKit: (id: string, data: UpdateProductKitRequest) =>
    request<ProductKitDetailDto>(`/inventory/kits/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteProductKit: (id: string) =>
    request<void>(`/inventory/kits/${id}`, { method: 'DELETE' }),
  getInventoryLookupItems: (type: number, search?: string) =>
    request<InventoryLookupItemDto[]>(
      `/inventory/config/lookup-items?type=${type}${search ? `&search=${encodeURIComponent(search)}` : ''}`,
    ),
  createInventoryLookupItem: (type: number, data: CreateInventoryLookupItemRequest) =>
    request<InventoryLookupItemDto>(`/inventory/config/lookup-items?type=${type}`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateInventoryLookupItem: (id: string, data: UpdateInventoryLookupItemRequest) =>
    request<InventoryLookupItemDto>(`/inventory/config/lookup-items/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  deleteInventoryLookupItem: (id: string) =>
    request<void>(`/inventory/config/lookup-items/${id}`, { method: 'DELETE' }),
  getMedicationInsuranceMappings: (search?: string) =>
    request<MedicationInsuranceMappingDto[]>(
      `/inventory/config/medication-mappings${search ? `?search=${encodeURIComponent(search)}` : ''}`,
    ),
  createMedicationInsuranceMapping: (data: CreateMedicationInsuranceMappingRequest) =>
    request<MedicationInsuranceMappingDto>('/inventory/config/medication-mappings', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateMedicationInsuranceMapping: (id: string, data: UpdateMedicationInsuranceMappingRequest) =>
    request<MedicationInsuranceMappingDto>(`/inventory/config/medication-mappings/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  deleteMedicationInsuranceMapping: (id: string) =>
    request<void>(`/inventory/config/medication-mappings/${id}`, { method: 'DELETE' }),
  getStockRequisitions: (status?: number, priority?: number, dueDateBefore?: string) => {
    const params = new URLSearchParams();
    if (status != null) params.set('status', String(status));
    if (priority != null) params.set('priority', String(priority));
    if (dueDateBefore) params.set('dueDateBefore', dueDateBefore);
    const query = params.toString();
    return request<StockRequisitionDto[]>(`/inventory/requisitions${query ? `?${query}` : ''}`);
  },
  getStockRequisition: (id: string) => request<StockRequisitionDetailDto>(`/inventory/requisitions/${id}`),
  createStockRequisition: (data: CreateStockRequisitionRequest) =>
    request<StockRequisitionDetailDto>('/inventory/requisitions', { method: 'POST', body: JSON.stringify(data) }),
  updateStockRequisition: (id: string, data: UpdateStockRequisitionRequest) =>
    request<StockRequisitionDetailDto>(`/inventory/requisitions/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  approveStockRequisition: (id: string) =>
    request<StockRequisitionDetailDto>(`/inventory/requisitions/${id}/approve`, { method: 'POST' }),
  fulfillStockRequisition: (id: string) =>
    request<StockRequisitionDetailDto>(`/inventory/requisitions/${id}/fulfill`, { method: 'POST' }),
  cancelStockRequisition: (id: string) =>
    request<void>(`/inventory/requisitions/${id}/cancel`, { method: 'POST' }),
  denyStockRequisition: (id: string, data: DenyStockRequisitionRequest) =>
    request<StockRequisitionDetailDto>(`/inventory/requisitions/${id}/deny`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    }),
  getWarehouseDashboard: () => request<WarehouseDashboardDto>('/warehouse/dashboard'),
  getWarehouseLots: (productId?: string, expiringWithinDays?: number) => {
    const params = new URLSearchParams();
    if (productId) params.set('productId', productId);
    if (expiringWithinDays != null) params.set('expiringWithinDays', String(expiringWithinDays));
    const query = params.toString();
    return request<ProductLotDto[]>(`/warehouse/lots${query ? `?${query}` : ''}`);
  },
  getWarehouseExpiringLots: (days = 30) =>
    request<ProductLotDto[]>(`/warehouse/expiring?days=${days}`),
  getWarehouseLowStock: () => request<ProductDto[]>('/warehouse/low-stock'),
  getWarehouseConsumptionBySector: (from: string, to: string) =>
    request<SectorConsumptionDto[]>(`/warehouse/consumption-by-sector?from=${from}&to=${to}`),
  createWarehouseReceipt: (data: CreateStockReceiptRequest) =>
    request<StockReceiptDto>('/warehouse/receipts', { method: 'POST', body: JSON.stringify(data) }),
  createWarehouseIssue: (data: CreateStockIssueRequest) =>
    request<StockIssueDto>('/warehouse/issues', { method: 'POST', body: JSON.stringify(data) }),
  getPatientClinicalAlerts: (patientId: string) =>
    request<PatientClinicalAlertsDto>(`/clinical-intelligence/patients/${patientId}/alerts`),
  getStockReplenishmentSuggestions: () =>
    request<StockReplenishmentSuggestionDto[]>('/clinical-intelligence/stock/replenishment'),
  getOperationalInsights: () =>
    request<OperationalInsightsDto>('/clinical-intelligence/operational'),
  analyzePrescriptionSafety: (data: { patientId: string; prescriptionContent: string }) =>
    request<PrescriptionSafetyResultDto>('/ai/prescription/safety', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  analyzeHospitalDashboardInsight: () =>
    request<AiInsightReportDto>('/ai/insights/hospital-dashboard'),
  dispenseMedication: (data: DispenseMedicationRequest) =>
    request<PharmacyDispensingDto>('/pharmacy/dispense', { method: 'POST', body: JSON.stringify(data) }),
  getDispensings: (patientId?: string) =>
    request<PharmacyDispensingDto[]>(`/pharmacy/dispensings${patientId ? `?patientId=${patientId}` : ''}`),
  reversePharmacyDispensing: (id: string, data: { quantity: number; reason?: string }) =>
    request<PharmacyDispensingReversalDto>(`/pharmacy/dispensings/${id}/reverse`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getHealthInsurances: () => request<HealthInsuranceDto[]>('/catalog/health-insurances'),
  getLabExams: (specialtyId?: string) =>
    request<LabExamCatalogDto[]>(`/lab/exams${specialtyId ? `?specialtyId=${specialtyId}` : ''}`),
  getClinicalCatalog: (specialtyId?: string) =>
    request<SpecialtyClinicalCatalogDto>(`/clinical-catalog${specialtyId ? `?specialtyId=${specialtyId}` : ''}`),
  getClinicalCatalogByProfessional: (professionalId: string) =>
    request<SpecialtyClinicalCatalogDto>(`/clinical-catalog/by-professional/${professionalId}`),
  searchMedications: (search?: string, page = 1, pageSize = 50, referenceOnly = false) =>
    request<PagedResult<MedicationCatalogDto>>(
      `/clinical-catalog/medications?search=${encodeURIComponent(search ?? '')}&page=${page}&pageSize=${pageSize}&referenceOnly=${referenceOnly}`,
    ),
  getMedications: (search?: string) =>
    request<PagedResult<MedicationCatalogDto>>(
      `/clinical-catalog/medications?search=${encodeURIComponent(search ?? '')}&page=1&pageSize=500`,
    ).then((r) => r.items),
  getMedication: (id: string) => request<MedicationCatalogDto>(`/clinical-catalog/medications/${id}`),
  searchBulario: (nome?: string, pagina = 1, pageSize = 50) =>
    request<BularioSearchResultDto>(
      `/bulario/search?nome=${encodeURIComponent(nome ?? '')}&pagina=${pagina}&pageSize=${pageSize}`,
    ),
  getBularioStats: () => request<BularioStatsDto>('/bulario/stats'),
  searchAnvisaBulario: async (nome: string, pagina = 1) => {
    const response = await fetch(
      `${API_BASE}/bulario/pesquisar?nome=${encodeURIComponent(nome)}&pagina=${pagina}`,
      { headers: authHeaders() },
    );
    if (response.status === 401) {
      handleUnauthorized();
      throw new Error('Sessão expirada');
    }
    if (response.status === 503) return null;
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Falha na consulta ANVISA' }));
      throw new Error(error.message ?? 'Falha na consulta ANVISA');
    }
    return response.json() as Promise<AnvisaBulaSearchResponse>;
  },
  getAnvisaMedication: (numProcesso: string) =>
    request<AnvisaMedicationDetailDto>(`/bulario/medicamento/${encodeURIComponent(numProcesso)}`),
  getAnvisaPdfBlob: async (bulaId: string) => {
    const response = await fetch(
      `${API_BASE}/bulario/pdf?id=${encodeURIComponent(bulaId)}`,
      { headers: authHeaders() },
    );
    if (!response.ok) throw new Error('PDF da bula não disponível');
    return response.blob();
  },
  getCid10Catalog: (search?: string) =>
    request<Cid10CatalogItemDto[]>(`/clinical-catalog/cid10${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  getCid10Children: (parentCode?: string) =>
    request<Cid10CatalogItemDto[]>(
      `/clinical-catalog/cid10/children${parentCode ? `?parentCode=${encodeURIComponent(parentCode)}` : ''}`,
    ),
  getAdministrationRoutes: () => request<AdministrationRouteDto[]>('/clinical-catalog/administration-routes'),
  getPatientReferenceCatalog: (type: PatientReferenceCatalogType) =>
    request<PatientReferenceCatalogItemDto[]>(`/clinical-catalog/patient-reference?type=${type}`),
  getHospitalCatalogTypes: () =>
    request<HospitalReferenceCatalogTypeInfoDto[]>('/hospital-catalog/types'),
  getHospitalCatalogSummary: () =>
    request<HospitalReferenceCatalogSummaryDto[]>('/hospital-catalog/summary'),
  getHospitalCatalogItems: (
    type: HospitalReferenceCatalogType,
    options?: { group?: string; search?: string },
  ) => {
    const params = new URLSearchParams({ type: String(type) });
    if (options?.group) params.set('group', options.group);
    if (options?.search) params.set('search', options.search);
    return request<HospitalReferenceCatalogItemDto[]>(`/hospital-catalog?${params}`);
  },
  getHospitalCatalogGroups: (type: HospitalReferenceCatalogType) =>
    request<HospitalReferenceCatalogGroupDto[]>(`/hospital-catalog/groups?type=${type}`),
  getTvSignageMonitor: () => request<TvMonitorSummaryDto>('/tv-signage/monitor'),
  getTvDisplays: () => request<TvDisplayDto[]>('/tv-signage/displays'),
  createTvDisplay: (data: CreateTvDisplayRequest) =>
    request<TvDisplayDto>('/tv-signage/displays', { method: 'POST', body: JSON.stringify(data) }),
  updateTvDisplay: (id: string, data: UpdateTvDisplayRequest) =>
    request<TvDisplayDto>(`/tv-signage/displays/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteTvDisplay: (id: string) => request<void>(`/tv-signage/displays/${id}`, { method: 'DELETE' }),
  regenerateTvDisplayToken: (id: string) =>
    request<{ token: string }>(`/tv-signage/displays/${id}/regenerate-token`, { method: 'POST' }),
  getTvLayouts: () => request<TvLayoutDto[]>('/tv-signage/layouts'),
  createTvLayout: (data: CreateTvLayoutRequest) =>
    request<TvLayoutDto>('/tv-signage/layouts', { method: 'POST', body: JSON.stringify(data) }),
  updateTvLayout: (id: string, data: UpdateTvLayoutRequest) =>
    request<TvLayoutDto>(`/tv-signage/layouts/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getTvMedia: () => request<TvMediaDto[]>('/tv-signage/media'),
  uploadTvMedia: async (form: FormData) => {
    const token = localStorage.getItem('hospital_token');
    const response = await fetch(`${API_BASE}/tv-signage/media`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    });
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Falha no upload' }));
      throw new Error(error.message ?? 'Falha no upload');
    }
    return response.json() as Promise<TvMediaDto>;
  },
  deleteTvMedia: (id: string) => request<void>(`/tv-signage/media/${id}`, { method: 'DELETE' }),
  getTvNews: () => request<TvNewsDto[]>('/tv-signage/news'),
  createTvNews: (data: CreateTvNewsRequest) =>
    request<TvNewsDto>('/tv-signage/news', { method: 'POST', body: JSON.stringify(data) }),
  deleteTvNews: (id: string) => request<void>(`/tv-signage/news/${id}`, { method: 'DELETE' }),
  getTvAnnouncements: () => request<TvAnnouncementDto[]>('/tv-signage/announcements'),
  createTvAnnouncement: (data: CreateTvAnnouncementRequest) =>
    request<TvAnnouncementDto>('/tv-signage/announcements', { method: 'POST', body: JSON.stringify(data) }),
  deleteTvAnnouncement: (id: string) => request<void>(`/tv-signage/announcements/${id}`, { method: 'DELETE' }),
  getTvCalls: (limit = 50) => request<TvQueueCallDto[]>(`/tv-signage/calls?limit=${limit}`),
  callTvQueue: (data: CallTvQueueRequest) =>
    request<TvQueueCallDto>('/tv-signage/calls', { method: 'POST', body: JSON.stringify(data) }),
  callTvKioskTicket: (kioskTicketId: string, data: CallKioskTicketRequest) =>
    request<TvQueueCallDto>(`/tv-signage/kiosk/${kioskTicketId}/call`, { method: 'POST', body: JSON.stringify(data) }),
  callKioskTicketOnTv: (kioskTicketId: string, data: CallKioskTicketRequest) =>
    request<TvQueueCallDto>(`/physical-access/kiosk/tickets/${kioskTicketId}/call`, { method: 'POST', body: JSON.stringify(data) }),
  getTvCampaigns: () => request<TvCampaignDto[]>('/tv-signage/campaigns'),
  createTvCampaign: (data: CreateTvCampaignRequest) =>
    request<TvCampaignDto>('/tv-signage/campaigns', { method: 'POST', body: JSON.stringify(data) }),
  updateTvCampaign: (id: string, data: UpdateTvCampaignRequest) =>
    request<TvCampaignDto>(`/tv-signage/campaigns/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteTvCampaign: (id: string) => request<void>(`/tv-signage/campaigns/${id}`, { method: 'DELETE' }),
  getTvSpeechProvider: () => request<{ provider: string }>('/tv-signage/speech/provider'),
  getTvPlayerState: (slug: string, token: string) =>
    fetch(`${API_BASE}/tv-signage/player/${encodeURIComponent(slug)}?token=${encodeURIComponent(token)}`).then(async (response) => {
      if (!response.ok) throw new Error('TV não encontrada ou token inválido');
      return response.json() as Promise<TvPlayerStateDto>;
    }),
  sendTvHeartbeat: (slug: string, token: string, data: TvHeartbeatRequest) =>
    fetch(`${API_BASE}/tv-signage/player/${encodeURIComponent(slug)}/heartbeat?token=${encodeURIComponent(token)}`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data),
    }),
  reserveBed: (bedId: string, data: ReserveBedRequest) =>
    request<BedEventDto>(`/hospitalizations/beds/${bedId}/reserve`, { method: 'POST', body: JSON.stringify(data) }),
  blockBed: (bedId: string, data: BlockBedRequest) =>
    request<BedEventDto>(`/hospitalizations/beds/${bedId}/block`, { method: 'POST', body: JSON.stringify(data) }),
  releaseBed: (bedId: string, data: ReleaseBedRequest) =>
    request<BedEventDto>(`/hospitalizations/beds/${bedId}/release`, { method: 'POST', body: JSON.stringify(data) }),
  getBedEvents: (bedId?: string, activeOnly = true) => {
    const params = new URLSearchParams();
    if (bedId) params.set('bedId', bedId);
    if (!activeOnly) params.set('activeOnly', 'false');
    const q = params.toString();
    return request<BedEventDto[]>(`/hospitalizations/bed-events${q ? `?${q}` : ''}`);
  },
  getLabOrders: (status?: number) => request<LabOrderDto[]>(`/lab/orders${status ? `?status=${status}` : ''}`),
  createLabOrder: (data: CreateLabOrderRequest) =>
    request<LabOrderDto>('/lab/orders', { method: 'POST', body: JSON.stringify(data) }),
  registerLabResult: (data: RegisterLabResultRequest) =>
    request<LabResultDto>('/lab/results', { method: 'POST', body: JSON.stringify(data) }),
  getImagingStudies: () => request<ImagingStudyDto[]>('/imaging/studies'),
  createImagingStudy: (data: CreateImagingStudyRequest) =>
    request<ImagingStudyDto>('/imaging/studies', { method: 'POST', body: JSON.stringify(data) }),
  updateImagingStatus: (id: string, status: number) =>
    request<ImagingStudyDto>(`/imaging/studies/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }),
  registerImagingReport: (id: string, data: RegisterImagingReportRequest) =>
    request<ImagingStudyDto>(`/imaging/studies/${id}/report`, { method: 'POST', body: JSON.stringify(data) }),
  getDepartments: () => request<DepartmentDto[]>('/hr/departments'),
  getHrDashboard: () => request<HrDashboardDto>('/hr/dashboard'),
  getEmployees: () => request<EmployeeDto[]>('/hr/employees'),
  getEmployee: (id: string) => request<EmployeeDetailDto>(`/hr/employees/${id}`),
  createEmployee: (data: CreateEmployeeRequest) =>
    request<EmployeeDetailDto>('/hr/employees', { method: 'POST', body: JSON.stringify(data) }),
  updateEmployee: (id: string, data: UpdateEmployeeRequest) =>
    request<EmployeeDetailDto>(`/hr/employees/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getShifts: (params?: { date?: string; from?: string; to?: string; shiftType?: number }) => {
    const qs = new URLSearchParams();
    if (params?.date) qs.set('date', params.date);
    if (params?.from) qs.set('from', params.from);
    if (params?.to) qs.set('to', params.to);
    if (params?.shiftType != null) qs.set('shiftType', String(params.shiftType));
    const q = qs.toString();
    return request<EmployeeShiftDto[]>(`/hr/shifts${q ? `?${q}` : ''}`);
  },
  createShift: (data: CreateShiftRequest) =>
    request<EmployeeShiftDto>('/hr/shifts', { method: 'POST', body: JSON.stringify(data) }),
  deleteShift: (id: string) =>
    request<void>(`/hr/shifts/${id}`, { method: 'DELETE' }),
  getHrEvents: (type?: number) =>
    request<EmployeeHrEventDto[]>(`/hr/events${type != null ? `?type=${type}` : ''}`),
  createHrEvent: (data: CreateEmployeeHrEventRequest) =>
    request<EmployeeHrEventDto>('/hr/events', { method: 'POST', body: JSON.stringify(data) }),
  getTpaAdministrators: () => request<TpaAdministratorDto[]>('/admin-ext/tpa/administrators'),
  createTpaAdministrator: (data: CreateTpaAdministratorRequest) =>
    request<TpaAdministratorDto>('/admin-ext/tpa/administrators', { method: 'POST', body: JSON.stringify(data) }),
  getTpaClaims: (params?: { administratorId?: string; status?: number }) => {
    const qs = new URLSearchParams();
    if (params?.administratorId) qs.set('administratorId', params.administratorId);
    if (params?.status != null) qs.set('status', String(params.status));
    const q = qs.toString();
    return request<TpaClaimDto[]>(`/admin-ext/tpa/claims${q ? `?${q}` : ''}`);
  },
  createTpaClaim: (data: CreateTpaClaimRequest) =>
    request<TpaClaimDto>('/admin-ext/tpa/claims', { method: 'POST', body: JSON.stringify(data) }),
  updateTpaClaimStatus: (id: string, data: UpdateTpaClaimStatusRequest) =>
    request<TpaClaimDto>(`/admin-ext/tpa/claims/${id}/status`, { method: 'PUT', body: JSON.stringify(data) }),
  getTpaReport: () => request<TpaReportDto>('/admin-ext/tpa/report'),
  getPayrollRuns: () => request<PayrollRunDto[]>('/admin-ext/payroll/runs'),
  getPayrollRun: (id: string) => request<PayrollRunDto>(`/admin-ext/payroll/runs/${id}`),
  generatePayrollRun: (data: GeneratePayrollRunRequest) =>
    request<PayrollRunDto>('/admin-ext/payroll/runs/generate', { method: 'POST', body: JSON.stringify(data) }),
  updatePayrollRunStatus: (id: string, data: UpdatePayrollRunStatusRequest) =>
    request<PayrollRunDto>(`/admin-ext/payroll/runs/${id}/status`, { method: 'PUT', body: JSON.stringify(data) }),
  updatePayrollItemLines: (runId: string, itemId: string, data: UpdatePayrollItemLinesRequest) =>
    request<PayrollItemDto>(`/admin-ext/payroll/runs/${runId}/items/${itemId}/lines`, { method: 'PUT', body: JSON.stringify(data) }),
  getPayrollSlip: (runId: string, employeeId: string) =>
    request<PayrollSlipDto>(`/admin-ext/payroll/runs/${runId}/slips/${employeeId}`),
  getPayrollMonthlySummary: (year: number, month: number) =>
    request<PayrollMonthlySummaryDto>(`/admin-ext/payroll/summary?year=${year}&month=${month}`),
  getPharmacyBillingEntries: (paid?: boolean) =>
    request<PharmacyBillingEntryDto[]>(`/admin-ext/pharmacy-billing${paid == null ? '' : `?paid=${paid}`}`),
  createPharmacyBillingEntry: (data: CreatePharmacyBillingEntryRequest) =>
    request<PharmacyBillingEntryDto>('/admin-ext/pharmacy-billing', { method: 'POST', body: JSON.stringify(data) }),
  getBirthRegistrations: () => request<BirthRegistrationDto[]>('/admin-ext/birth-registrations'),
  createBirthRegistration: (data: CreateBirthRegistrationRequest) =>
    request<BirthRegistrationDto>('/admin-ext/birth-registrations', { method: 'POST', body: JSON.stringify(data) }),
  getOperationalDashboard: (params?: { date?: string; professionalId?: string }) => {
    const qs = new URLSearchParams();
    if (params?.date) qs.set('date', params.date);
    if (params?.professionalId) qs.set('professionalId', params.professionalId);
    const query = qs.toString();
    return request<OperationalDashboardDto>(`/dashboard/operational${query ? `?${query}` : ''}`);
  },
  getCommandCenterDashboard: () => request<CommandCenterDashboardDto>('/command-center/dashboard'),
  getCommandCenterQueue: () => request<OperationsQueueSnapshotDto>('/command-center/queue'),
  getBusinessRules: (implementedOnly?: boolean) => {
    const qs = implementedOnly ? '?implementedOnly=true' : '';
    return request<BusinessRuleDto[]>(`/business-rules${qs}`);
  },
  getOfficialUpdatesDashboard: () => request<OfficialUpdatesDashboardDto>('/official-updates'),
  checkAllOfficialUpdates: () =>
    request<OfficialUpdatesDashboardDto>('/official-updates/check-all', { method: 'POST' }),
  updateOfficialSource: (sourceType: string) =>
    request<OfficialUpdateActionResultDto>(`/official-updates/update/${encodeURIComponent(sourceType)}`, {
      method: 'POST',
    }),
  getOfficialUpdateLogs: (take = 50, sourceType?: string) => {
    const qs = new URLSearchParams({ take: String(take) });
    if (sourceType) qs.set('sourceType', sourceType);
    return request<IntegrationLogDto[]>(`/official-updates/logs?${qs}`);
  },
  getBiDashboard: () => request<BiDashboardDto>('/bi/dashboard'),
  getMimicResearchStatus: () => request<MimicResearchStatusDto>('/research/mimic/status'),
  getMimicEtlStatus: () => request<MimicEtlStatusDto>('/research/mimic/etl/status'),
  getMimicVitals: (subjectId: number, limit = 50) =>
    request<MimicVitalsQueryResultDto>(`/research/mimic/vitals?subjectId=${subjectId}&limit=${limit}`),
  triggerMimicSubsetImport: (maxSubjects?: number) => {
    const qs = maxSubjects != null ? `?maxSubjects=${maxSubjects}` : '';
    return request<MimicEtlTriggerResultDto>(`/research/mimic/etl/import${qs}`, { method: 'POST' });
  },
  getReportsSummary: () => request<ReportCatalogSummaryDto>('/reports/catalog/summary'),
  getReportsCatalog: (params?: { module?: string; essentialOnly?: boolean; implementedOnly?: boolean; search?: string }) => {
    const qs = new URLSearchParams();
    if (params?.module) qs.set('module', params.module);
    if (params?.essentialOnly) qs.set('essentialOnly', 'true');
    if (params?.implementedOnly) qs.set('implementedOnly', 'true');
    if (params?.search) qs.set('search', params.search);
    const q = qs.toString();
    return request<ReportCatalogItemDto[]>(`/reports/catalog${q ? `?${q}` : ''}`);
  },
  runReport: (code: string, params?: ReportFilterParams) => {
    const qs = new URLSearchParams();
    if (params?.dateFrom) qs.set('dateFrom', params.dateFrom);
    if (params?.dateTo) qs.set('dateTo', params.dateTo);
    if (params?.professionalId) qs.set('professionalId', params.professionalId);
    if (params?.specialtyId) qs.set('specialtyId', params.specialtyId);
    if (params?.healthInsuranceId) qs.set('healthInsuranceId', params.healthInsuranceId);
    if (params?.patientId) qs.set('patientId', params.patientId);
    if (params?.tpaAdministratorId) qs.set('tpaAdministratorId', params.tpaAdministratorId);
    if (params?.year != null) qs.set('year', String(params.year));
    if (params?.month != null) qs.set('month', String(params.month));
    if (params?.department) qs.set('department', params.department);
    const q = qs.toString();
    return request<ReportResultDto>(`/reports/${encodeURIComponent(code)}${q ? `?${q}` : ''}`);
  },
  getTissGuideTypes: () => request<TissGuideTypeCatalogDto[]>('/tiss/guide-types'),
  getTissGuides: (status?: number, patientId?: string, search?: string) => {
    const params = new URLSearchParams();
    if (status) params.set('status', String(status));
    if (patientId) params.set('patientId', patientId);
    if (search) params.set('search', search);
    const qs = params.toString();
    return request<TissGuideDto[]>(`/tiss/guides${qs ? `?${qs}` : ''}`);
  },
  getGuidesHubDashboard: (dateFrom?: string, dateTo?: string) => {
    const params = new URLSearchParams();
    if (dateFrom) params.set('dateFrom', dateFrom);
    if (dateTo) params.set('dateTo', dateTo);
    const qs = params.toString();
    return request<GuidesHubDashboardDto>(`/guides/dashboard${qs ? `?${qs}` : ''}`);
  },
  getGuidesHubList: (params: GuidesHubFilterParams) => {
    const qs = new URLSearchParams();
    if (params.dateFrom) qs.set('dateFrom', params.dateFrom);
    if (params.dateTo) qs.set('dateTo', params.dateTo);
    if (params.patientId) qs.set('patientId', params.patientId);
    if (params.healthInsuranceId) qs.set('healthInsuranceId', params.healthInsuranceId);
    if (params.professionalId) qs.set('professionalId', params.professionalId);
    if (params.specialtyId) qs.set('specialtyId', params.specialtyId);
    if (params.procedureSearch) qs.set('procedureSearch', params.procedureSearch);
    if (params.guideNumber) qs.set('guideNumber', params.guideNumber);
    if (params.status) qs.set('status', params.status);
    if (params.guideType) qs.set('guideType', params.guideType);
    if (params.groupId) qs.set('groupId', params.groupId);
    if (params.serviceUnit) qs.set('serviceUnit', params.serviceUnit);
    if (params.serviceUnitId) qs.set('serviceUnitId', params.serviceUnitId);
    if (params.skip != null) qs.set('skip', String(params.skip));
    if (params.take != null) qs.set('take', String(params.take));
    const q = qs.toString();
    return request<GuidesHubListResultDto>(`/guides${q ? `?${q}` : ''}`);
  },
  getGuideHistory: (id: string, source?: string) => {
    const qs = source ? `?source=${encodeURIComponent(source)}` : '';
    return request<GuideHistoryEntryDto[]>(`/guides/${id}/history${qs}`);
  },
  duplicateGuide: (id: string) =>
    request<TissGuideDto>(`/guides/${id}/duplicate`, { method: 'POST' }),
  getServiceUnits: () => request<ServiceUnitDto[]>('/guides/service-units'),
  createServiceUnit: (data: CreateServiceUnitRequest) =>
    request<ServiceUnitDto>('/guides/service-units', { method: 'POST', body: JSON.stringify(data) }),
  updateServiceUnit: (id: string, data: UpdateServiceUnitRequest) =>
    request<ServiceUnitDto>(`/guides/service-units/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getSusGuides: (params?: SusGuideFilterParams) => {
    const qs = new URLSearchParams();
    if (params?.dateFrom) qs.set('dateFrom', params.dateFrom);
    if (params?.dateTo) qs.set('dateTo', params.dateTo);
    if (params?.patientId) qs.set('patientId', params.patientId);
    if (params?.professionalId) qs.set('professionalId', params.professionalId);
    if (params?.serviceUnitId) qs.set('serviceUnitId', params.serviceUnitId);
    if (params?.guideType != null) qs.set('guideType', String(params.guideType));
    if (params?.status != null) qs.set('status', String(params.status));
    if (params?.guideNumber) qs.set('guideNumber', params.guideNumber);
    if (params?.procedureSearch) qs.set('procedureSearch', params.procedureSearch);
    if (params?.skip != null) qs.set('skip', String(params.skip));
    if (params?.take != null) qs.set('take', String(params.take));
    const q = qs.toString();
    return request<SusGuideListResultDto>(`/guides/sus${q ? `?${q}` : ''}`);
  },
  getSusGuide: (id: string) => request<SusGuideDto>(`/guides/sus/${id}`),
  createSusGuide: (data: CreateSusGuideRequest) =>
    request<SusGuideDto>('/guides/sus', { method: 'POST', body: JSON.stringify(data) }),
  updateSusGuide: (id: string, data: UpdateSusGuideRequest) =>
    request<SusGuideDto>(`/guides/sus/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  cancelSusGuide: (id: string) =>
    request<SusGuideDto>(`/guides/sus/${id}/cancel`, { method: 'POST' }),
  submitSusGuide: (id: string) =>
    request<SusGuideDto>(`/guides/sus/${id}/submit`, { method: 'POST' }),
  authorizeSusGuide: (id: string, authorizationNumber?: string) => {
    const qs = authorizationNumber ? `?authorizationNumber=${encodeURIComponent(authorizationNumber)}` : '';
    return request<SusGuideDto>(`/guides/sus/${id}/authorize${qs}`, { method: 'POST' });
  },
  duplicateSusGuide: (id: string) =>
    request<SusGuideDto>(`/guides/sus/${id}/duplicate`, { method: 'POST' }),
  getTissGuide: (id: string) => request<TissGuideDto>(`/tiss/guides/${id}`),
  createTissGuide: (data: CreateTissGuideRequest) =>
    request<TissGuideDto>('/tiss/guides', { method: 'POST', body: JSON.stringify(data) }),
  updateTissGuide: (id: string, data: UpdateTissGuideRequest) =>
    request<TissGuideDto>(`/tiss/guides/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteTissGuide: (id: string) => request<void>(`/tiss/guides/${id}`, { method: 'DELETE' }),
  closeTissGuideAccount: (id: string) =>
    request<TissGuideDto>(`/tiss/guides/${id}/close-account`, { method: 'POST' }),
  sendTissGuide: (id: string) => request<TissGuideDto>(`/tiss/guides/${id}/send`, { method: 'POST' }),
  cancelTissGuide: (id: string) => request<TissGuideDto>(`/tiss/guides/${id}/cancel`, { method: 'POST' }),
  markTissGuidePaid: (id: string) => request<TissGuideDto>(`/tiss/guides/${id}/mark-paid`, { method: 'POST' }),
  registerTissGlosa: (guideId: string, data: RegisterGlosaRequest) =>
    request<TissGlosaDto>(`/tiss/guides/${guideId}/glosas`, { method: 'POST', body: JSON.stringify(data) }),
  updateTissGlosa: (glosaId: string, data: UpdateGlosaRequest) =>
    request<TissGlosaDto>(`/tiss/glosas/${glosaId}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteTissGlosa: (glosaId: string) => request<void>(`/tiss/glosas/${glosaId}`, { method: 'DELETE' }),
  resolveTissGlosa: (glosaId: string) =>
    request<TissGlosaDto>(`/tiss/glosas/${glosaId}/resolve`, { method: 'POST' }),
  contestTissGlosa: (glosaId: string, data: ContestGlosaRequest) =>
    request<TissGlosaDto>(`/tiss/glosas/${glosaId}/contest`, { method: 'POST', body: JSON.stringify(data) }),
  searchTuss: (q: string) =>
    request<TussSearchResultDto[]>(`/tiss/tuss-search?q=${encodeURIComponent(q)}`),
  getSuggestedTissItems: (data: SuggestedGuideItemsRequest) =>
    request<TissGuideItemRequest[]>('/tiss/suggested-items', { method: 'POST', body: JSON.stringify(data) }),
  getTissGuidePrefill: (data: GuidePrefillRequest) =>
    request<TissGuidePrefillDto>('/tiss/guide-prefill', { method: 'POST', body: JSON.stringify(data) }),
  getClinicalSources: (params?: {
    patientId?: string;
    documentKind?: number;
    guideType?: number;
    reportCode?: string;
    pendingOnly?: boolean;
  }) => {
    const qs = new URLSearchParams();
    if (params?.patientId) qs.set('patientId', params.patientId);
    if (params?.documentKind != null) qs.set('documentKind', String(params.documentKind));
    if (params?.guideType != null) qs.set('guideType', String(params.guideType));
    if (params?.reportCode) qs.set('reportCode', params.reportCode);
    if (params?.pendingOnly) qs.set('pendingOnly', 'true');
    const q = qs.toString();
    return request<TissClinicalSourceDto[]>(`/tiss/clinical-sources${q ? `?${q}` : ''}`);
  },
  getClinicalSource: (id: string) => request<TissClinicalSourceDto>(`/tiss/clinical-sources/${id}`),
  lookupClinicalSource: (data: ClinicalSourceLookupRequest) =>
    request<TissClinicalSourceDto>('/tiss/clinical-sources/lookup', { method: 'POST', body: JSON.stringify(data) }),
  upsertClinicalSource: (data: UpsertTissClinicalSourceRequest) =>
    request<TissClinicalSourceDto>('/tiss/clinical-sources', { method: 'POST', body: JSON.stringify(data) }),
  linkClinicalSourceGuide: (id: string, tissGuideId: string) =>
    request<TissClinicalSourceDto>(`/tiss/clinical-sources/${id}/link-guide`, {
      method: 'POST',
      body: JSON.stringify({ tissGuideId }),
    }),
  linkClinicalSourceArtifact: (id: string, artifactJson: string) =>
    request<TissClinicalSourceDto>(`/tiss/clinical-sources/${id}/link-artifact`, {
      method: 'POST',
      body: JSON.stringify({ artifactJson }),
    }),
  lookupTissProcedure: (q: string) =>
    request<ProcedureLookupDto[]>(`/tiss/procedure-lookup?q=${encodeURIComponent(q)}`),
  getBillingCatalogSummary: () => request<BillingCatalogSummaryDto>('/tiss/billing-catalog-summary'),
  checkEligibility: (data: EligibilityCheckRequest) =>
    request<EligibilityCheckDto>('/tiss/eligibility', { method: 'POST', body: JSON.stringify(data) }),
  getEligibilityHistory: (patientId?: string) =>
    request<EligibilityCheckDto[]>(`/tiss/eligibility${patientId ? `?patientId=${patientId}` : ''}`),
  getInsuranceAuthorizations: (patientId?: string, healthInsuranceId?: string) => {
    const params = new URLSearchParams();
    if (patientId) params.set('patientId', patientId);
    if (healthInsuranceId) params.set('healthInsuranceId', healthInsuranceId);
    const qs = params.toString();
    return request<InsuranceAuthorizationDto[]>(`/tiss/authorizations${qs ? `?${qs}` : ''}`);
  },
  createInsuranceAuthorization: (data: CreateAuthorizationRequest) =>
    request<InsuranceAuthorizationDto>('/tiss/authorizations', { method: 'POST', body: JSON.stringify(data) }),
  requestOnlineAuthorization: (data: RequestOnlineAuthorizationRequest) =>
    request<InsuranceAuthorizationDto>('/tiss/authorizations/request-online', { method: 'POST', body: JSON.stringify(data) }),
  updateInsuranceAuthorization: (id: string, data: UpdateAuthorizationRequest) =>
    request<InsuranceAuthorizationDto>(`/tiss/authorizations/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getTissBatches: () => request<TissBatchDto[]>('/tiss/batches'),
  getTissBatch: (id: string) => request<TissBatchDetailDto>(`/tiss/batches/${id}`),
  createTissBatch: (data: CreateTissBatchRequest) =>
    request<TissBatchDetailDto>('/tiss/batches', { method: 'POST', body: JSON.stringify(data) }),
  sendTissBatch: (id: string) => request<TissBatchDetailDto>(`/tiss/batches/${id}/send`, { method: 'POST' }),
  validateTissBatch: (id: string) =>
    request<TissXmlValidationResultDto>(`/tiss/batches/${id}/validate`, { method: 'POST' }),
  validateTissXml: (xmlContent: string) =>
    request<TissXmlValidationResultDto>('/tiss/validate-xml', { method: 'POST', body: JSON.stringify({ xmlContent }) }),
  downloadTissBatchXml: async (id: string, fallbackName: string) => {
    const response = await fetch(`${API_BASE}/tiss/batches/${id}/xml`, { headers: authHeaders() });
    if (response.status === 401) {
      handleUnauthorized();
      throw new Error('Sessão expirada');
    }
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Erro ao baixar XML' }));
      throw new Error(error.message ?? 'Falha no download do XML');
    }
    const blob = await response.blob();
    const disposition = response.headers.get('Content-Disposition');
    const filename = disposition?.match(/filename="?([^";]+)"?/)?.[1] ?? `${fallbackName}.xml`;
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
  },
  getTissConvenioDashboard: () => request<TissConvenioDashboardDto>('/tiss/dashboard'),
  getTussCatalog: (search?: string, tableType?: number, page = 1, pageSize = 50) => {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    if (tableType) params.set('tableType', String(tableType));
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    return request<PagedResult<TussCatalogDto>>(`/tiss/tuss-catalog?${params.toString()}`);
  },
  importTussCsv: (csvContent: string) =>
    request<ImportTussResultDto>('/tiss/tuss-catalog/import-csv', { method: 'POST', body: JSON.stringify({ csvContent }) }),
  importBundledTuss202601: () =>
    request<ImportTussResultDto>('/tiss/tuss-catalog/import-bundled-202601', { method: 'POST' }),
  importTussXlsx: async (file: File) => {
    const form = new FormData();
    form.append('file', file);
    const token = localStorage.getItem('hospital_token');
    const response = await fetch(`${API_BASE}/tiss/tuss-catalog/import-xlsx`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    });
    if (response.status === 401) {
      handleUnauthorized();
      throw new Error('Sessão expirada');
    }
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Erro na importação XLSX' }));
      throw new Error(error.message ?? 'Falha ao importar XLSX TUSS');
    }
    return response.json() as Promise<ImportTussResultDto>;
  },
  seedExpandedTussCatalog: () =>
    request<ImportTussResultDto>('/tiss/tuss-catalog/seed-expanded', { method: 'POST' }),
  getTussSampleCsv: () => request<{ csv: string }>('/tiss/tuss-catalog/sample-csv'),
  getOperatorProfiles: () => request<OperatorProfileDto[]>('/tiss/operator-profiles'),
  getSigtapProcedures: (search?: string, page = 1, pageSize = 50) => {
    const params = new URLSearchParams();
    if (search) params.set('search', search);
    params.set('page', String(page));
    params.set('pageSize', String(pageSize));
    return request<PagedResult<SigtapProcedureDto>>(`/tiss/sigtap?${params.toString()}`);
  },
  getSigtapSummary: () => request<SigtapCatalogSummaryDto>('/tiss/sigtap/summary'),
  syncSigtapOfficial: () =>
    request<SyncSigtapOfficialResultDto>('/tiss/sigtap/sync-official', { method: 'POST' }),
  importSigtapZip: async (file: File) => {
    const form = new FormData();
    form.append('file', file);
    const token = localStorage.getItem('hospital_token');
    const response = await fetch(`${API_BASE}/tiss/sigtap/import-zip`, {
      method: 'POST',
      headers: token ? { Authorization: `Bearer ${token}` } : {},
      body: form,
    });
    if (response.status === 401) {
      handleUnauthorized();
      throw new Error('Sessão expirada');
    }
    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Erro na importação SIGTAP' }));
      throw new Error(error.message ?? 'Falha ao importar SIGTAP');
    }
    return response.json() as Promise<ImportSigtapResultDto>;
  },
  getTissDemonstrativos: () => request<TissDemonstrativoDto[]>('/tiss/demonstrativos'),
  getTissDemonstrativo: (id: string) => request<TissDemonstrativoDetailDto>(`/tiss/demonstrativos/${id}`),
  importTissDemonstrativo: (data: ImportDemonstrativoRequest) =>
    request<TissDemonstrativoDetailDto>('/tiss/demonstrativos/import', { method: 'POST', body: JSON.stringify(data) }),
  processTissDemonstrativo: (id: string) =>
    request<TissDemonstrativoDetailDto>(`/tiss/demonstrativos/${id}/process`, { method: 'POST' }),
  fetchOperatorDemonstrativo: (data: FetchOperatorDemonstrativoRequest) =>
    request<TissDemonstrativoDetailDto>('/tiss/demonstrativos/fetch-operator', { method: 'POST', body: JSON.stringify(data) }),
  getTissGuideAnnexes: (guideId: string) => request<TissGuideAnnexDto[]>(`/tiss/guides/${guideId}/annexes`),
  createTissGuideAnnex: (data: CreateTissGuideAnnexRequest) =>
    request<TissGuideAnnexDto>('/tiss/annexes', { method: 'POST', body: JSON.stringify(data) }),
  getInsuranceIntegrations: () => request<HealthInsuranceIntegrationDto[]>('/tiss/integrations'),
  updateInsuranceIntegration: (id: string, data: UpdateHealthInsuranceIntegrationRequest) =>
    request<HealthInsuranceIntegrationDto>(`/tiss/integrations/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  getOperatorTransactionLogs: (limit = 50) => request<OperatorTransactionLogDto[]>(`/tiss/operator-logs?limit=${limit}`),
  getTissReconciliation: () => request<TissReconciliationSummaryDto>('/tiss/reconciliation'),
  analyzeTriage: (data: TriageRequestDto) =>
    request<TriageResponseDto>('/ai/triage', { method: 'POST', body: JSON.stringify(data) }),
  suggestCid10: (data: Cid10SuggestionRequestDto) =>
    request<Cid10SuggestionDto[]>('/ai/cid10/suggest', { method: 'POST', body: JSON.stringify(data) }),
  getTriageLogs: (limit = 20) => request<AiTriageLogDto[]>(`/ai/triage/logs?limit=${limit}`),
  getTriageAdmissionSuggestion: async (patientId: string) => {
    const result = await request<TriageAdmissionSuggestionDto | undefined>(
      `/ai/triage/patient/${patientId}/admission-suggestion`,
    );
    return result ?? null;
  },
  analyzeOutbreak: (days = 30) =>
    request<AiInsightReportDto>(`/ai/insights/outbreak?days=${days}`),
  analyzeRecurrentPatient: (patientId: string) =>
    request<AiInsightReportDto>(`/ai/insights/patient/${patientId}/recurrent`),
  analyzeTriageOperational: (days = 7) =>
    request<AiInsightReportDto>(`/ai/insights/triage-operational?days=${days}`),
  getAiInsightReports: (limit = 20, type?: string) => {
    const qs = new URLSearchParams({ limit: String(limit) });
    if (type) qs.set('type', type);
    return request<AiInsightReportDto[]>(`/ai/insights/reports?${qs}`);
  },
  getAiInsightReport: (id: string) => request<AiInsightReportDto>(`/ai/insights/reports/${id}`),
  getGroqStatus: () => request<GroqStatusDto>('/ai/groq/status'),
  getIntegrationMessages: (limit = 30) => request<IntegrationMessageDto[]>(`/integrations/messages?limit=${limit}`),
  processHl7Inbound: (data: Hl7InboundRequestDto) =>
    request<IntegrationProcessResultDto>('/integrations/hl7/inbound', { method: 'POST', body: JSON.stringify(data) }),
  exportFhirPatient: (patientId: string) => request<FhirPatientExportDto>(`/integrations/fhir/Patient/${patientId}`),
  importFhirPatient: (json: string) =>
    request<IntegrationProcessResultDto>('/integrations/fhir/import', { method: 'POST', body: JSON.stringify({ json }) }),
  getPatientPortalDashboard: (patientId?: string) =>
    request<PatientPortalDashboardDto>(
      `/patient-portal/dashboard${patientId ? `?patientId=${patientId}` : ''}`,
    ),
  getPatientPortalMedicalRecord: (patientId?: string) =>
    request<PatientMedicalRecordDto>(
      `/patient-portal/medical-record${patientId ? `?patientId=${patientId}` : ''}`,
    ),
  getEmergencyVisits: (status?: string) =>
    request<EmergencyVisitDto[]>(`/emergency/visits${status ? `?status=${status}` : ''}`),
  createEmergencyVisit: (data: CreateEmergencyVisitRequest) =>
    request<EmergencyVisitDto>('/emergency/visits', { method: 'POST', body: JSON.stringify(data) }),
  updateEmergencyVisitStatus: (id: string, data: UpdateEmergencyVisitStatusRequest) =>
    request<EmergencyVisitDto>(`/emergency/visits/${id}/status`, { method: 'PATCH', body: JSON.stringify(data) }),
  getSuppliers: () => request<SupplierDto[]>('/purchasing/suppliers'),
  createSupplier: (data: CreateSupplierRequest) =>
    request<SupplierDto>('/purchasing/suppliers', { method: 'POST', body: JSON.stringify(data) }),
  getPurchaseOrders: (status?: string) =>
    request<PurchaseOrderDto[]>(`/purchasing/orders${status ? `?status=${status}` : ''}`),
  getPurchaseSuggestions: (sector?: number, priority?: number) =>
    request<PurchaseCreateSuggestionsDto>(
      `/purchasing/suggestions?${sector ? `sector=${sector}&` : ''}${priority ? `priority=${priority}` : ''}`,
    ),
  createPurchaseOrder: (data: CreatePurchaseOrderRequest) =>
    request<PurchaseOrderDto>('/purchasing/orders', { method: 'POST', body: JSON.stringify(data) }),
  sendPurchaseOrder: (id: string) =>
    request<PurchaseOrderDto>(`/purchasing/orders/${id}/send`, { method: 'POST' }),
  receivePurchaseOrder: (id: string, data: ReceivePurchaseOrderRequest) =>
    request<PurchaseOrderDto>(`/purchasing/orders/${id}/receive`, { method: 'POST', body: JSON.stringify(data) }),
  getAuditLogs: (limit = 50, entityType?: string) =>
    request<AuditLogDto[]>(`/audit/logs?limit=${limit}${entityType ? `&entityType=${encodeURIComponent(entityType)}` : ''}`),
  getComplianceDashboard: () => request<ComplianceDashboardDto>('/security/dashboard'),
  getLoginAttempts: (limit = 100) => request<LoginAttemptDto[]>(`/security/login-attempts?limit=${limit}`),
  getUserSessions: (activeOnly = true) => request<UserSessionDto[]>(`/security/sessions?activeOnly=${activeOnly}`),
  revokeUserSession: (id: string) => request<void>(`/security/sessions/${id}/revoke`, { method: 'POST' }),
  getConsentTerms: () => request<ConsentTermDto[]>('/security/consent-terms'),
  getCurrentConsentTerms: () => request<ConsentTermDto[]>('/security/consent-terms/current'),
  getPatientConsentStatus: (patientId: string) =>
    request<PatientConsentStatusDto>(`/security/consent-status?patientId=${patientId}`),
  getPatientConsents: (patientId?: string) =>
    request<PatientConsentDto[]>(`/security/consents${patientId ? `?patientId=${patientId}` : ''}`),
  recordPatientConsent: (data: RecordPatientConsentRequest) =>
    request<PatientConsentDto>('/security/consents', { method: 'POST', body: JSON.stringify(data) }),
  revokePatientConsent: (id: string) =>
    request<void>(`/security/consents/${id}/revoke`, { method: 'POST' }),
  getPatientPortalConsentStatus: (patientId?: string) =>
    request<PatientConsentStatusDto>(
      `/patient-portal/consent-status${patientId ? `?patientId=${patientId}` : ''}`,
    ),
  signPatientPortalConsent: (data: SignPatientConsentRequest, patientId?: string) =>
    request<PatientConsentDto>(
      `/patient-portal/consents${patientId ? `?patientId=${patientId}` : ''}`,
      { method: 'POST', body: JSON.stringify(data) },
    ),
  getPatientConsentDetail: (id: string) =>
    request<PatientConsentDetailDto>(`/security/consents/${id}`),
  getPatientPortalConsentDetail: (id: string, patientId?: string) =>
    request<PatientConsentDetailDto>(
      `/patient-portal/consents/${id}${patientId ? `?patientId=${patientId}` : ''}`,
    ),
  getDataSubjectRequests: (status?: string) =>
    request<DataSubjectRequestDto[]>(`/security/subject-requests${status ? `?status=${status}` : ''}`),
  createDataSubjectRequest: (data: CreateDataSubjectRequest) =>
    request<DataSubjectRequestDto>('/security/subject-requests', { method: 'POST', body: JSON.stringify(data) }),
  updateDataSubjectRequest: (id: string, data: UpdateDataSubjectRequestStatus) =>
    request<DataSubjectRequestDto>(`/security/subject-requests/${id}`, { method: 'PATCH', body: JSON.stringify(data) }),
  exportDataSubjectRequest: async (id: string) => {
    const response = await fetch(`${API_BASE}/security/subject-requests/${id}/export`, {
      method: 'POST',
      headers: authHeaders(),
    });

    if (response.status === 401) {
      handleUnauthorized();
      throw new Error('Sessão expirada');
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Erro ao exportar dados' }));
      throw new Error(error.message ?? 'Falha na exportação LGPD');
    }

    const blob = await response.blob();
    const disposition = response.headers.get('Content-Disposition');
    const filename = disposition?.match(/filename="?([^";]+)"?/)?.[1] ?? `lgpd-export-${id}.json`;
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    link.click();
    URL.revokeObjectURL(url);
  },
  getPrivacyIncidents: () => request<PrivacyIncidentDto[]>('/security/incidents'),
  createPrivacyIncident: (data: CreatePrivacyIncidentRequest) =>
    request<PrivacyIncidentDto>('/security/incidents', { method: 'POST', body: JSON.stringify(data) }),
  updatePrivacyIncident: (id: string, data: UpdatePrivacyIncidentRequest) =>
    request<PrivacyIncidentDto>(`/security/incidents/${id}`, { method: 'PATCH', body: JSON.stringify(data) }),
  setupMfa: () => request<MfaSetupResponse>('/auth/mfa/setup', { method: 'POST' }),
  enableMfa: (code: string) => request<void>('/auth/mfa/enable', { method: 'POST', body: JSON.stringify({ code }) }),
  disableMfa: (code: string) => request<void>('/auth/mfa/disable', { method: 'POST', body: JSON.stringify({ code }) }),
  verifyMfaLogin: (mfaToken: string, code: string) =>
    request<LoginApiResponse>('/auth/mfa/verify', { method: 'POST', body: JSON.stringify({ mfaToken, code }) }),
  logoutApi: () => request<void>('/auth/logout', { method: 'POST' }),
  getUsers: () => request<UserListDto[]>('/users'),
  getUser: (id: string) => request<UserDetailDto>(`/users/${id}`),
  createUser: (data: CreateUserRequest) =>
    request<UserDetailDto>('/users', { method: 'POST', body: JSON.stringify(data) }),
  updateUser: (id: string, data: UpdateUserRequest) =>
    request<UserDetailDto>(`/users/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  resetUserPassword: (id: string, newPassword: string) =>
    request<void>(`/users/${id}/password`, {
      method: 'PATCH',
      body: JSON.stringify({ newPassword }),
    }),
  getNotifications: (unreadOnly?: boolean) =>
    request<NotificationDto[]>(`/notifications${unreadOnly ? '?unreadOnly=true' : ''}`),
  getUnreadNotificationCount: () => request<{ count: number }>('/notifications/unread-count'),
  getNotificationHubSummary: () => request<HubSummaryDto>('/notifications/hub-summary'),
  getPendencies: (modulo?: string, status?: string) => {
    const q = new URLSearchParams();
    if (modulo) q.set('modulo', modulo);
    if (status) q.set('status', status);
    const qs = q.toString();
    return request<PendencyDto[]>(`/pendencies${qs ? `?${qs}` : ''}`);
  },
  getPendenciesSummary: () => request<PendencySummaryDto>('/pendencies/summary'),
  markNotificationRead: (id: string) =>
    request<void>(`/notifications/${id}/read`, { method: 'POST' }),
  getIcuDashboard: () => request<IcuDashboardDto>('/icu/dashboard'),
  recordVitalSigns: (data: RecordVitalSignsRequest) =>
    request<VitalSignDto>('/icu/vitals', { method: 'POST', body: JSON.stringify(data) }),
  getVitalHistory: (hospitalizationId: string, limit = 20) =>
    request<VitalSignDto[]>(`/icu/vitals/${hospitalizationId}?limit=${limit}`),
  getAmbulanceFleet: () => request<AmbulanceDto[]>('/ambulance/fleet'),
  getAmbulanceDispatches: (status?: string) =>
    request<AmbulanceDispatchDto[]>(`/ambulance/dispatches${status ? `?status=${status}` : ''}`),
  createAmbulanceDispatch: (data: CreateAmbulanceDispatchRequest) =>
    request<AmbulanceDispatchDto>('/ambulance/dispatches', { method: 'POST', body: JSON.stringify(data) }),
  assignAmbulance: (dispatchId: string, ambulanceId: string) =>
    request<AmbulanceDispatchDto>(`/ambulance/dispatches/${dispatchId}/assign`, {
      method: 'POST',
      body: JSON.stringify({ ambulanceId }),
    }),
  updateAmbulanceDispatchStatus: (dispatchId: string, status: string) =>
    request<AmbulanceDispatchDto>(`/ambulance/dispatches/${dispatchId}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getTransportDashboard: () => request<TransportDashboardDto>('/transport/dashboard'),
  getTransportMetrics: () => request<TransportMetricsDto>('/transport/metrics'),
  getTransportAssets: () => request<TransportAssetDto[]>('/transport/assets'),
  createTransportAsset: (data: CreateTransportAssetRequest) =>
    request<TransportAssetDto>('/transport/assets', { method: 'POST', body: JSON.stringify(data) }),
  updateTransportAssetStatus: (id: string, status: string) =>
    request<TransportAssetDto>(`/transport/assets/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getTransportRequests: (status?: string) =>
    request<TransportRequestDto[]>(`/transport/requests${status ? `?status=${status}` : ''}`),
  getTransportPorters: () => request<TransportPorterDto[]>('/transport/porters'),
  createTransportRequest: (data: CreateTransportRequestRequest) =>
    request<TransportRequestDto>('/transport/requests', { method: 'POST', body: JSON.stringify(data) }),
  acceptTransportRequest: (id: string, data: AcceptTransportRequestRequest) =>
    request<TransportRequestDto>(`/transport/requests/${id}/accept`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  advanceTransportRequest: (id: string, status: string) =>
    request<TransportRequestDto>(`/transport/requests/${id}/advance`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  cancelTransportRequest: (id: string) =>
    request<TransportRequestDto>(`/transport/requests/${id}/cancel`, { method: 'POST' }),
  getHotelariaNoc: () => request<HotelariaNocDto>('/hotelaria/noc'),
  getCleaningRequests: (status?: string) =>
    request<CleaningRequestDto[]>(`/hotelaria/cleaning${status ? `?status=${status}` : ''}`),
  createCleaningRequest: (data: CreateCleaningRequestRequest) =>
    request<CleaningRequestDto>('/hotelaria/cleaning', { method: 'POST', body: JSON.stringify(data) }),
  startCleaningRequest: (id: string, data: StartCleaningRequestRequest) =>
    request<CleaningRequestDto>(`/hotelaria/cleaning/${id}/start`, { method: 'POST', body: JSON.stringify(data) }),
  updateCleaningChecklist: (id: string, checklist: CleaningChecklistItemDto[]) =>
    request<CleaningRequestDto>(`/hotelaria/cleaning/${id}/checklist`, {
      method: 'PATCH',
      body: JSON.stringify({ checklist }),
    }),
  completeCleaningRequest: (id: string, notes?: string) =>
    request<CleaningRequestDto>(`/hotelaria/cleaning/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    }),
  cancelCleaningRequest: (id: string) =>
    request<CleaningRequestDto>(`/hotelaria/cleaning/${id}/cancel`, { method: 'POST' }),
  syncPush: (data: SyncPushRequest) =>
    request<SyncPushResponse>('/sync/push', { method: 'POST', body: JSON.stringify(data) }),
  syncPull: (data?: SyncPullRequest) =>
    request<SyncPullResponse>('/sync/pull', {
      method: 'POST',
      body: JSON.stringify(data ?? {}),
    }),
  getParkingZones: () => request<ParkingZoneDto[]>('/parking/zones'),
  getParkingSessions: (activeOnly?: boolean) =>
    request<ParkingSessionDto[]>(`/parking/sessions${activeOnly ? '?activeOnly=true' : ''}`),
  checkInParking: (data: CheckInParkingRequest) =>
    request<ParkingSessionDto>('/parking/check-in', { method: 'POST', body: JSON.stringify(data) }),
  payParking: (sessionId: string) =>
    request<ParkingSessionDto>('/parking/pay', { method: 'POST', body: JSON.stringify({ sessionId }) }),
  checkOutParking: (sessionId: string) =>
    request<ParkingSessionDto>('/parking/check-out', { method: 'POST', body: JSON.stringify({ sessionId }) }),
  processParkingGateExit: (qrPayload: string) =>
    request<ParkingGateExitResultDto>('/parking/gate/exit', { method: 'POST', body: JSON.stringify({ qrPayload }) }),
  getDietOrders: (status?: string, mealDate?: string) => {
    const params = new URLSearchParams();
    if (status) params.set('status', status);
    if (mealDate) params.set('mealDate', mealDate);
    const qs = params.toString();
    return request<DietOrderDto[]>(`/nutrition/orders${qs ? `?${qs}` : ''}`);
  },
  createDietOrder: (data: CreateDietOrderRequest) =>
    request<DietOrderDto>('/nutrition/orders', { method: 'POST', body: JSON.stringify(data) }),
  updateDietOrderStatus: (id: string, status: string) =>
    request<DietOrderDto>(`/nutrition/orders/${id}/status`, { method: 'PATCH', body: JSON.stringify({ status }) }),
  getSpecialties: () => request<SpecialtyDto[]>('/catalog/specialties'),
  getConsultingRooms: () => request<ConsultingRoomDto[]>('/consulting-rooms'),
  createConsultingRoom: (data: CreateConsultingRoomRequest) =>
    request<ConsultingRoomDto>('/consulting-rooms', { method: 'POST', body: JSON.stringify(data) }),
  getRoomSchedules: (roomId?: string) =>
    request<ConsultingRoomScheduleDto[]>(`/consulting-rooms/schedules${roomId ? `?roomId=${roomId}` : ''}`),
  createRoomSchedule: (data: CreateRoomScheduleRequest) =>
    request<ConsultingRoomScheduleDto>('/consulting-rooms/schedules', { method: 'POST', body: JSON.stringify(data) }),
  getHospitalityRooms: () => request<HospitalityRoomDto[]>('/hospitality/rooms'),
  getHospitalityBookings: () => request<HospitalityBookingDto[]>('/hospitality/bookings'),
  createHospitalityBooking: (data: CreateHospitalityBookingRequest) =>
    request<HospitalityBookingDto>('/hospitality/bookings', { method: 'POST', body: JSON.stringify(data) }),
  checkInHospitality: (id: string) =>
    request<HospitalityBookingDto>(`/hospitality/bookings/${id}/check-in`, { method: 'POST' }),
  checkOutHospitality: (id: string) =>
    request<HospitalityBookingDto>(`/hospitality/bookings/${id}/check-out`, { method: 'POST' }),
  getMedicalEquipment: () => request<MedicalEquipmentDto[]>('/clinical-engineering/equipment'),
  createMedicalEquipment: (data: CreateMedicalEquipmentRequest) =>
    request<MedicalEquipmentDto>('/clinical-engineering/equipment', { method: 'POST', body: JSON.stringify(data) }),
  getMaintenanceWorkOrders: () => request<MaintenanceWorkOrderDto[]>('/clinical-engineering/work-orders'),
  createMaintenanceWorkOrder: (data: CreateWorkOrderRequest) =>
    request<MaintenanceWorkOrderDto>('/clinical-engineering/work-orders', { method: 'POST', body: JSON.stringify(data) }),
  updateWorkOrderStatus: (id: string, status: string) =>
    request<MaintenanceWorkOrderDto>(`/clinical-engineering/work-orders/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getSecuritySettings: () => request<SecuritySettingsDto>('/physical-security/settings'),
  getSecurityDashboard: () => request<SecurityDashboardDto>('/physical-security/dashboard'),
  getSecurityIncidents: () => request<SecurityIncidentDto[]>('/physical-security/incidents'),
  createSecurityIncident: (data: CreateSecurityIncidentRequest) =>
    request<SecurityIncidentDto>('/physical-security/incidents', { method: 'POST', body: JSON.stringify(data) }),
  resolveSecurityIncident: (id: string, resolutionNotes: string) =>
    request<SecurityIncidentDto>(`/physical-security/incidents/${id}/resolve`, {
      method: 'POST',
      body: JSON.stringify({ resolutionNotes }),
    }),
  getVisitorLogs: (insideOnly?: boolean) =>
    request<VisitorLogDto[]>(`/physical-security/visitors${insideOnly ? '?insideOnly=true' : ''}`),
  registerVisitor: (data: RegisterVisitorRequest) =>
    request<VisitorLogDto>('/physical-security/visitors', { method: 'POST', body: JSON.stringify(data) }),
  registerVisitorExit: (id: string) =>
    request<VisitorLogDto>(`/physical-security/visitors/${id}/exit`, { method: 'POST' }),
  getPhysicalAccessDashboard: () => request<PhysicalAccessDashboardDto>('/physical-access/dashboard'),
  getAccessZones: () => request<AccessZoneDto[]>('/physical-access/zones'),
  getAccessTurnstiles: () => request<AccessTurnstileDto[]>('/physical-access/turnstiles'),
  getAccessRecords: (limit?: number) =>
    request<AccessControlRecordDto[]>(`/physical-access/records${limit ? `?limit=${limit}` : ''}`),
  getAccessCredentials: () => request<AccessCredentialDto[]>('/physical-access/credentials'),
  getFacialEnrollments: () => request<FacialBiometricDto[]>('/physical-access/facial'),
  getRegisteredVehicles: () => request<RegisteredVehicleDto[]>('/physical-access/vehicles'),
  getLprEvents: () => request<LprReadEventDto[]>('/physical-access/lpr'),
  getKioskTickets: (pendingOnly?: boolean) =>
    request<KioskTicketDto[]>(`/physical-access/kiosk/tickets${pendingOnly ? '?pendingOnly=true' : ''}`),
  getAccessIntegrations: () => request<AccessIntegrationProfileDto[]>('/physical-access/integrations'),
  getEmployeeSectorAccess: () => request<EmployeeSectorAccessDto[]>('/physical-access/employees'),
  getAppointmentQr: (appointmentId: string) =>
    request<AppointmentQrDto>(`/physical-access/appointments/${appointmentId}/qr`),
  validateTurnstile: (data: TurnstileValidationRequest) =>
    request<TurnstileValidationResultDto>('/physical-access/turnstile/validate', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  issueCompanionCredential: (data: IssueCompanionCredentialRequest) =>
    request<AccessCredentialDto>('/physical-access/companions/credential', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  enrollFacial: (data: EnrollFacialRequest) =>
    request<FacialBiometricDto>('/physical-access/facial/enroll', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  validateFacialAccess: (data: FacialValidationRequest) =>
    request<TurnstileValidationResultDto>('/physical-access/facial/validate', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  kioskCheckIn: (data: KioskCheckInRequest) =>
    request<KioskCheckInResultDto>('/physical-access/kiosk/check-in', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  issueKioskTicket: (data: IssueKioskTicketRequest) =>
    request<KioskTicketDto>('/physical-access/kiosk/tickets', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  registerAccessVehicle: (data: RegisterVehicleRequest) =>
    request<RegisteredVehicleDto>('/physical-access/vehicles', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  processLprRead: (data: LprReadRequest) =>
    request<LprReadResultDto>('/physical-access/lpr/read', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  getInstrumentKits: () => request<InstrumentKitDto[]>('/cme/kits'),
  createInstrumentKit: (data: CreateInstrumentKitRequest) =>
    request<InstrumentKitDto>('/cme/kits', { method: 'POST', body: JSON.stringify(data) }),
  getSterilizationCycles: () => request<SterilizationCycleDto[]>('/cme/cycles'),
  createSterilizationCycle: (data: CreateSterilizationCycleRequest) =>
    request<SterilizationCycleDto>('/cme/cycles', { method: 'POST', body: JSON.stringify(data) }),
  startSterilizationCycle: (id: string) =>
    request<SterilizationCycleDto>(`/cme/cycles/${id}/start`, { method: 'POST' }),
  completeSterilizationCycle: (id: string, expirationDate: string) =>
    request<SterilizationCycleDto>(`/cme/cycles/${id}/complete`, {
      method: 'POST',
      body: JSON.stringify({ expirationDate }),
    }),
  rejectSterilizationCycle: (id: string, reason?: string) =>
    request<SterilizationCycleDto>(`/cme/cycles/${id}/reject`, {
      method: 'POST',
      body: JSON.stringify({ reason }),
    }),
  getRecentHospitalEvents: (limit = 20) =>
    request<HospitalEventLogDto[]>(`/events/recent?limit=${limit}`),
  getMyMissions: () => request<UserMissionsDto>('/tasks/my-missions'),
  completeMission: (id: string) =>
    request<void>(`/tasks/${id}/complete`, { method: 'POST' }),
  getWasteDashboard: () => request<WasteDashboardDto>('/waste/dashboard'),
  getWasteCollections: (params?: { wasteType?: string; status?: string; sector?: string }) => {
    const q = new URLSearchParams();
    if (params?.wasteType) q.set('wasteType', params.wasteType);
    if (params?.status) q.set('status', params.status);
    if (params?.sector) q.set('sector', params.sector);
    const suffix = q.toString() ? `?${q}` : '';
    return request<WasteCollectionDto[]>(`/waste${suffix}`);
  },
  createWasteCollection: (data: CreateWasteCollectionRequest) =>
    request<WasteCollectionDto>('/waste', { method: 'POST', body: JSON.stringify(data) }),
  updateWasteCollection: (id: string, data: UpdateWasteCollectionRequest) =>
    request<WasteCollectionDto>(`/waste/${id}`, { method: 'PATCH', body: JSON.stringify(data) }),
  getBloodUnits: (status?: string) =>
    request<BloodUnitDto[]>(`/hemotherapy/units${status ? `?status=${status}` : ''}`),
  createBloodUnit: (data: CreateBloodUnitRequest) =>
    request<BloodUnitDto>('/hemotherapy/units', { method: 'POST', body: JSON.stringify(data) }),
  getTransfusionRequests: () => request<TransfusionRequestDto[]>('/hemotherapy/transfusions'),
  createTransfusionRequest: (data: CreateTransfusionRequestRequest) =>
    request<TransfusionRequestDto>('/hemotherapy/transfusions', { method: 'POST', body: JSON.stringify(data) }),
  matchTransfusion: (id: string, bloodUnitId: string) =>
    request<TransfusionRequestDto>(`/hemotherapy/transfusions/${id}/match`, {
      method: 'POST',
      body: JSON.stringify({ bloodUnitId }),
    }),
  completeTransfusion: (id: string) =>
    request<TransfusionRequestDto>(`/hemotherapy/transfusions/${id}/complete`, { method: 'POST' }),
  getDialysisSessions: () => request<DialysisSessionDto[]>('/dialysis/sessions'),
  createDialysisSession: (data: CreateDialysisSessionRequest) =>
    request<DialysisSessionDto>('/dialysis/sessions', { method: 'POST', body: JSON.stringify(data) }),
  updateDialysisSessionStatus: (id: string, status: string) =>
    request<DialysisSessionDto>(`/dialysis/sessions/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getLaundryBatches: () => request<LaundryBatchDto[]>('/laundry/batches'),
  createLaundryBatch: (data: CreateLaundryBatchRequest) =>
    request<LaundryBatchDto>('/laundry/batches', { method: 'POST', body: JSON.stringify(data) }),
  updateLaundryBatchStatus: (id: string, status: string) =>
    request<LaundryBatchDto>(`/laundry/batches/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getChemotherapySessions: () => request<ChemotherapySessionDto[]>('/oncology/sessions'),
  createChemotherapySession: (data: CreateChemotherapySessionRequest) =>
    request<ChemotherapySessionDto>('/oncology/sessions', { method: 'POST', body: JSON.stringify(data) }),
  updateChemotherapyStatus: (id: string, status: string) =>
    request<ChemotherapySessionDto>(`/oncology/sessions/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getPhysiotherapySessions: () => request<PhysiotherapySessionDto[]>('/physiotherapy/sessions'),
  createPhysiotherapySession: (data: CreatePhysiotherapySessionRequest) =>
    request<PhysiotherapySessionDto>('/physiotherapy/sessions', { method: 'POST', body: JSON.stringify(data) }),
  updatePhysiotherapyStatus: (id: string, status: string) =>
    request<PhysiotherapySessionDto>(`/physiotherapy/sessions/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getTelemedicineAppointments: () => request<TelemedicineAppointmentDto[]>('/telemedicine/appointments'),
  createTelemedicineAppointment: (data: CreateTelemedicineAppointmentRequest) =>
    request<TelemedicineAppointmentDto>('/telemedicine/appointments', { method: 'POST', body: JSON.stringify(data) }),
  updateTelemedicineStatus: (id: string, status: string) =>
    request<TelemedicineAppointmentDto>(`/telemedicine/appointments/${id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
  getInfectionControlDashboard: () => request<InfectionControlDashboardDto>('/infection-control/dashboard'),
  getInfectionSurveillance: () => request<InfectionSurveillanceDto[]>('/infection-control/surveillance'),
  createInfectionSurveillance: (data: CreateInfectionSurveillanceRequest) =>
    request<InfectionSurveillanceDto>('/infection-control/surveillance', { method: 'POST', body: JSON.stringify(data) }),
  resolveInfectionSurveillance: (id: string, notes?: string) =>
    request<InfectionSurveillanceDto>(`/infection-control/surveillance/${id}/resolve`, {
      method: 'POST',
      body: JSON.stringify({ notes }),
    }),
  getIsolationPrecautions: (activeOnly?: boolean) =>
    request<IsolationPrecautionDto[]>(`/infection-control/isolations${activeOnly ? '?activeOnly=true' : ''}`),
  createIsolationPrecaution: (data: CreateIsolationPrecautionRequest) =>
    request<IsolationPrecautionDto>('/infection-control/isolations', { method: 'POST', body: JSON.stringify(data) }),
  liftIsolationPrecaution: (id: string) =>
    request<IsolationPrecautionDto>(`/infection-control/isolations/${id}/lift`, { method: 'POST' }),
  getConnectDashboard: () => request<ConnectDashboardDto>('/connect/dashboard'),
  getConnectConversations: (params: ConnectConversationQuery = {}) => {
    const q = new URLSearchParams();
    q.set('limit', String(params.limit ?? 50));
    if (params.botStep) q.set('botStep', params.botStep);
    if (params.queue) q.set('queue', params.queue);
    if (params.awaitingHumanOnly) q.set('awaitingHumanOnly', 'true');
    return request<ConnectConversationDto[]>(`/connect/conversations?${q.toString()}`);
  },
  getConnectConversation: (id: string) =>
    request<ConnectConversationDetailDto>(`/connect/conversations/${id}`),
  replyConnectConversation: (id: string, body: string) =>
    request<ConnectMessageDto>(`/connect/conversations/${id}/reply`, {
      method: 'POST',
      body: JSON.stringify({ body }),
    }),
  assignConnectConversation: (id: string, data: ConnectAssignRequest) =>
    request<ConnectConversationDto>(`/connect/conversations/${id}/assign`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  resolveConnectConversation: (id: string) =>
    request<ConnectConversationDto>(`/connect/conversations/${id}/resolve`, { method: 'POST' }),
  getConnectInboxSummary: () => request<ConnectInboxSummaryDto>('/connect/inbox/summary'),
  getConnectWaitlist: () => request<ConnectWaitlistDto[]>('/connect/waitlist'),
  getConnectKnowledge: () => request<ConnectKnowledgeArticleDto[]>('/connect/knowledge'),
  getConnectSatisfaction: () => request<ConnectSatisfactionStatsDto>('/connect/satisfaction'),
  simulateConnectInbound: (data: SimulateInboundRequest) =>
    request<SimulateInboundResponse>('/connect/simulate', { method: 'POST', body: JSON.stringify(data) }),
  blockConnectSchedule: (data: BlockProfessionalScheduleRequest) =>
    request<BlockProfessionalScheduleResult>('/connect/block-schedule', { method: 'POST', body: JSON.stringify(data) }),
  getConnectIntegrationStatus: () =>
    request<ConnectIntegrationStatusDto>('/connect/integration-status'),

  getIntegrationReadiness: () =>
    request<IntegrationReadinessDto>('/integrations/readiness'),
  testWhatsAppIntegration: () =>
    request<IntegrationTestResultDto>('/integrations/test/whatsapp', { method: 'POST' }),
  testPixIntegration: () =>
    request<IntegrationTestResultDto>('/integrations/test/pix', { method: 'POST' }),
  testTissIntegration: (operatorId?: string) => {
    const q = operatorId ? `?operatorId=${encodeURIComponent(operatorId)}` : '';
    return request<IntegrationTestResultDto>(`/integrations/test/tiss${q}`, { method: 'POST' });
  },

  getConnectCommSummary: () => request<ConnectCommSummaryDto>('/connect/comm/summary'),
  getConnectMail: (folder: MailFolder = 'Inbox', search?: string) => {
    const q = new URLSearchParams();
    q.set('folder', folder);
    if (search) q.set('search', search);
    return request<MailListItemDto[]>(`/connect/mail?${q.toString()}`);
  },
  getConnectMailDetail: (id: string) => request<MailDetailDto>(`/connect/mail/${id}`),
  createConnectMail: (data: CreateMailRequest) =>
    request<MailDetailDto>('/connect/mail', { method: 'POST', body: JSON.stringify(data) }),
  updateConnectMailDraft: (id: string, data: UpdateMailRequest) =>
    request<MailDetailDto>(`/connect/mail/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  sendConnectMailDraft: (id: string) =>
    request<MailDetailDto>(`/connect/mail/${id}/send`, { method: 'POST' }),
  markConnectMailRead: (id: string) =>
    request<void>(`/connect/mail/${id}/read`, { method: 'POST' }),
  archiveConnectMail: (id: string) =>
    request<void>(`/connect/mail/${id}/archive`, { method: 'POST' }),
  trashConnectMail: (id: string) =>
    request<void>(`/connect/mail/${id}/trash`, { method: 'POST' }),
  downloadConnectMailAttachment: (messageId: string, attachmentId: string, fileName: string) =>
    downloadFile(`/connect/mail/${messageId}/attachments/${attachmentId}`, fileName),
  getConnectChatRooms: () => request<ChatRoomDto[]>('/connect/chat/rooms'),
  createConnectChatRoom: (data: CreateChatRoomRequest) =>
    request<ChatRoomDto>('/connect/chat/rooms', { method: 'POST', body: JSON.stringify(data) }),
  getConnectChatMessages: (roomId: string, limit = 50) =>
    request<ChatMessageDto[]>(`/connect/chat/rooms/${roomId}/messages?limit=${limit}`),
  sendConnectChatMessage: (roomId: string, data: SendChatMessageRequest) =>
    request<ChatMessageDto>(`/connect/chat/rooms/${roomId}/messages`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  markConnectChatRead: (roomId: string) =>
    request<void>(`/connect/chat/rooms/${roomId}/read`, { method: 'POST' }),
  getConnectNotifications: (unreadOnly?: boolean) =>
    request<ConnectNotificationDto[]>(
      `/connect/connect-notifications${unreadOnly ? '?unreadOnly=true' : ''}`,
    ),
  getConnectNotificationUnreadCount: () =>
    request<{ count: number }>('/connect/connect-notifications/unread-count'),
  markConnectNotificationRead: (id: string) =>
    request<void>(`/connect/connect-notifications/${id}/read`, { method: 'POST' }),
  getConnectBulletinPosts: () => request<BulletinPostDto[]>('/connect/bulletin'),
  getConnectBulletinPost: (id: string) => request<BulletinPostDto>(`/connect/bulletin/${id}`),
  createConnectBulletinPost: (data: CreateBulletinPostRequest) =>
    request<BulletinPostDto>('/connect/bulletin', { method: 'POST', body: JSON.stringify(data) }),
  updateConnectBulletinPost: (id: string, data: UpdateBulletinPostRequest) =>
    request<BulletinPostDto>(`/connect/bulletin/${id}`, { method: 'PUT', body: JSON.stringify(data) }),
  deleteConnectBulletinPost: (id: string) =>
    request<void>(`/connect/bulletin/${id}`, { method: 'DELETE' }),
  markConnectBulletinViewed: (id: string) =>
    request<void>(`/connect/bulletin/${id}/view`, { method: 'POST' }),

  getConnectTicketSummary: () => request<ConnectTicketSummaryDto>('/connect/tickets/summary'),
  getConnectTickets: (params?: {
    status?: ConnectTicketStatus;
    category?: ConnectTicketCategory;
    priority?: MessagePriority;
    assignedToMe?: boolean;
    myRequests?: boolean;
    search?: string;
  }) => {
    const q = new URLSearchParams();
    if (params?.status) q.set('status', params.status);
    if (params?.category) q.set('category', params.category);
    if (params?.priority) q.set('priority', params.priority);
    if (params?.assignedToMe) q.set('assignedToMe', 'true');
    if (params?.myRequests) q.set('myRequests', 'true');
    if (params?.search) q.set('search', params.search);
    const qs = q.toString();
    return request<ConnectTicketListItemDto[]>(`/connect/tickets${qs ? `?${qs}` : ''}`);
  },
  getConnectTicketDetail: (id: string) => request<ConnectTicketDetailDto>(`/connect/tickets/${id}`),
  createConnectTicket: (data: CreateConnectTicketRequest) =>
    request<ConnectTicketDetailDto>('/connect/tickets', { method: 'POST', body: JSON.stringify(data) }),
  assignConnectTicket: (id: string, responsavelId: string) =>
    request<ConnectTicketDetailDto>(`/connect/tickets/${id}/assign`, {
      method: 'POST',
      body: JSON.stringify({ responsavelId }),
    }),
  changeConnectTicketStatus: (id: string, status: ConnectTicketStatus) =>
    request<ConnectTicketDetailDto>(`/connect/tickets/${id}/status`, {
      method: 'POST',
      body: JSON.stringify({ status }),
    }),
  addConnectTicketComment: (id: string, content: string) =>
    request<ConnectTicketCommentDto>(`/connect/tickets/${id}/comments`, {
      method: 'POST',
      body: JSON.stringify({ content }),
    }),

  getConnectTaskSummary: () => request<ConnectTaskSummaryDto>('/connect/tasks/summary'),
  getConnectTasks: (params?: { scope?: string; status?: ConnectTaskStatus }) => {
    const q = new URLSearchParams();
    if (params?.scope) q.set('scope', params.scope);
    if (params?.status) q.set('status', params.status);
    const qs = q.toString();
    return request<ConnectTaskListItemDto[]>(`/connect/tasks${qs ? `?${qs}` : ''}`);
  },
  getConnectTaskDetail: (id: string) => request<ConnectTaskDetailDto>(`/connect/tasks/${id}`),
  createConnectTask: (data: CreateConnectTaskRequest) =>
    request<ConnectTaskDetailDto>('/connect/tasks', { method: 'POST', body: JSON.stringify(data) }),
  changeConnectTaskStatus: (id: string, status: ConnectTaskStatus) =>
    request<ConnectTaskDetailDto>(`/connect/tasks/${id}/status`, {
      method: 'POST',
      body: JSON.stringify({ status }),
    }),

  getConnectApprovalSummary: () => request<WorkflowSummaryDto>('/connect/approvals/summary'),
  getConnectApprovals: (params?: { pendingForMe?: boolean }) => {
    const q = new URLSearchParams();
    if (params?.pendingForMe) q.set('pendingForMe', 'true');
    const qs = q.toString();
    return request<WorkflowInstanceListItemDto[]>(`/connect/approvals${qs ? `?${qs}` : ''}`);
  },
  getConnectApprovalDetail: (id: string) => request<WorkflowInstanceDetailDto>(`/connect/approvals/${id}`),
  createConnectApproval: (data: CreateWorkflowInstanceRequest) =>
    request<WorkflowInstanceDetailDto>('/connect/approvals', { method: 'POST', body: JSON.stringify(data) }),
  approveConnectApproval: (id: string, justificativa?: string) =>
    request<WorkflowInstanceDetailDto>(`/connect/approvals/${id}/approve`, {
      method: 'POST',
      body: JSON.stringify({ justificativa }),
    }),
  rejectConnectApproval: (id: string, justificativa?: string) =>
    request<WorkflowInstanceDetailDto>(`/connect/approvals/${id}/reject`, {
      method: 'POST',
      body: JSON.stringify({ justificativa }),
    }),

  getConnectCalendarEvents: (params: { from: string; to: string; scope?: string }) => {
    const q = new URLSearchParams({ from: params.from, to: params.to });
    if (params.scope) q.set('scope', params.scope);
    return request<ConnectCalendarEventListItemDto[]>(`/connect/calendar?${q.toString()}`);
  },
  getConnectCalendarEvent: (id: string) =>
    request<ConnectCalendarEventDetailDto>(`/connect/calendar/${id}`),
  createConnectCalendarEvent: (data: CreateConnectCalendarEventRequest) =>
    request<ConnectCalendarEventDetailDto>('/connect/calendar', {
      method: 'POST',
      body: JSON.stringify(data),
    }),
  updateConnectCalendarEvent: (id: string, data: UpdateConnectCalendarEventRequest) =>
    request<ConnectCalendarEventDetailDto>(`/connect/calendar/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    }),
  deleteConnectCalendarEvent: (id: string) =>
    request<void>(`/connect/calendar/${id}`, { method: 'DELETE' }),
  respondConnectCalendarEvent: (id: string, response: ConnectCalendarParticipantResponse) =>
    request<ConnectCalendarEventDetailDto>(`/connect/calendar/${id}/respond`, {
      method: 'POST',
      body: JSON.stringify({ response }),
    }),

  getConnectPatientContextMessages: (patientId: string) =>
    request<ConnectContextMessageDto[]>(`/connect/context/patient/${patientId}/messages`),
  getConnectGuideContextMessages: (guideId: string, type = 'tiss') =>
    request<ConnectContextMessageDto[]>(`/connect/context/guide/${guideId}/messages?type=${type}`),

  getConnectAiQuickQueries: () => request<ConnectAiQuickQueryDto[]>('/connect/ai/quick-queries'),
  askConnectAi: (question: string) =>
    request<ConnectAiAskResponse>('/connect/ai/ask', {
      method: 'POST',
      body: JSON.stringify({ question }),
    }),
  askConnectAiStream: async (
    question: string,
    onChunk: (chunk: ConnectAiStreamChunk) => void,
    signal?: AbortSignal,
  ): Promise<void> => {
    const response = await fetch(`${API_BASE}/connect/ai/ask/stream`, {
      method: 'POST',
      headers: authHeaders(),
      body: JSON.stringify({ question }),
      signal,
    });

    if (response.status === 401) {
      handleUnauthorized();
      throw new Error('Sessão expirada');
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ message: 'Erro ao consultar assistente' }));
      throw new Error(error.message ?? 'Falha na requisição');
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error('Streaming não suportado');
    }

    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const { done, value } = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n');
      buffer = lines.pop() ?? '';

      for (const line of lines) {
        if (!line.startsWith('data:')) continue;
        const json = line.slice(5).trim();
        if (!json) continue;
        try {
          onChunk(JSON.parse(json) as ConnectAiStreamChunk);
        } catch {
          // ignore malformed chunks
        }
      }
    }
  },

  getHelpSummary: () => request<HelpSummaryDto>('/help/summary'),
  getHelpCategories: () => request<HelpCategoryDto[]>('/help/categories'),
  searchHelp: (q: string, params?: { type?: HelpArticleType; category?: string; limit?: number }) => {
    const qs = new URLSearchParams();
    qs.set('q', q);
    if (params?.type) qs.set('type', params.type);
    if (params?.category) qs.set('category', params.category);
    if (params?.limit) qs.set('limit', String(params.limit));
    return request<HelpSearchResultDto>(`/help/search?${qs}`);
  },
  getHelpArticles: (params?: { type?: HelpArticleType; category?: string }) => {
    const qs = new URLSearchParams();
    if (params?.type) qs.set('type', params.type);
    if (params?.category) qs.set('category', params.category);
    const query = qs.toString();
    return request<HelpArticleListItemDto[]>(`/help/articles${query ? `?${query}` : ''}`);
  },
  getHelpArticle: (slug: string) => request<HelpArticleDetailDto>(`/help/articles/${encodeURIComponent(slug)}`),
  getHelpContext: (route: string) =>
    request<HelpContextDto>(`/help/context?route=${encodeURIComponent(route)}`),
  askHelp: (data: HelpAskRequest) =>
    request<HelpAskResponse>('/help/ask', { method: 'POST', body: JSON.stringify(data) }),
  createHelpSuggestion: (data: CreateHelpSuggestionRequest) =>
    request<HelpSuggestionDto>('/help/suggestions', { method: 'POST', body: JSON.stringify(data) }),
  getMyHelpSuggestions: () => request<HelpSuggestionDto[]>('/help/suggestions/mine'),
  markHelpTrainingComplete: (articleId: string) =>
    request<void>('/help/training/complete', {
      method: 'POST',
      body: JSON.stringify({ articleId }),
    }),
};

export type PagedResult<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type PatientDto = {
  id: string;
  fullName: string;
  socialName?: string;
  cpf: string;
  cns?: string;
  birthDate: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  addressCity?: string;
  addressState?: string;
  primaryInsuranceName?: string;
  hasPhoto: boolean;
  isActive: boolean;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  motherName?: string;
  emergencyContactRelationship?: string;
  createdAt: string;
  usesResponsibleCpf: boolean;
  legalResponsible?: LegalResponsibleDto | null;
  openReceivableCount?: number;
  lastAppointmentAt?: string | null;
  nextAppointmentAt?: string | null;
};

export type LegalResponsibleDto = {
  name: string;
  birthDate: string;
  relationship: number;
  rg: string;
  authorizationDocumentType?: number | null;
  authorizationDocumentReference?: string | null;
};

export type LegalResponsibleInput = {
  name: string;
  cpf: string;
  birthDate: string;
  relationship: number;
  rg: string;
  authorizationDocumentType?: number;
  authorizationDocumentReference?: string;
};

export type PatientInsuranceDto = {
  id: string;
  healthInsuranceId: string;
  healthInsuranceName: string;
  cardNumber: string;
  planName?: string;
  cardHolderName?: string;
  productCode?: string;
  cnsNumber?: string;
  accommodationType?: string;
  validFrom?: string;
  validUntil?: string;
  isPrimary: boolean;
};

export type PatientInsuranceInput = {
  healthInsuranceId: string;
  cardNumber: string;
  planName?: string;
  cardHolderName?: string;
  productCode?: string;
  cnsNumber?: string;
  accommodationType?: string;
  validFrom?: string;
  validUntil?: string;
  isPrimary: boolean;
};

export type PatientDetailDto = {
  id: string;
  fullName: string;
  socialName?: string;
  cpf: string;
  cns?: string;
  birthDate: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  motherName?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  notes?: string;
  photoData?: string;
  rg?: string;
  nationality?: string;
  bloodType?: string;
  occupation?: string;
  maritalStatus?: string;
  birthPlace?: string;
  isActive: boolean;
  createdAt: string;
  medicalRecordId?: string;
  medicalRecordNumber?: string;
  insurances: PatientInsuranceDto[];
  usesResponsibleCpf: boolean;
  legalResponsible?: LegalResponsibleDto | null;
};

export type PatientInitialAdmissionInput = {
  bedId: string;
  professionalId: string;
  reason: string;
  diagnosis?: string;
  notes?: string;
};

export type CreatePatientResult = {
  patient: PatientDetailDto;
  initialHospitalization?: HospitalizationDto;
};

export type CpfAvailabilityResult = {
  available: boolean;
  message?: string | null;
};

export type CreatePatientRequest = {
  fullName: string;
  socialName?: string;
  cpf: string;
  birthDate: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  motherName?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  notes?: string;
  photoData?: string;
  rg?: string;
  nationality?: string;
  bloodType?: string;
  occupation?: string;
  maritalStatus?: string;
  birthPlace?: string;
  insurances?: PatientInsuranceInput[];
  usesResponsibleCpf?: boolean;
  legalResponsible?: LegalResponsibleInput;
  initialAdmission?: PatientInitialAdmissionInput;
};

export type UpdatePatientRequest = Omit<
  CreatePatientRequest,
  'cpf' | 'initialAdmission' | 'usesResponsibleCpf' | 'legalResponsible'
> & { isActive: boolean };

export type AppointmentDto = {
  id: string;
  patientId: string;
  patientName: string;
  professionalId: string;
  professionalName: string;
  specialtyName: string;
  scheduledAt: string;
  durationMinutes: number;
  status: AppointmentStatusValue;
  reason?: string;
  room?: string;
};

export type CreateAppointmentRequest = {
  patientId: string;
  professionalId: string;
  scheduledAt: string;
  durationMinutes: number;
  reason?: string;
  notes?: string;
  room?: string;
  ignoreEligibilityWarning?: boolean;
};

export type CreateAppointmentResultDto = {
  appointment: AppointmentDto;
  warnings: string[];
};

export type ProfessionalDto = {
  id: string;
  fullName: string;
  crm?: string;
  specialtyId: string;
  specialtyName: string;
  hasPhoto: boolean;
};

export type ProfessionalListDto = {
  id: string;
  fullName: string;
  crm?: string;
  councilUf?: string;
  specialtyId: string;
  specialtyName: string;
  hasPhoto: boolean;
};

export type ProfessionalDetailDto = {
  id: string;
  fullName: string;
  socialName?: string;
  crm?: string;
  councilUf?: string;
  cpf?: string;
  rg?: string;
  birthDate?: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  notes?: string;
  photoData?: string;
  specialtyId: string;
  specialtyName: string;
  isActive: boolean;
  createdAt: string;
};

export type CreateProfessionalRequest = {
  fullName: string;
  socialName?: string;
  crm?: string;
  councilUf?: string;
  cpf?: string;
  rg?: string;
  birthDate?: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  notes?: string;
  photoData?: string;
  specialtyId: string;
};

export type UpdateProfessionalRequest = CreateProfessionalRequest & { isActive: boolean };

export type MedicalRecordSummaryDto = {
  id: string;
  patientId: string;
  patientName: string;
  recordNumber: string;
  entries: MedicalRecordEntryDto[];
};

export type MedicalRecordEntryDto = {
  id: string;
  entryType: number | string;
  content: string;
  cid10Code?: string;
  professionalName?: string;
  hospitalizationId?: string;
  createdAt: string;
  isSigned: boolean;
  signedAt?: string;
  signedByProfessionalName?: string;
  signatureHash?: string;
  hasSignatureImage: boolean;
};

export type CreateMedicalRecordEntryRequest = {
  entryType: number;
  content: string;
  cid10Code?: string;
  professionalId?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  clientRequestId?: string;
  signatureImage?: string;
  password?: string;
  signatureType?: number;
};

export type UpdateMedicalRecordEntryRequest = {
  entryType: number;
  content: string;
  cid10Code?: string;
};

export type SignMedicalRecordEntryRequest = {
  professionalId: string;
  signatureImage: string;
  password: string;
  signatureType?: number;
};

export type PendingSignatureEntryDto = {
  entryId: string;
  patientId: string;
  patientName: string;
  recordNumber?: string;
  entryType: number;
  contentPreview: string;
  createdAt: string;
  professionalName?: string;
};

export type PatientIdentityDto = {
  id: string;
  patientId: string;
  hospitalizationId?: string;
  identityType: number;
  code: string;
  labelContext?: string;
  issuedAt: string;
  isActive: boolean;
};

export type PatientIdentityResolveDto = {
  patientId: string;
  patientName: string;
  medicalRecordNumber?: string;
  socialName?: string;
  birthDate: string;
  bloodType?: string;
  code: string;
  identityType: number;
  labelContext?: string;
  hospitalizationId?: string;
  bedNumber?: string;
  wardName?: string;
  allergyWarnings?: string[];
};

export type GenerateBraceletRequest = {
  hospitalizationId?: string;
};

export type GenerateLabelRequest = {
  labelType: number;
  labelContext?: string;
  hospitalizationId?: string;
};

export type BedsideVitalsRequest = {
  identityCode: string;
  bloodPressure?: string;
  heartRate?: string;
  respiratoryRate?: string;
  temperature?: string;
  spO2?: string;
  password: string;
};

export type BedsideMedicationRequest = {
  identityCode: string;
  prescriptionEntryId?: string;
  medicationName: string;
  dose: string;
  route: string;
  password: string;
};

export type BedsideCareResultDto = {
  entryId: string;
  isSigned: boolean;
  createdAt: string;
  message: string;
};

export type DigitalRecordSummaryDto = {
  record: MedicalRecordSummaryDto;
  activeHospitalization?: HospitalizationDto;
  hospitalizationHistory: HospitalizationDto[];
  tissGuides: TissGuideDto[];
};

/** API serializa enums operacionais em PT-BR (PortugueseEnumJsonConverterFactory) ou número. */
export type ApiEnum<T extends number> = T | string;

export type PixChargeDto = {
  id: string;
  financialAccountId: string;
  patientId: string;
  patientName: string;
  txId: string;
  amount: number;
  status: ApiEnum<1 | 2 | 3 | 4>;
  copyPasteCode: string;
  expiresAt: string;
  paidAt?: string;
  createdAt: string;
};

export type FinancialPaymentDto = {
  id: string;
  financialAccountId: string;
  amount: number;
  method: ApiEnum<1 | 2 | 3 | 4 | 5>;
  paidAt: string;
  notes?: string;
  createdAt: string;
  installmentCount?: number | null;
  installments: FinancialPaymentInstallmentDto[];
};

export type FinancialSummaryDto = {
  receivableOpen: number;
  payableOpen: number;
  totalReceived: number;
  totalPaidOut: number;
  receivedThisMonth: number;
  paidOutThisMonth: number;
  openProposalsCount: number;
  openProposalsBalance: number;
  openHonorariosCount: number;
  openHonorariosBalance: number;
};

export type RegisterPaymentRequest = {
  amount: number;
  method: number;
  paidAt?: string;
  notes?: string;
  installments?: PaymentInstallmentInput[];
};

export type PaymentInstallmentInput = {
  installmentNumber: number;
  amount: number;
  dueDate: string;
};

export type FinancialPaymentInstallmentDto = {
  installmentNumber: number;
  installmentCount: number;
  amount: number;
  dueDate: string;
  financialAccountId?: string | null;
};

export type FinancialAccountDto = {
  id: string;
  direction: ApiEnum<1 | 2>;
  patientId?: string;
  patientName?: string;
  supplierId?: string;
  supplierName?: string;
  counterpartyName?: string;
  counterpartyDisplay: string;
  appointmentId?: string;
  hospitalizationId?: string;
  category: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12>;
  description: string;
  notes?: string;
  invoiceNumber?: string;
  amount: number;
  paidAmount: number;
  balance: number;
  status: ApiEnum<1 | 2 | 3 | 4>;
  dueDate?: string;
  paidAt?: string;
  lastPaymentMethod?: ApiEnum<1 | 2 | 3 | 4 | 5>;
  expectedPaymentMethod?: ApiEnum<1 | 2 | 3 | 4 | 5>;
  paymentCount: number;
  createdAt: string;
  parentFinancialAccountId?: string | null;
  installmentNumber?: number | null;
  installmentCount?: number | null;
  lineItems: FinancialAccountLineItemDto[];
};

export type FinancialAccountLineItemDto = {
  id: string;
  description: string;
  quantity: number;
  unitAmount: number;
  totalAmount: number;
  notes?: string;
};

export type CreateFinancialAccountRequest = {
  direction: number;
  patientId?: string;
  supplierId?: string;
  counterpartyName?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  category: number;
  description: string;
  amount: number;
  dueDate?: string;
  notes?: string;
  expectedPaymentMethod?: number;
  invoiceNumber?: string;
  installmentCount?: number;
  lineItems?: FinancialAccountLineItemInput[];
};

export type FinancialAccountLineItemInput = {
  description: string;
  quantity: number;
  unitAmount: number;
  notes?: string;
};

export type FinancialCashSessionDto = {
  id: string;
  label: string;
  openedAt: string;
  closedAt?: string;
  openingBalance: number;
  closingBalance?: number;
  expectedBalance: number;
  status: ApiEnum<1 | 2>;
  notes?: string;
  cashReceived: number;
  cashPaidOut: number;
  counterReceived: number;
  counterPaidOut: number;
  dayOperationalReceived: number;
  dayOperationalPaidOut: number;
};

export type OpenFinancialCashSessionRequest = {
  label: string;
  openingBalance: number;
  notes?: string;
};

export type CloseFinancialCashSessionRequest = {
  closingBalance: number;
  notes?: string;
};

export type MiscellaneousReceiptDto = {
  id: string;
  receiptNumber: string;
  receiptDate: string;
  payerName: string;
  receiverName: string;
  amount: number;
  description: string;
  paymentMethod: ApiEnum<1 | 2 | 3 | 4 | 5>;
  reference?: string;
  createdAt: string;
};

export type CreateMiscellaneousReceiptRequest = {
  receiptDate: string;
  payerName: string;
  receiverName: string;
  amount: number;
  description: string;
  paymentMethod: number;
  reference?: string;
};

export type UpdateMiscellaneousReceiptRequest = CreateMiscellaneousReceiptRequest;

export type VaccineCatalogDto = {
  id: string;
  code: string;
  name: string;
  scheduleType: ApiEnum<1 | 2 | 3>;
  displayOrder: number;
};

export type PatientVaccinationDto = {
  id: string;
  patientId: string;
  patientName: string;
  vaccineCatalogId: string;
  vaccineName: string;
  vaccineCode: string;
  administeredAt: string;
  doseNumber: number;
  batchNumber?: string;
  professionalId?: string;
  professionalName?: string;
  notes?: string;
};

export type CreatePatientVaccinationRequest = {
  patientId: string;
  vaccineCatalogId: string;
  administeredAt: string;
  doseNumber?: number;
  batchNumber?: string;
  professionalId?: string;
  notes?: string;
};

export type EpidemicDiseaseCatalogDto = {
  id: string;
  code: string;
  name: string;
  diseaseClass: ApiEnum<1 | 2 | 3 | 4 | 5>;
  includeOpd: boolean;
  includeIpd: boolean;
  displayOrder: number;
};

export type WardStockBalanceDto = {
  id: string;
  wardId: string;
  wardName: string;
  productId: string;
  productName: string;
  productSku: string;
  quantityOnHand: number;
  minimumStock: number;
  unit: string;
  isLowStock: boolean;
};

export type WardStockMovementDto = {
  id: string;
  wardId: string;
  wardName: string;
  productId: string;
  productName: string;
  movementType: ApiEnum<1 | 2 | 3 | 4>;
  quantity: number;
  unit: string;
  patientId?: string;
  patientName?: string;
  reference?: string;
  notes?: string;
  movementDate: string;
};

export type WardStockTransferRequest = {
  wardId: string;
  productId: string;
  quantity: number;
  reference?: string;
  notes?: string;
};

export type WardStockDispenseRequest = {
  wardId: string;
  productId: string;
  patientId: string;
  quantity: number;
  notes?: string;
};

export type PayableCategoryPresetDto = {
  category: ApiEnum<7 | 8 | 9 | 10 | 11 | 12>;
  label: string;
  suggestedAmount: number;
  descriptionTemplate: string;
  suggestedDueDays: number;
};

export type FinancialAccountSourceOptionDto = {
  sourceType: string;
  sourceId: string;
  label: string;
  detail: string;
  suggestedAmount: number;
  suggestedDescription: string;
  suggestedCategory: ApiEnum<1 | 2 | 3 | 4 | 5 | 6>;
  alreadyBilled: boolean;
};

export type FinancialAccountCategoryPresetDto = {
  category: ApiEnum<1 | 2 | 3 | 4 | 5 | 6>;
  label: string;
  suggestedAmount: number;
  descriptionTemplate: string;
  suggestedDueDays: number;
};

export type FinancialAccountCreateSuggestionsDto = {
  patientId: string;
  patientName: string;
  cpf: string;
  phone?: string;
  insuranceName?: string;
  paymentModality: number;
  suggestedDueDays: number;
  outstandingBalance: number;
  sourceOptions: FinancialAccountSourceOptionDto[];
  categoryPresets: FinancialAccountCategoryPresetDto[];
};

export const entryTypeLabels: Record<number, string> = {
  1: 'Anamnese',
  2: 'Evolução',
  3: 'Prescrição',
  4: 'Solicitação de exame',
  5: 'Procedimento',
};

/** Rótulos quando a API serializa o enum como string (JsonStringEnumConverter). */
export const entryTypeNameLabels: Record<string, string> = {
  Anamnesis: 'Anamnese',
  Evolution: 'Evolução',
  Prescription: 'Prescrição',
  ExamRequest: 'Solicitação de exame',
  Procedure: 'Procedimento',
};

const entryTypeNumbers: Record<string, number> = {
  Anamnesis: 1,
  Evolution: 2,
  Prescription: 3,
  ExamRequest: 4,
  Procedure: 5,
};

export function entryTypeToNumber(entryType: number | string): number {
  if (typeof entryType === 'number') return entryType;
  const fromName = entryTypeNumbers[entryType];
  if (fromName != null) return fromName;
  const parsed = Number(entryType);
  return Number.isFinite(parsed) ? parsed : 0;
}

export function formatEntryTypeLabel(entryType: number | string): string {
  if (typeof entryType === 'string') {
    return entryTypeNameLabels[entryType]
      ?? entryTypeLabels[Number(entryType)]
      ?? entryType;
  }
  return entryTypeLabels[entryType] ?? String(entryType);
}

export const genderLabels: Record<number, string> = {
  0: 'Não informado',
  1: 'Masculino',
  2: 'Feminino',
  3: 'Outro',
};

export const appointmentStatusLabels: Record<number, string> = {
  1: 'Agendado',
  2: 'Confirmado',
  3: 'Em atendimento',
  4: 'Concluído',
  5: 'Cancelado',
  6: 'Faltou',
};

export const appointmentStatusNameLabels: Record<string, string> = {
  Scheduled: 'Agendado',
  Confirmed: 'Confirmado',
  InProgress: 'Em atendimento',
  Completed: 'Concluído',
  Cancelled: 'Cancelado',
  NoShow: 'Faltou',
  Agendado: 'Agendado',
  Confirmado: 'Confirmado',
  'Em atendimento': 'Em atendimento',
  Concluído: 'Concluído',
  Cancelado: 'Cancelado',
  Faltou: 'Faltou',
};

const appointmentStatusNameToCode: Record<string, number> = {
  Agendado: 1,
  Confirmado: 2,
  'Em atendimento': 3,
  Concluído: 4,
  Cancelado: 5,
  Faltou: 6,
  Scheduled: 1,
  Confirmed: 2,
  InProgress: 3,
  Completed: 4,
  Cancelled: 5,
  NoShow: 6,
};

/** Converte status da API (PT-BR, inglês legado ou número) para código 1–6. */
export function normalizeAppointmentStatus(status: string | number): number {
  if (typeof status === 'number') return status;
  return appointmentStatusNameToCode[status] ?? 0;
}

export type AppointmentStatusValue = number | keyof typeof appointmentStatusNameLabels;

export function isAppointmentStatus(status: AppointmentStatusValue, ...codes: number[]): boolean {
  return codes.includes(normalizeAppointmentStatus(status));
}

/** Use em vez de appointmentStatusLabels[status] — a API envia enum como string. */
export function appointmentStatusLabel(status: AppointmentStatusValue): string {
  return formatAppointmentStatus(status);
}

export function formatAppointmentStatus(status: string | number) {
  if (typeof status === 'number') return appointmentStatusLabels[status] ?? String(status);
  if (appointmentStatusNameLabels[status]) return appointmentStatusNameLabels[status];
  const code = appointmentStatusNameToCode[status];
  return code ? appointmentStatusLabels[code] ?? status : status;
}

export const emergencyVisitStatusLabels: Record<string, string> = {
  Aguardando: 'Aguardando',
  'Em atendimento': 'Em atendimento',
  'Em triagem': 'Em triagem',
  Alta: 'Alta',
  Encaminhado: 'Encaminhado',
  Waiting: 'Aguardando',
  InCare: 'Em atendimento',
  InTriage: 'Em triagem',
  Discharged: 'Alta',
  Referred: 'Encaminhado',
  1: 'Aguardando',
  2: 'Em atendimento',
  3: 'Alta',
  4: 'Encaminhado',
};

export function formatEmergencyVisitStatus(status: string | number): string {
  const key = String(status);
  return emergencyVisitStatusLabels[key] ?? key;
}

const financialStatusByName: Record<string, number> = {
  'Em aberto': 1,
  Parcial: 2,
  Pago: 3,
  Cancelado: 4,
  Open: 1,
  PartiallyPaid: 2,
  Paid: 3,
  Cancelled: 4,
};

const pixChargeStatusByName: Record<string, number> = {
  Pendente: 1,
  Pago: 2,
  Expirado: 3,
  Cancelado: 4,
  Pending: 1,
  Paid: 2,
  Expired: 3,
  Cancelled: 4,
};

export function financialStatusValue(status: ApiEnum<1 | 2 | 3 | 4>): number {
  if (typeof status === 'number') return status;
  return financialStatusByName[status] ?? 0;
}

export function pixChargeStatusValue(status: ApiEnum<1 | 2 | 3 | 4>): number {
  if (typeof status === 'number') return status;
  return pixChargeStatusByName[status] ?? 0;
}

export function isFinancialOpen(status: ApiEnum<1 | 2 | 3 | 4>): boolean {
  const value = financialStatusValue(status);
  return value === 1 || value === 2;
}

export function financialStatusLabel(status: ApiEnum<1 | 2 | 3 | 4>): string {
  return financialStatusLabels[financialStatusValue(status)] ?? '—';
}

export function pixChargeStatusLabel(status: ApiEnum<1 | 2 | 3 | 4>): string {
  return pixChargeStatusLabels[pixChargeStatusValue(status)] ?? '—';
}

export const financialStatusLabels: Record<number, string> = {
  1: 'Em aberto',
  2: 'Parcial',
  3: 'Pago',
  4: 'Cancelado',
};

export const financialDirectionLabels: Record<number, string> = {
  1: 'Entrada',
  2: 'Saída',
};

const financialDirectionByName: Record<string, number> = {
  'A receber': 1,
  'A pagar': 2,
  Receivable: 1,
  Payable: 2,
};

export function financialDirectionValue(direction: ApiEnum<1 | 2>): number {
  if (typeof direction === 'number') return direction;
  return financialDirectionByName[direction] ?? 1;
}

export function financialDirectionLabel(direction: ApiEnum<1 | 2>): string {
  return financialDirectionLabels[financialDirectionValue(direction)] ?? '—';
}

export function isFinancialReceivable(direction: ApiEnum<1 | 2>): boolean {
  return financialDirectionValue(direction) === 1;
}

export const paymentMethodLabels: Record<number, string> = {
  1: 'Dinheiro',
  2: 'PIX',
  3: 'Cartão débito',
  4: 'Cartão crédito',
  5: 'Transferência',
};

const paymentMethodByName: Record<string, number> = {
  Dinheiro: 1,
  PIX: 2,
  'Cartão de débito': 3,
  'Cartão de crédito': 4,
  Transferência: 5,
  Cash: 1,
  Pix: 2,
  DebitCard: 3,
  CreditCard: 4,
  BankTransfer: 5,
};

export function paymentMethodValue(method: ApiEnum<1 | 2 | 3 | 4 | 5>): number {
  if (typeof method === 'number') return method;
  return paymentMethodByName[method] ?? 0;
}

export function paymentMethodLabel(method?: ApiEnum<1 | 2 | 3 | 4 | 5>): string {
  if (method === undefined || method === null) return '—';
  return paymentMethodLabels[paymentMethodValue(method)] ?? '—';
}

export const financialCategoryLabels: Record<number, string> = {
  1: 'Consulta',
  2: 'Internação',
  3: 'Exame',
  4: 'Coparticipação',
  5: 'Estacionamento',
  6: 'Outros',
  7: 'Compra / fornecedor',
  8: 'Folha de pagamento',
  9: 'Utilidades',
  10: 'Impostos e taxas',
  11: 'Manutenção',
  12: 'Outras despesas',
};

export const paymentModalityLabels: Record<number, string> = {
  1: 'Particular',
  2: 'Convênio',
  3: 'SUS',
};

const financialCategoryByName: Record<string, number> = {
  Consulta: 1,
  Internação: 2,
  Exame: 3,
  Coparticipação: 4,
  Estacionamento: 5,
  Outros: 6,
  'Compra / fornecedor': 7,
  'Folha de pagamento': 8,
  Utilidades: 9,
  'Impostos e taxas': 10,
  Manutenção: 11,
  'Outras despesas': 12,
  Consultation: 1,
  Hospitalization: 2,
  Exam: 3,
  Copayment: 4,
  Parking: 5,
  Other: 6,
  SupplierPurchase: 7,
  Payroll: 8,
  Utilities: 9,
  Taxes: 10,
  Maintenance: 11,
  OtherExpense: 12,
};

export function financialCategoryLabel(category: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12>): string {
  return financialCategoryLabels[financialCategoryValue(category)] ?? 'Outros';
}

export function financialCategoryValue(category: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12>): number {
  if (typeof category === 'number') return category;
  return financialCategoryByName[category] ?? 6;
}

export const pixChargeStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Pago',
  3: 'Expirado',
  4: 'Cancelado',
};

export const roleLabels: Record<string, string> = {
  Admin: 'Administrador',
  HospitalDirector: 'Diretor Hospitalar',
  Doctor: 'Médico',
  Nurse: 'Enfermeiro',
  NursingTechnician: 'Téc. Enfermagem',
  Reception: 'Recepção',
  Billing: 'Faturista',
  Pharmacy: 'Farmácia',
  Warehouse: 'Almoxarifado',
  Porter: 'Maqueiro',
  Hospitality: 'Hotelaria',
  IT: 'TI',
  Auditor: 'Auditor',
  Insurance: 'Convênios',
  Patient: 'Paciente',
  Administrador: 'Administrador',
  'Diretor hospitalar': 'Diretor Hospitalar',
  Médico: 'Médico',
  Enfermeiro: 'Enfermeiro',
  'Téc. enfermagem': 'Téc. Enfermagem',
  Recepção: 'Recepção',
  Faturista: 'Faturista',
  Farmácia: 'Farmácia',
  Almoxarifado: 'Almoxarifado',
  Maqueiro: 'Maqueiro',
  Hotelaria: 'Hotelaria',
  TI: 'TI',
  Convênios: 'Convênios',
  Paciente: 'Paciente',
};

export function userRoleLabel(role: string): string {
  return roleLabels[role] ?? role;
}

export type UserRoleName = keyof typeof roleLabels;

export type UserListDto = {
  id: string;
  fullName: string;
  email: string;
  role: UserRoleName;
  isActive: boolean;
  createdAt: string;
  professionalId?: string;
  professionalName?: string;
  patientId?: string;
  patientName?: string;
};

export type UserDetailDto = UserListDto;

export type CreateUserRequest = {
  fullName: string;
  email: string;
  password: string;
  role: UserRoleName;
  professionalId?: string;
  patientId?: string;
};

export type UpdateUserRequest = {
  fullName: string;
  email: string;
  role: UserRoleName;
  isActive: boolean;
  professionalId?: string;
  patientId?: string;
};

export type WardDto = {
  id: string;
  name: string;
  code?: string;
  floor?: string;
  description?: string;
  coverageModality: ApiEnum<1 | 2 | 3 | 4>;
  category: ApiEnum<1 | 2 | 3 | 4 | 5>;
  totalBeds: number;
  availableBeds: number;
  occupiedBeds: number;
  blockedBeds: number;
};
export type BedDto = {
  id: string;
  wardId: string;
  wardName: string;
  wardCode?: string;
  wardCoverageModality: ApiEnum<1 | 2 | 3 | 4>;
  wardCategory: ApiEnum<1 | 2 | 3 | 4 | 5>;
  bedNumber: string;
  status: ApiEnum<1 | 2 | 3>;
  statusReason?: string;
  blockedUntil?: string;
  occupantPatientId?: string;
  occupantPatientName?: string;
  occupantProfessionalName?: string;
  occupantAdmittedAt?: string;
};
export type CreateWardRequest = {
  name: string;
  code?: string;
  floor?: string;
  description?: string;
  coverageModality: number;
  category: number;
};
export type UpdateWardRequest = CreateWardRequest;
export type CreateBedRequest = { wardId: string; bedNumber: string };
export type UpdateBedRequest = { bedNumber: string };
export type UpdateBedStatusRequest = { status: number; reason?: string; blockedUntil?: string };
export type HospitalizationSusDataDto = {
  aihNumber?: string;
  susCompetence?: string;
  primaryCid10Code?: string;
  secondaryCid10Code?: string;
  primarySigtapProcedureCode?: string;
  secondarySigtapProcedureCode?: string;
  susCharacter?: ApiEnum<1 | 2 | 3>;
  susModality?: ApiEnum<1 | 2 | 3 | 4 | 5>;
  cnesCode?: string;
  susAuthorizationNumber?: string;
  aihExportedAt?: string;
};
export type UpdateHospitalizationSusDataRequest = {
  aihNumber?: string;
  susCompetence?: string;
  primaryCid10Code?: string;
  secondaryCid10Code?: string;
  primarySigtapProcedureCode?: string;
  secondarySigtapProcedureCode?: string;
  susCharacter?: number;
  susModality?: number;
  cnesCode?: string;
  susAuthorizationNumber?: string;
};
export type HospitalizationSusDataInput = Omit<UpdateHospitalizationSusDataRequest, 'aihNumber' | 'susCompetence'>;
export type HospitalizationDto = {
  id: string;
  patientId: string;
  patientName: string;
  patientIsDeceased: boolean;
  patientCns?: string;
  bedId: string;
  bedNumber: string;
  wardName: string;
  wardCode?: string;
  wardCoverageModality: ApiEnum<1 | 2 | 3 | 4>;
  wardCategory: ApiEnum<1 | 2 | 3 | 4 | 5>;
  professionalId: string;
  professionalName: string;
  admittedAt: string;
  dischargedAt?: string;
  status: ApiEnum<1 | 2 | 3>;
  reason: string;
  diagnosis?: string;
  billingAccountClosedAt?: string;
  susData?: HospitalizationSusDataDto | null;
};
export type HospitalizationHubFilterParams = {
  dateFrom?: string;
  dateTo?: string;
  patientId?: string;
  wardId?: string;
  professionalId?: string;
  modality?: number;
  category?: number;
  status?: number;
  search?: string;
  groupId?: string;
  skip?: number;
  take?: number;
};
export type HospitalizationHubListItemDto = {
  id: string;
  itemType: 'hospitalization' | 'request';
  patientId: string;
  patientName: string;
  wardName?: string;
  bedNumber?: string;
  professionalName?: string;
  eventAt: string;
  status: number;
  statusLabel: string;
  modalityLabel?: string;
  diagnosis?: string;
  hasSusAih: boolean;
  daysHospitalized?: number | null;
};
export type HospitalizationHubListResultDto = {
  total: number;
  items: HospitalizationHubListItemDto[];
};
export type HospitalizationHubSliceDto = {
  label: string;
  count: number;
};
export type HospitalizationHubDashboardDto = {
  activeCount: number;
  dischargedInPeriod: number;
  pendingRequests: number;
  availableBeds: number;
  occupiedBeds: number;
  blockedBeds: number;
  avgLengthOfStayDays?: number | null;
  byWard: HospitalizationHubSliceDto[];
  byModality: HospitalizationHubSliceDto[];
  byProfessional: HospitalizationHubSliceDto[];
};
export type TransferBedRequest = { targetBedId: string; professionalId?: string; reason?: string };
export type HospitalizationSnippetDto = { id: string; text: string; usageCount: number };
export type BedTransferDto = {
  id: string; hospitalizationId: string; patientName: string;
  fromWardName: string; fromBedNumber: string; toWardName: string; toBedNumber: string;
  professionalName?: string; transferredAt: string; reason?: string;
};
export type GovIntegrationProfileDto = {
  system: string; name: string; description: string; priority: string;
  credentialStatus: string; mockEnabled: boolean; officialEndpoint?: string; credentialNote?: string;
};
export type CnsLookupResultDto = {
  found: boolean; cns?: string; fullName?: string; birthDate?: string;
  motherName?: string; gender?: string; addressCity?: string; addressState?: string; message?: string;
};
export type CnesEstablishmentDto = {
  cnesCode: string; name: string; fantasyName?: string; address?: string;
  city?: string; state?: string; managementType?: string; professionals: CnesProfessionalDto[];
};
export type CnesProfessionalDto = {
  name: string; cns?: string; cboCode?: string; specialty?: string; occupation?: string;
};
export type GovIntegrationActionResultDto = { messageId?: string; success: boolean; message: string; details?: string };
export type SihAihPreviewDto = {
  hospitalizationId: string;
  patientName: string;
  patientCns?: string;
  wardName: string;
  bedNumber: string;
  admittedAt: string;
  lengthOfStayDays: number;
  primaryDiagnosis?: string;
  primaryCid10Code?: string;
  secondaryCid10Code?: string;
  primaryProcedureCode?: string;
  secondaryProcedureCode?: string;
  character?: string;
  modality?: string;
  cnesCode?: string;
  authorizationNumber?: string;
  competence: string;
  aihNumber: string;
  payloadSummary: string;
};
export type SiaDocumentPreviewDto = {
  documentType: string;
  competence: string;
  recordCount: number;
  estimatedValue: number;
  payloadSummary: string;
  lines?: SiaProductionLineDto[];
};
export type SiaProductionLineDto = {
  patientName: string;
  patientCns?: string;
  procedureCode: string;
  procedureLabel: string;
  serviceDate: string;
  quantity: number;
  unitValue: number;
  professionalCbo?: string;
};
export type DatasusExportFileDto = {
  fileName: string;
  contentType: string;
  content: string;
  recordCount: number;
  checksumSha256: string;
  layoutVersion: string;
  competence: string;
  documentType: string;
};
export type RndsPatientSummaryDto = {
  patientId: string; patientName: string; mockData: boolean; items: RndsClinicalItemDto[];
};
export type RndsClinicalItemDto = {
  category: string; title: string; occurredAt?: string; source: string;
};

export const wardModalityLabels: Record<number, string> = {
  1: 'Particular',
  2: 'Convênio',
  3: 'SUS',
  4: 'Mista',
};

export const wardCategoryLabels: Record<number, string> = {
  1: 'Enfermaria',
  2: 'Apartamento',
  3: 'UTI',
  4: 'Pediatria',
  5: 'Maternidade',
};

export function resolvePatientModality(insuranceName?: string | null): number {
  if (!insuranceName) return 1;
  if (insuranceName.toLowerCase() === 'sus') return 3;
  if (insuranceName.toLowerCase() === 'particular') return 1;
  return 2;
}

const wardModalityByName: Record<string, number> = {
  Particular: 1,
  Convênio: 2,
  SUS: 3,
  Misto: 4,
  Convenio: 2,
  Sus: 3,
  Mixed: 4,
};

const wardCategoryByName: Record<string, number> = {
  Enfermaria: 1,
  Apartamento: 2,
  UTI: 3,
  Pediátrica: 4,
  Maternidade: 5,
  Uti: 3,
  Pediatrica: 4,
};

const bedStatusByName: Record<string, number> = {
  Disponível: 1,
  Ocupado: 2,
  Manutenção: 3,
  Higienização: 4,
  Reservado: 5,
  Available: 1,
  Occupied: 2,
  Maintenance: 3,
  Cleaning: 4,
  Reserved: 5,
};

const hospitalizationStatusByName: Record<string, number> = {
  Internado: 1,
  Alta: 2,
  Transferido: 3,
  Active: 1,
  Discharged: 2,
  Transferred: 3,
};

export function wardModalityValue(modality: ApiEnum<1 | 2 | 3 | 4>): number {
  if (typeof modality === 'number') return modality;
  return wardModalityByName[modality] ?? 0;
}

export function wardCategoryValue(category: ApiEnum<1 | 2 | 3 | 4 | 5>): number {
  if (typeof category === 'number') return category;
  return wardCategoryByName[category] ?? 0;
}

export function bedStatusValue(status: ApiEnum<1 | 2 | 3 | 4 | 5>): number {
  if (typeof status === 'number') return status;
  return bedStatusByName[status] ?? 0;
}

export function hospitalizationStatusValue(status: ApiEnum<1 | 2 | 3>): number {
  if (typeof status === 'number') return status;
  return hospitalizationStatusByName[status] ?? 0;
}

export function isBedAvailable(status: ApiEnum<1 | 2 | 3 | 4 | 5>): boolean {
  return bedStatusValue(status) === 1;
}

export function isBedOccupied(status: ApiEnum<1 | 2 | 3 | 4 | 5>): boolean {
  return bedStatusValue(status) === 2;
}

export function isBedReserved(status: ApiEnum<1 | 2 | 3 | 4 | 5>): boolean {
  return bedStatusValue(status) === 5;
}

export function isHospitalizationActive(status: ApiEnum<1 | 2 | 3>): boolean {
  return hospitalizationStatusValue(status) === 1;
}

export function bedStatusLabel(status: ApiEnum<1 | 2 | 3 | 4 | 5>): string {
  return bedStatusLabels[bedStatusValue(status)] ?? '—';
}

export function hospitalizationStatusLabel(status: ApiEnum<1 | 2 | 3>): string {
  return hospitalizationStatusLabels[hospitalizationStatusValue(status)] ?? '—';
}

export function isWardCompatible(wardModality: ApiEnum<1 | 2 | 3 | 4>, patientModality: number): boolean {
  const ward = wardModalityValue(wardModality);
  return ward === 4 || ward === patientModality;
}
export type HospitalizationRequestDto = {
  id: string;
  patientId: string;
  patientName: string;
  requestingProfessionalId: string;
  requestingProfessionalName: string;
  preferredWardId?: string;
  preferredWardName?: string;
  preferredWardCategory?: ApiEnum<1 | 2 | 3 | 4 | 5>;
  reason: string;
  diagnosis?: string;
  cid10Code?: string;
  notes?: string;
  priority: ApiEnum<1 | 2 | 3>;
  status: ApiEnum<1 | 2 | 3 | 4 | 5>;
  requestedAt: string;
  reviewedByProfessionalId?: string;
  reviewedByProfessionalName?: string;
  reviewedAt?: string;
  reviewNotes?: string;
  hospitalizationId?: string;
};
export type CreateHospitalizationRequestRequest = {
  patientId: string;
  requestingProfessionalId: string;
  reason: string;
  diagnosis?: string;
  cid10Code?: string;
  notes?: string;
  preferredWardId?: string;
  preferredWardCategory?: number;
  priority: number;
  aiTriageLogId?: string;
};
export type ReviewHospitalizationRequestRequest = {
  approve: boolean;
  reviewedByProfessionalId: string;
  reviewNotes?: string;
};
export type AdmitFromHospitalizationRequestRequest = {
  bedId: string;
  professionalId: string;
  notes?: string;
};
export type AdmitPatientRequest = {
  patientId: string; bedId: string; professionalId: string;
  reason: string; diagnosis?: string; notes?: string; aiTriageLogId?: string;
  hospitalizationRequestId?: string;
};
export type OperatingRoomDto = { id: string; name: string; status: number; location?: string };
export type SurgeryDto = {
  id: string; patientId: string; patientName: string; operatingRoomId: string;
  operatingRoomName: string; surgeonId: string; surgeonName: string; procedureName: string;
  scheduledAt: string; estimatedDurationMinutes: number; status: number; notes?: string;
  consentConfirmed: boolean;
  omsSignInCompleted: boolean;
  omsTimeOutCompleted: boolean;
  omsSignOutCompleted: boolean;
};
export type UpdateSurgerySafetyChecklistRequest = {
  consentConfirmed?: boolean;
  omsSignInCompleted?: boolean;
  omsTimeOutCompleted?: boolean;
  omsSignOutCompleted?: boolean;
};
export type RegisterPatientDeathRequest = {
  notes?: string;
  primaryCid10Code?: string;
};
export type CreateSurgeryRequest = {
  patientId: string; operatingRoomId: string; surgeonId: string; procedureName: string;
  scheduledAt: string; estimatedDurationMinutes: number; notes?: string;
};
export type ProductDto = {
  id: string; name: string; sku: string; type: number; unit: string;
  quantityOnHand: number; minimumStock: number; isLowStock: boolean;
  presentation?: string; barcode?: string; category?: string; averageSalePrice?: number;
  manufacturer?: string; defaultLocation?: string;
};
export type ProductDetailDto = ProductDto & {
  maximumStock: number;
  description?: string;
  contentQuantity?: number;
  manufacturer?: string;
  defaultLocation?: string;
  tussCode?: string;
  expiryWarningDays: number;
  averagePurchasePrice: number;
  allowOutboundFromRegister: boolean;
  entryLocations?: string;
  photoData?: string;
};
export type StockMovementDto = {
  id: string; productId: string; productName: string; type: number;
  quantity: number; reason: string; reference?: string; createdAt: string;
  patientOrSupplier?: string; responsibleName?: string; userName?: string;
  batchNumber?: string; individualCode?: string; location?: string;
  expiryDate?: string; invoiceNumber?: string; unitPrice?: number; account?: string;
};
export type StockInboundRequest = {
  productId: string; quantity: number; reason: string; reference?: string;
  patientOrSupplier?: string; responsibleName?: string; userName?: string;
  batchNumber?: string; individualCode?: string; location?: string;
  expiryDate?: string; invoiceNumber?: string; unitPrice?: number; account?: string;
};
export type StockOutboundRequest = {
  productId: string; quantity: number; reason: string;
  patientOrSupplier?: string; responsibleName?: string; userName?: string;
  batchNumber?: string; individualCode?: string; location?: string;
  invoiceNumber?: string; unitPrice?: number; account?: string;
};
export type ProductBillingRuleDto = {
  id: string; productId: string; priceTable: string; referenceTable?: string;
  code?: string; pricePfb: number; pmc: number; edition?: string;
  validFrom?: string; validTo?: string; isActive: boolean;
};
export type CreateProductBillingRuleRequest = {
  priceTable: string; referenceTable?: string; code?: string;
  pricePfb: number; pmc: number; edition?: string;
  validFrom?: string; validTo?: string; isActive?: boolean;
};
export type UpdateProductBillingRuleRequest = CreateProductBillingRuleRequest & { isActive: boolean };
export type InventoryLookupItemDto = {
  id: string;
  type: number;
  name: string;
};
export type CreateInventoryLookupItemRequest = { name: string };
export type UpdateInventoryLookupItemRequest = { name: string };
export type MedicationInsuranceMappingDto = {
  id: string;
  prescribedProductId: string;
  prescribedProductName: string;
  referenceProductId: string;
  referenceProductName: string;
  healthInsuranceId: string;
  healthInsuranceName: string;
};
export type CreateMedicationInsuranceMappingRequest = {
  prescribedProductId: string;
  referenceProductId: string;
  healthInsuranceId: string;
};
export type UpdateMedicationInsuranceMappingRequest = CreateMedicationInsuranceMappingRequest;
export type ProductKitItemDto = {
  id: string;
  productId: string;
  productName: string;
  productSku?: string;
  quantity: number;
  insuranceCode?: string;
  unitPrice: number;
  variablePrice: boolean;
};
export type ProductKitDto = {
  id: string;
  name: string;
  priceTable?: string;
  itemCount: number;
  totalUnitPrice: number;
};
export type ProductKitDetailDto = {
  id: string;
  name: string;
  priceTable?: string;
  items: ProductKitItemDto[];
};
export type ProductKitItemRequest = {
  productId: string;
  quantity: number;
  insuranceCode?: string;
  unitPrice: number;
  variablePrice: boolean;
};
export type CreateProductKitRequest = {
  name: string;
  priceTable?: string;
  items: ProductKitItemRequest[];
};
export type UpdateProductKitRequest = CreateProductKitRequest;
export type StockRequisitionItemDto = {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  productUnit: string;
  quantityOnHand: number;
  quantity: number;
  fulfilledQuantity: number;
  itemStatus: number;
  unitPrice: number;
  notes?: string;
};
export type StockRequisitionDto = {
  id: string;
  sequenceNumber: number;
  requestNumber: string;
  requestingSector: number;
  originLocation?: string;
  destinationLocation?: string;
  requestedBy: string;
  recipientName?: string;
  priority: number;
  dueDate?: string;
  status: number;
  requestedAt: string;
  itemCount: number;
  totalQuantity: number;
};
export type StockRequisitionDetailDto = {
  id: string;
  sequenceNumber: number;
  requestNumber: string;
  requestingSector: number;
  originLocation?: string;
  destinationLocation?: string;
  requestedBy: string;
  recipientName?: string;
  priority: number;
  dueDate?: string;
  notes?: string;
  status: number;
  requestedAt: string;
  items: StockRequisitionItemDto[];
};
export type StockRequisitionItemRequest = {
  productId: string;
  quantity: number;
  itemStatus: number;
  unitPrice: number;
  notes?: string;
};
export type CreateStockRequisitionRequest = {
  requestedBy: string;
  recipientName?: string;
  priority: number;
  dueDate?: string;
  destinationLocation?: string;
  notes?: string;
  items: StockRequisitionItemRequest[];
};
export type UpdateStockRequisitionRequest = CreateStockRequisitionRequest;
export type DenyStockRequisitionRequest = { reason: string };

export type ClinicalAlertDto = {
  code: string;
  severity: string;
  title: string;
  message: string;
  ruleId?: string;
};

export type PatientClinicalAlertsDto = {
  patientId: string;
  patientName: string;
  alerts: ClinicalAlertDto[];
};

export type StockReplenishmentSuggestionDto = {
  productId: string;
  productName: string;
  sku: string;
  quantityOnHand: number;
  minimumStock: number;
  avgDailyConsumption?: number;
  daysUntilStockout?: number;
  recommendation: string;
};

export type OperationalInsightDto = {
  code: string;
  label: string;
  value: string;
  severity?: string;
};

export type OperationalInsightsDto = {
  insights: OperationalInsightDto[];
  generatedAt: string;
};

export type PrescriptionSafetyResultDto = {
  isSafe: boolean;
  alerts: ClinicalAlertDto[];
  summary: string;
};

export type WarehouseDashboardDto = {
  totalProducts: number;
  lowStockCount: number;
  expiringLotsCount: number;
  todayInboundQuantity: number;
  todayOutboundQuantity: number;
  pendingRequisitions: number;
};

export type ProductLotDto = {
  id: string;
  productId: string;
  productName: string;
  productSku: string;
  batchNumber: string;
  expiryDate?: string;
  manufacturer?: string;
  quantityOnHand: number;
  locationName?: string;
  unitCost?: number;
  isExpiringSoon: boolean;
  createdAt: string;
};

export type StockReceiptItemRequest = {
  productId: string;
  batchNumber: string;
  expiryDate?: string;
  quantity: number;
  unitPrice: number;
  manufacturer?: string;
  locationName?: string;
  ncm?: string;
  cfop?: string;
};

export type CreateStockReceiptRequest = {
  supplierName: string;
  supplierCnpj?: string;
  invoiceNumber?: string;
  invoiceSeries?: string;
  invoiceIssueDate?: string;
  nfeAccessKey?: string;
  receivedAt?: string;
  freightAmount?: number;
  discountAmount?: number;
  paymentCondition?: string;
  notes?: string;
  receivedByUserName?: string;
  items: StockReceiptItemRequest[];
};

export type StockReceiptItemDto = {
  id: string;
  productId: string;
  productName: string;
  productLotId?: string;
  batchNumber: string;
  expiryDate?: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  ncm?: string;
  cfop?: string;
};

export type StockReceiptDto = {
  id: string;
  supplierName: string;
  supplierCnpj?: string;
  invoiceNumber?: string;
  invoiceSeries?: string;
  invoiceIssueDate?: string;
  nfeAccessKey?: string;
  receivedAt: string;
  totalAmount: number;
  freightAmount: number;
  discountAmount: number;
  paymentCondition?: string;
  notes?: string;
  receivedByUserName?: string;
  items: StockReceiptItemDto[];
};

export type StockIssueItemRequest = {
  productId: string;
  quantity: number;
  productLotId?: string;
};

export type CreateStockIssueRequest = {
  sectorName: string;
  responsibleName: string;
  issueType: number;
  patientId?: string;
  hospitalizationId?: string;
  notes?: string;
  userName?: string;
  items: StockIssueItemRequest[];
};

export type StockIssueItemDto = {
  id: string;
  productId: string;
  productName: string;
  productLotId?: string;
  batchNumber?: string;
  quantity: number;
};

export type StockIssueDto = {
  id: string;
  sectorName: string;
  responsibleName: string;
  issueType: number;
  patientId?: string;
  hospitalizationId?: string;
  notes?: string;
  createdAt: string;
  items: StockIssueItemDto[];
};

export type SectorConsumptionDto = {
  sectorName: string;
  totalQuantity: number;
  movementCount: number;
};
export const stockRequisitionStatusLabels: Record<number, string> = {
  1: 'Pendente',
  2: 'Aprovada',
  3: 'Atendida',
  4: 'Cancelada',
  5: 'Negada',
};
export const stockRequisitionPriorityLabels: Record<number, string> = {
  1: 'Baixíssima',
  2: 'Baixa',
  3: 'Normal',
  4: 'Alta',
  5: 'Urgente',
};
export type CreateProductRequest = {
  name: string; sku: string; type: number; unit: string; minimumStock: number; description?: string;
  presentation?: string; contentQuantity?: number; barcode?: string; category?: string;
  manufacturer?: string; defaultLocation?: string; tussCode?: string; maximumStock?: number;
  expiryWarningDays?: number; averagePurchasePrice?: number; averageSalePrice?: number;
  allowOutboundFromRegister?: boolean; entryLocations?: string; photoData?: string;
};
export type UpdateProductRequest = Omit<CreateProductRequest, 'sku'>;
export type DispenseMedicationRequest = {
  patientId: string; productId: string; quantity: number;
  professionalId?: string; hospitalizationId?: string; notes?: string;
};
export type PharmacyDispensingDto = {
  id: string; patientId: string; patientName: string; productId: string;
  productName: string; quantity: number; reversedQuantity: number; professionalName?: string;
  dispensedAt: string; notes?: string;
};
export type PharmacyDispensingReversalDto = {
  id: string; dispensingId: string; quantity: number; reason?: string; reversedAt: string;
};
export type ReserveBedRequest = { patientId: string; reason?: string; until?: string };
export type BlockBedRequest = { reason: string; until?: string };
export type ReleaseBedRequest = { reason?: string };
export type BedEventDto = {
  id: string; bedId: string; bedNumber: string; wardName: string;
  eventType: ApiEnum<1 | 2 | 3 | 4>; patientId?: string; patientName?: string;
  hospitalizationId?: string; reason?: string; startAt: string; endAt?: string;
};
export type AdministrationRouteDto = { code: string; name: string; abbreviation?: string };
export type PatientReferenceCatalogType = 1 | 2 | 3 | 4;
export type PatientReferenceCatalogItemDto = { code: string; name: string; catalogType: PatientReferenceCatalogType };

export type HospitalReferenceCatalogType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13 | 14 | 15 | 16;
export type HospitalReferenceCatalogItemDto = {
  code: string;
  name: string;
  catalogType: HospitalReferenceCatalogType;
  parentGroup?: string;
  displayOrder: number;
  description?: string;
  metadataJson?: string;
};
export type HospitalReferenceCatalogGroupDto = { parentGroup?: string; itemCount: number };
export type HospitalReferenceCatalogSummaryDto = {
  catalogType: HospitalReferenceCatalogType;
  label: string;
  itemCount: number;
  groupCount: number;
};
export type HospitalReferenceCatalogTypeInfoDto = {
  catalogType: HospitalReferenceCatalogType;
  label: string;
  description: string;
};
export const hospitalReferenceCatalogTypeLabels: Record<HospitalReferenceCatalogType, string> = {
  1: 'Tipos de usuário',
  2: 'Setores hospitalares',
  3: 'Alas',
  4: 'Tipos de leito',
  5: 'Tipos de fornecedor',
  6: 'Tipos de produto',
  7: 'Tipos de serviço',
  8: 'Especialidades médicas',
  9: 'Exames laboratoriais',
  10: 'Exames de imagem',
  11: 'Tipos de guia TISS',
  12: 'Módulos do menu',
  13: 'Ações de permissão',
  14: 'Perfis prontos',
  15: 'Bases regulatórias',
  16: 'Módulos recomendados',
};

export type TvWidgetType = 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9;
export type TvDisplayOrientation = 1 | 2;
export type TvDisplayStatus = 1 | 2;
export type TvMediaType = 1 | 2 | 3 | 4;

export type TvLayoutZoneDto = { id: string; widget: TvWidgetType; x: number; y: number; w: number; h: number };
export type TvLayoutDto = { id: string; name: string; description?: string; zones: TvLayoutZoneDto[]; isSystem: boolean };
export type TvDisplayDto = {
  id: string; name: string; slug: string; sector?: string; ipAddress?: string; resolution?: string;
  orientation: TvDisplayOrientation; status: TvDisplayStatus; playerToken: string; layoutId?: string;
  layoutName?: string; showPatientName: boolean; enableSound: boolean; callDisplaySeconds: number;
  weatherCity?: string; lastSeenAt?: string; playerUrl: string;
};
export type TvMediaDto = {
  id: string; title: string; mediaType: TvMediaType; url: string; mimeType?: string; sector?: string;
  startsAt?: string; endsAt?: string; priority: number; durationSeconds: number;
};
export type TvNewsDto = {
  id: string; title: string; summary?: string; imageUrl?: string; sector?: string;
  publishedAt: string; expiresAt?: string;
};
export type TvAnnouncementDto = {
  id: string; title: string; body: string; sector?: string; startsAt: string; endsAt?: string; priority: number;
};
export type TvQueueCallDto = {
  id: string; ticketNumber: string; patientName?: string; destination: string; sector?: string;
  calledAt: string; displaySeconds: number; showPatientName: boolean; isActive: boolean;
};
export type TvWeatherDto = {
  city: string; temperatureC: number; condition: string; icon?: string; humidityPercent: number; updatedAt: string;
};
export type TvDashboardWidgetDto = {
  attendancesToday: number; emergencyWaiting: number; averageEmergencyWaitMinutes: number;
  bedOccupancyRate: number; labOrdersPending: number;
};
export type TvScheduleItemDto = {
  name: string; roleOrSpecialty: string; shiftLabel: string; timeLabel?: string;
};
export type TvCampaignDto = {
  id: string; name: string; sector?: string; startsAt: string; endsAt?: string;
  dailyStart?: string; dailyEnd?: string; daysOfWeek?: string; priority: number; mediaIds: string[];
};
export type TvPlayerStateDto = {
  display: TvDisplayDto; layout: TvLayoutDto; media: TvMediaDto[]; news: TvNewsDto[];
  announcements: TvAnnouncementDto[]; recentCalls: TvQueueCallDto[]; activeCall?: TvQueueCallDto;
  weather?: TvWeatherDto; dashboard?: TvDashboardWidgetDto; schedule: TvScheduleItemDto[];
  speechProvider: string; activeCallSpeechUrl?: string; generatedAt: string;
};
export type TvMonitorSummaryDto = {
  totalDisplays: number; onlineDisplays: number; offlineDisplays: number; callsToday: number;
  activeMedia: number; displays: TvDisplayDto[];
};
export type CreateTvDisplayRequest = {
  name: string; slug: string; sector?: string; layoutId?: string; orientation: TvDisplayOrientation;
  resolution?: string; weatherCity?: string; showPatientName: boolean; enableSound: boolean; callDisplaySeconds: number;
};
export type UpdateTvDisplayRequest = Omit<CreateTvDisplayRequest, 'slug'>;
export type CreateTvNewsRequest = { title: string; summary?: string; imageUrl?: string; sector?: string; expiresAt?: string };
export type CreateTvAnnouncementRequest = {
  title: string; body: string; sector?: string; startsAt?: string; endsAt?: string; priority: number;
};
export type CallTvQueueRequest = {
  ticketNumber: string; patientName?: string; destination: string; sector?: string;
  displayId?: string; showPatientName?: boolean;
};
export type CallKioskTicketRequest = {
  destination: string; displayId?: string; showPatientName?: boolean;
};
export type CreateTvLayoutRequest = { name: string; description?: string; zones: TvLayoutZoneDto[] };
export type UpdateTvLayoutRequest = CreateTvLayoutRequest;
export type CreateTvCampaignRequest = {
  name: string; sector?: string; startsAt?: string; endsAt?: string;
  dailyStart?: string; dailyEnd?: string; daysOfWeek?: string; priority: number; mediaIds: string[];
};
export type UpdateTvCampaignRequest = CreateTvCampaignRequest;
export type TvHeartbeatRequest = { ipAddress?: string; resolution?: string };

export function resolveTvSpeechUrl(path: string): string {
  if (path.startsWith('http')) return path;
  const base = API_BASE.replace(/\/api\/?$/, '');
  return `${base}${path.startsWith('/') ? path : `/${path}`}`;
}

export const tvWidgetTypeLabels: Record<TvWidgetType, string> = {
  1: 'Mídia', 2: 'Chamadas', 3: 'Notícias', 4: 'Clima', 5: 'Relógio', 6: 'Indicadores', 7: 'Avisos', 8: 'Mural', 9: 'Escalas',
};

export const bedStatusLabels: Record<number, string> = {
  1: 'Disponível', 2: 'Ocupado', 3: 'Manutenção', 4: 'Higienização', 5: 'Reservado',
};
export const hospitalizationStatusLabels: Record<number, string> = {
  1: 'Internado', 2: 'Alta', 3: 'Transferido',
};
export const susHospitalizationCharacterLabels: Record<number, string> = {
  1: 'Eletiva', 2: 'Urgência', 3: 'Emergência',
};
export const susHospitalizationModalityLabels: Record<number, string> = {
  1: 'Clínica', 2: 'Cirúrgica', 3: 'Obstétrica', 4: 'Pediátrica', 5: 'Psiquiátrica',
};
const susCharacterByName: Record<string, number> = {
  Eletiva: 1, Urgência: 2, Emergência: 3,
  Elective: 1, Urgent: 2, Emergency: 3,
};
const susModalityByName: Record<string, number> = {
  Clínica: 1, Cirúrgica: 2, Obstétrica: 3, Pediátrica: 4, Psiquiátrica: 5,
  Clinical: 1, Surgical: 2, Obstetric: 3, Pediatric: 4, Psychiatric: 5,
};
export function susCharacterValue(v: ApiEnum<1 | 2 | 3> | undefined): number {
  if (v == null) return 0;
  if (typeof v === 'number') return v;
  return susCharacterByName[v] ?? 0;
}
export function susModalityValue(v: ApiEnum<1 | 2 | 3 | 4 | 5> | undefined): number {
  if (v == null) return 0;
  if (typeof v === 'number') return v;
  return susModalityByName[v] ?? 0;
}
export function isSusHospitalization(h: HospitalizationDto): boolean {
  const ward = wardModalityValue(h.wardCoverageModality);
  return ward === 3 || ward === 4;
}
export const hospitalizationRequestStatusLabels: Record<number, string> = {
  1: 'Pendente', 2: 'Aprovada', 3: 'Rejeitada', 4: 'Internado', 5: 'Cancelada',
};
export const hospitalizationRequestPriorityLabels: Record<number, string> = {
  1: 'Eletiva', 2: 'Urgente', 3: 'Emergência',
};
const hospitalizationRequestStatusByName: Record<string, number> = {
  Pendente: 1, Aprovada: 2, Rejeitada: 3, Internado: 4, Cancelada: 5,
  Pending: 1, Approved: 2, Rejected: 3, Admitted: 4, Cancelled: 5,
};
const hospitalizationRequestPriorityByName: Record<string, number> = {
  Eletiva: 1, Urgente: 2, Emergência: 3,
  Elective: 1, Urgent: 2, Emergency: 3,
};
export function hospitalizationRequestStatusValue(status: ApiEnum<1 | 2 | 3 | 4 | 5>): number {
  if (typeof status === 'number') return status;
  return hospitalizationRequestStatusByName[status] ?? 0;
}
export function hospitalizationRequestPriorityValue(priority: ApiEnum<1 | 2 | 3>): number {
  if (typeof priority === 'number') return priority;
  return hospitalizationRequestPriorityByName[priority] ?? 0;
}
export function hospitalizationRequestStatusLabel(status: ApiEnum<1 | 2 | 3 | 4 | 5>): string {
  return hospitalizationRequestStatusLabels[hospitalizationRequestStatusValue(status)] ?? '—';
}
export function hospitalizationRequestPriorityLabel(priority: ApiEnum<1 | 2 | 3>): string {
  return hospitalizationRequestPriorityLabels[hospitalizationRequestPriorityValue(priority)] ?? '—';
}
export const surgeryStatusLabels: Record<number, string> = {
  1: 'Agendada', 2: 'Em andamento', 3: 'Concluída', 4: 'Cancelada',
};
export const operatingRoomStatusLabels: Record<number, string> = {
  1: 'Disponível', 2: 'Em uso', 3: 'Manutenção',
};
export const productTypeLabels: Record<number, string> = {
  1: 'Medicamento', 2: 'Material', 3: 'Geral', 4: 'Produto',
};
export const stockMovementTypeLabels: Record<number, string> = {
  1: 'Entrada', 2: 'Saída', 3: 'Ajuste',
};

export type HealthInsuranceDto = {
  id: string;
  name: string;
  ansRegistration?: string;
  cnpj?: string;
  logoUrl?: string;
  websiteUrl?: string;
  isActive: boolean;
};
export type LabExamCatalogDto = {
  id: string; name: string; tussCode?: string; sampleType?: string;
  referenceRange?: string; unit?: string; category?: string; isGeneral: boolean;
};

export type ImagingProcedureDto = {
  id: string; name: string; tussCode?: string; modality: number;
  bodyPart?: string; description?: string; isGeneral: boolean;
};

export type MedicationCatalogDto = {
  id: string; name: string; activeIngredient?: string; pharmaceuticalForm?: string;
  strength?: string; defaultDosage?: string; route?: string; notes?: string;
  packageInsert?: string;
  isGeneral: boolean; productId?: string; stockAvailable?: number;
};

export type AnvisaBulaSummary = {
  numProcesso: string;
  nomeProduto: string;
  razaoSocial?: string;
  principioAtivo?: string;
  numeroRegistro?: string;
  idBulaPacienteProtegido?: string;
  idBulaProfissionalProtegido?: string;
  categoriaRegulatoria?: string;
};

export type AnvisaBulaSearchResponse = {
  content: AnvisaBulaSummary[];
  totalElements?: number;
  totalPages?: number;
  number?: number;
};

export type AnvisaMedicationDetailDto = AnvisaBulaSummary & Record<string, unknown>;

export type BularioMedicationListItemDto = {
  id: string;
  name: string;
  activeIngredient?: string;
  source?: string;
  hasPackageInsert: boolean;
};

export type BularioSearchResultDto = {
  items: BularioMedicationListItemDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  catalogTotal: number;
  anvisaAvailable: boolean;
  anvisa?: AnvisaBulaSearchResponse | null;
};

export type BularioStatsDto = {
  catalogTotal: number;
  withPackageInsert: number;
  anvisaAvailable: boolean;
};

export type Cid10CatalogItemDto = {
  code: string;
  description: string;
  category?: string;
  parentCode?: string;
};

export type SpecialtyClinicalCatalogDto = {
  specialtyId?: string;
  specialtyName?: string;
  labExams: LabExamCatalogDto[];
  imagingProcedures: ImagingProcedureDto[];
  medications: MedicationCatalogDto[];
};
export type LabOrderDto = {
  id: string; patientId: string; patientName: string; requestingProfessionalName: string;
  status: number; createdAt: string; items: LabOrderItemDto[];
};
export type LabOrderItemDto = { id: string; examCatalogId: string; examName: string; status: number; result?: LabResultDto };
export type LabResultDto = { id: string; value: string; unit?: string; referenceRange?: string; isAbnormal: boolean; releasedAt?: string };
export type CreateLabOrderRequest = { patientId: string; requestingProfessionalId: string; examCatalogIds: string[]; notes?: string };
export type RegisterLabResultRequest = { orderItemId: string; value: string; unit?: string; referenceRange?: string; isAbnormal: boolean; notes?: string };
export type ImagingStudyDto = {
  id: string; patientId: string; patientName: string; requestingProfessionalName: string;
  modality: number; studyDescription: string; status: number; scheduledAt: string;
  completedAt?: string; reportContent?: string; reportedAt?: string; accessionNumber?: string;
};
export type CreateImagingStudyRequest = { patientId: string; requestingProfessionalId: string; modality: number; studyDescription: string; scheduledAt: string };
export type RegisterImagingReportRequest = { reportContent: string; reportingProfessionalId?: string };
export type DepartmentDto = { id: string; name: string; description?: string; employeeCount: number };
export type EmployeeDto = {
  id: string;
  fullName: string;
  email?: string;
  jobTitle?: string;
  role: number;
  departmentName: string;
  hireDate: string;
  baseSalary: number;
  hasPhoto: boolean;
};

export type EmployeeDetailDto = {
  id: string;
  fullName: string;
  socialName?: string;
  cpf?: string;
  rg?: string;
  birthDate?: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  jobTitle?: string;
  role: number;
  departmentId: string;
  departmentName: string;
  hireDate: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  notes?: string;
  photoData?: string;
  baseSalary: number;
  isActive: boolean;
  createdAt: string;
};

export type CreateEmployeeRequest = {
  fullName: string;
  socialName?: string;
  cpf?: string;
  rg?: string;
  birthDate?: string;
  gender: number;
  email?: string;
  phone?: string;
  mobilePhone?: string;
  jobTitle?: string;
  role: number;
  departmentId: string;
  hireDate: string;
  addressStreet?: string;
  addressNumber?: string;
  addressComplement?: string;
  addressNeighborhood?: string;
  addressCity?: string;
  addressState?: string;
  addressZipCode?: string;
  emergencyContactName?: string;
  emergencyContactPhone?: string;
  emergencyContactRelationship?: string;
  notes?: string;
  photoData?: string;
  baseSalary?: number;
};

export type UpdateEmployeeRequest = CreateEmployeeRequest & { isActive: boolean };
export type EmployeeShiftDto = { id: string; employeeId: string; employeeName: string; departmentName: string; shiftDate: string; shiftType: number };
export type EmployeeHrEventDto = {
  id: string;
  employeeId: string;
  employeeName: string;
  eventType: number;
  title: string;
  detail: string;
  startDate: string;
  endDate?: string;
  notes?: string;
};
export type CreateEmployeeHrEventRequest = {
  employeeId: string;
  eventType: number;
  title: string;
  detail: string;
  startDate: string;
  endDate?: string;
  notes?: string;
};
export type HrDashboardDto = {
  activeEmployees: number;
  shiftsThisWeek: number;
  nightShiftsThisWeek: number;
  onVacationToday: number;
  trainingsThisMonth: number;
  reviewsThisQuarter: number;
  latestPayrollNet?: number;
  payrollEmployeeCount: number;
};
export type CreateShiftRequest = { employeeId: string; departmentId: string; shiftDate: string; shiftType: number };
export type DashboardHourlyStatDto = { hour: number; count: number };

export type DashboardAlertDto = {
  code: string;
  severity: 'critical' | 'warning' | 'info' | string;
  title: string;
  message: string;
  linkPath?: string;
};

export type DashboardFinancialMonthlyPointDto = {
  monthLabel: string;
  revenue: number;
  expense: number;
};

export type DashboardWeeklyCalendarItemDto = {
  appointmentId: string;
  scheduledAt: string;
  patientName: string;
  professionalName: string;
  specialtyName: string;
};

export type DashboardDepartmentRevenueDto = {
  departmentCode: string;
  departmentLabel: string;
  amount: number;
};

export type OperationalDashboardDto = {
  totalPatients: number;
  appointmentsToday: number;
  appointmentsPendingToday: number;
  activeHospitalizations: number;
  surgeriesToday: number;
  occupiedBeds: number;
  totalBeds: number;
  bedOccupancyRate: number;
  emergencyWaiting: number;
  emergencyInCare: number;
  emergencyCritical: number;
  triageToday: number;
  triageEmergencyToday: number;
  labOrdersPending: number;
  imagingStudiesPending: number;
  revenueThisMonth: number;
  revenuePending: number;
  financialAccountsOpen: number;
  payablePending: number;
  expenseThisMonth: number;
  payableAccountsOpen: number;
  overdueReceivable: number;
  overduePayable: number;
  lowStockProducts: number;
  parkingOccupied: number;
  parkingAwaitingPayment: number;
  visitorsInside: number;
  openSecurityIncidents: number;
  unreadNotifications: number;
  appointmentsTodayList: DashboardAppointmentItemDto[];
  revenueExpenseMonthly: DashboardFinancialMonthlyPointDto[];
  weeklyCalendar: DashboardWeeklyCalendarItemDto[];
  departmentRevenue: DashboardDepartmentRevenueDto[];
  emergencyQueue: DashboardEmergencyItemDto[];
  monthlyAppointments: BiMonthlyStatDto[];
  labOrdersByStatus: BiStatusCountDto[];
  monthBirthdays: DashboardBirthdayEmployeeDto[];
  revenueToday: number;
  availableBeds: number;
  cleaningBeds: number;
  maintenanceBeds: number;
  attendancesToday: number;
  averageEmergencyWaitMinutes: number;
  emergencySlaViolations: number;
  integrationFailures: number;
  hourlyAttendances: DashboardHourlyStatDto[];
  productionBySpecialty: BiStatusCountDto[];
  alerts: DashboardAlertDto[];
  appointmentStatusBreakdown: BiStatusCountDto[];
  generatedAt: string;
};

export type TpaAdministratorDto = {
  id: string;
  name: string;
  cnpj?: string;
  contactName?: string;
  contactEmail?: string;
  commissionPercent: number;
  discountPercent: number;
  claimsCount: number;
};

export type CreateTpaAdministratorRequest = {
  name: string;
  cnpj?: string;
  contactName?: string;
  contactEmail?: string;
  commissionPercent: number;
  discountPercent: number;
};

export type TpaClaimDto = {
  id: string;
  tpaAdministratorId: string;
  tpaAdministratorName: string;
  patientId: string;
  patientName: string;
  healthInsuranceId?: string;
  healthInsuranceName?: string;
  serviceDate: string;
  grossAmount: number;
  commissionAmount: number;
  discountAmount: number;
  netAmount: number;
  status: number;
  notes?: string;
  financialAccountId?: string;
};

export type CreateTpaClaimRequest = {
  tpaAdministratorId: string;
  patientId: string;
  healthInsuranceId?: string;
  serviceDate: string;
  grossAmount: number;
  commissionPercent?: number;
  discountPercent?: number;
  notes?: string;
};

export type UpdateTpaClaimStatusRequest = {
  status: number;
  createFinancialAccountWhenPaid?: boolean;
};

export type TpaReportDto = {
  totalClaims: number;
  grossTotal: number;
  netTotal: number;
  latestClaims: TpaClaimDto[];
};

export type PayrollItemLineDto = {
  id: string;
  lineType: number;
  code: string;
  description: string;
  amount: number;
};

export type PayrollItemDto = {
  id: string;
  employeeId: string;
  employeeName: string;
  jobTitle?: string;
  departmentName: string;
  baseSalary: number;
  overtimeAmount: number;
  benefitsAmount: number;
  discountAmount: number;
  grossAmount: number;
  netAmount: number;
  fgtsEmployerAmount: number;
  financialAccountId?: string;
  lines: PayrollItemLineDto[];
};

export type PayrollRunDto = {
  id: string;
  year: number;
  month: number;
  referenceDate: string;
  status: number;
  totalGross: number;
  totalDiscounts: number;
  totalNet: number;
  totalFgtsEmployer: number;
  generatedAt?: string;
  approvedAt?: string;
  paidAt?: string;
  notes?: string;
  consolidatedFinancialAccountId?: string;
  items: PayrollItemDto[];
};

export type GeneratePayrollRunRequest = {
  year: number;
  month: number;
  defaultBaseSalary?: number;
  valeRefeicao: number;
  valeTransportePercent: number;
  healthPlanDiscount: number;
  dependentCount: number;
  notes?: string;
};

export type UpdatePayrollRunStatusRequest = {
  status: number;
  createFinancialAccountsWhenPaid?: boolean;
};

export type PayrollItemLineInputDto = {
  lineType: number;
  code: string;
  description: string;
  amount: number;
};

export type UpdatePayrollItemLinesRequest = {
  lines: PayrollItemLineInputDto[];
};

export type PayrollSlipDto = {
  payrollRunId: string;
  year: number;
  month: number;
  referenceDate: string;
  status: number;
  item: PayrollItemDto;
  totalFgtsEmployer: number;
  earnings: PayrollItemLineDto[];
  discounts: PayrollItemLineDto[];
};

export type PayrollDepartmentSummaryDto = {
  departmentName: string;
  employeeCount: number;
  totalGross: number;
  totalNet: number;
};

export type PayrollMonthlySummaryDto = {
  year: number;
  month: number;
  status?: number;
  runId?: string;
  employeeCount: number;
  totalGross: number;
  totalDiscounts: number;
  totalNet: number;
  totalFgtsEmployer: number;
  employeesOnVacation: number;
  nightShiftsInMonth: number;
  byDepartment: PayrollDepartmentSummaryDto[];
};

export type PharmacyBillingEntryDto = {
  id: string;
  dispensingId: string;
  dispensedAt: string;
  patientName: string;
  productName: string;
  quantity: number;
  payerType: number;
  healthInsuranceId?: string;
  healthInsuranceName?: string;
  unitPrice: number;
  totalAmount: number;
  paid: boolean;
  paidAt?: string;
  financialAccountId?: string;
  notes?: string;
};

export type CreatePharmacyBillingEntryRequest = {
  dispensingId: string;
  payerType: number;
  healthInsuranceId?: string;
  unitPrice: number;
  paid: boolean;
  notes?: string;
  createFinancialAccountWhenPaid?: boolean;
};

export type BirthRegistrationDto = {
  id: string;
  motherPatientId: string;
  motherName: string;
  babyName: string;
  birthAt: string;
  weightKg: number;
  heightCm: number;
  notes?: string;
};

export type CreateBirthRegistrationRequest = {
  motherPatientId: string;
  babyName: string;
  birthAt: string;
  weightKg: number;
  heightCm: number;
  notes?: string;
};

export type BillingAlertDto = {
  code: string;
  severity: string;
  title: string;
  message: string;
  linkPath?: string;
};

export type BillingDashboardDto = {
  openAccountsCount: number;
  openAccountsAmount: number;
  receivableOpen: number;
  receivedThisMonth: number;
  tissGuidesDraft: number;
  tissGuidesSent: number;
  tissGuidesPaid: number;
  tissGuidesGlosa: number;
  totalBilled: number;
  totalPaid: number;
  totalGlosaOpen: number;
  glosaRatePercent: number;
  guidesPendingOver30Days: number;
  activeSusHospitalizations: number;
  aihReadyCount: number;
  susExportsThisMonth: number;
  alerts: BillingAlertDto[];
  generatedAt: string;
};

export type OfficialSourceStatusDto = {
  sourceType: string;
  displayName: string;
  currentVersion: string;
  availableVersion?: string | null;
  status: string;
  sourceUrl?: string | null;
  notes?: string | null;
  lastCheckedAt?: string | null;
  lastImportedAt?: string | null;
  installedRecordCount?: number | null;
  canAutoImport: boolean;
};

export type OfficialUpdatesDashboardDto = {
  lastCheckAt?: string | null;
  sources: OfficialSourceStatusDto[];
};

export type IntegrationLogDto = {
  id: string;
  sourceType: string;
  action: string;
  status: string;
  message: string;
  triggeredBy?: string | null;
  durationMs?: number | null;
  createdAt: string;
};

export type OfficialUpdateActionResultDto = {
  sourceType: string;
  status: string;
  message: string;
  importedCount?: number | null;
};

export type BusinessRuleDto = {
  code: string;
  module: string;
  title: string;
  description: string;
  implemented: boolean;
  brReference?: string | null;
  layer?: string | null;
};

export type DashboardBirthdayEmployeeDto = {
  id: string;
  fullName: string;
  birthDate: string;
  photoData?: string;
  jobTitle?: string;
  departmentName: string;
};
export type DashboardAppointmentItemDto = {
  id: string;
  scheduledAt: string;
  patientName: string;
  professionalName: string;
  specialtyName: string;
  status: string;
};
export type DashboardEmergencyItemDto = {
  id: string;
  patientName: string;
  chiefComplaint: string;
  urgency: string;
  status: string;
  arrivedAt: string;
};

export type ReportCatalogItemDto = {
  code: string;
  name: string;
  module: string;
  moduleLabel: string;
  description: string;
  isEssential: boolean;
  isImplemented: boolean;
  phase: number;
};

export type ReportModuleSummaryDto = {
  module: string;
  label: string;
  total: number;
  essential: number;
  implemented: number;
};

export type ReportCatalogSummaryDto = {
  totalReports: number;
  essentialReports: number;
  implementedReports: number;
  modules: ReportModuleSummaryDto[];
};

export type ReportFilterParams = {
  dateFrom?: string;
  dateTo?: string;
  professionalId?: string;
  specialtyId?: string;
  healthInsuranceId?: string;
  patientId?: string;
  tpaAdministratorId?: string;
  year?: number;
  month?: number;
  department?: string;
};

export type ReportColumnDto = { key: string; label: string; format?: string };
export type ReportKpiDto = { label: string; value: string; variant?: string };
export type ReportResultDto = {
  code: string;
  title: string;
  subtitle?: string;
  isImplemented: boolean;
  columns: ReportColumnDto[];
  rows: Record<string, unknown>[];
  kpis: ReportKpiDto[];
  generatedAt: string;
};

export type BiDashboardDto = {
  totalPatients: number;
  activeHospitalizations: number;
  appointmentsToday: number;
  surgeriesToday: number;
  labOrdersPending: number;
  imagingStudiesPending: number;
  revenueThisMonth: number;
  revenueLastMonth: number;
  revenueGrowthPercent: number;
  revenuePending: number;
  bedOccupancyRate: number;
  occupiedBeds: number;
  totalBeds: number;
  emergencyWaiting: number;
  emergencyInCare: number;
  financialAccountsOpen: number;
  lowStockProducts: number;
  purchaseOrdersPending: number;
  tissGuidesPending: number;
  tissAmountPending: number;
  monthlyAppointments: BiMonthlyStatDto[];
  monthlyRevenue: BiMonthlyStatDto[];
  monthlyExpenses: BiMonthlyStatDto[];
  monthlyHospitalizations: BiMonthlyStatDto[];
  averageLengthOfStayDays: number;
  dischargesThisMonth: number;
  bedTurnoverRate: number;
  monthlyBedTurnover: number;
  expenseThisMonth: number;
  expenseLastMonth: number;
  expenseGrowthPercent: number;
  overdueReceivable: number;
  overdueReceivableCount: number;
  defaultRatePercent: number;
  medicalProductionThisMonth: number;
  hospitalProductionThisMonth: number;
  revenueByCategory: BiCategoryStatDto[];
  tissGuidesByStatus: BiStatusCountDto[];
  labOrdersByStatus: BiStatusCountDto[];
  financialAccountsByStatus: BiStatusCountDto[];
  imagingByStatus: BiStatusCountDto[];
  emergencyByUrgency: BiStatusCountDto[];
  wardOccupancy: BiWardOccupancyDto[];
  topSpecialties: BiSpecialtyStatDto[];
  lowStockItems: BiLowStockItemDto[];
  generatedAt: string;
};
export type BiMonthlyStatDto = { label: string; count: number; amount?: number };
export type BiStatusCountDto = { label: string; count: number; amount?: number };
export type BiCategoryStatDto = { label: string; amount: number; count: number };
export type BiWardOccupancyDto = { wardName: string; totalBeds: number; occupiedBeds: number; occupancyRate: number };
export type BiSpecialtyStatDto = { specialtyName: string; appointmentsThisMonth: number };
export type BiLowStockItemDto = { productName: string; sku: string; onHand: number; minimum: number; unit: string };

export type MimicSampleQueryDto = {
  id: string;
  title: string;
  description: string;
  sql: string;
};

export type MimicEtlStatusDto = {
  stagingSchemaReady: boolean;
  rawVitalRows: number;
  snapshotRows: number;
  lastRunId: number | null;
  lastRunStatus: string | null;
  lastRunStartedAt: string | null;
  lastRunCompletedAt: string | null;
  lastRunRowsProcessed: number | null;
  lastRunError: string | null;
  importInProgress: boolean;
  currentPhase: string | null;
  currentRowsProcessed: number | null;
};

export type MimicVitalSignDto = {
  id: number;
  subjectId: number;
  hadmId: number | null;
  icuStayId: number | null;
  recordedAt: string;
  heartRate: number | null;
  systolicBp: number | null;
  diastolicBp: number | null;
  spO2: number | null;
  respiratoryRate: number | null;
  temperatureC: number | null;
  source: string;
};

export type MimicVitalsQueryResultDto = {
  subjectId: number;
  count: number;
  displayLabel: string;
  warning: string;
  records: MimicVitalSignDto[];
};

export type MimicEtlTriggerResultDto = {
  accepted: boolean;
  message: string;
  runId: number | null;
};

export type MimicResearchStatusDto = {
  enabled: boolean;
  databaseConfigured: boolean;
  databaseReachable: boolean;
  displayLabel: string;
  warning: string;
  sampleQueries: MimicSampleQueryDto[];
  etl: MimicEtlStatusDto | null;
};

export type TissGuideClinicalDto = {
  cid10Code?: string;
  cid10Secondary?: string;
  clinicalJustification?: string;
  serviceCharacter?: number;
  accidentIndicator?: number;
  requestingProfessionalId?: string;
  requestingProfessionalName?: string;
  requestingProfessionalCrm?: string;
  executingProfessionalId?: string;
  executingProfessionalName?: string;
  executingProfessionalCrm?: string;
  admissionDate?: string;
  dischargeDate?: string;
  requestedBedType?: string;
  parentGuideId?: string;
  professionalRole?: number;
  participationPercent?: number;
  surgeryId?: string;
};
export type TissGuideClinicalRequest = TissGuideClinicalDto;
export type TissGuideDto = {
  id: string;
  guideNumber: string;
  patientId: string;
  patientName: string;
  healthInsuranceId: string;
  healthInsuranceName: string;
  serviceUnitId?: string;
  serviceUnitName?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  guideType: number;
  status: number;
  totalAmount: number;
  sentAt?: string;
  accountClosedAt?: string;
  notes?: string;
  beneficiaryCardNumber?: string;
  beneficiaryPlanName?: string;
  beneficiaryCns?: string;
  authorizationPassword?: string;
  clinical: TissGuideClinicalDto;
  createdAt: string;
  items: TissGuideItemDto[];
  glosas: TissGlosaDto[];
};
export type TissGuideItemDto = {
  id: string;
  tussCode: string;
  description: string;
  quantity: number;
  unitPrice: number;
  total: number;
  priceTableSource?: number;
  cid10Code?: string;
  relatedTussCode?: string;
  isAudited: boolean;
};
export type TissGlosaDto = {
  id: string; tissGuideItemId?: string; reason: string; ansGlosaCode?: string; glosaAmount: number;
  isResolved: boolean; contestationStatus: number; contestationNotes?: string; itemDescription?: string;
};
export type TissGuideItemRequest = {
  tussCode: string;
  description: string;
  quantity: number;
  unitPrice: number;
  priceTableSource?: number;
  cid10Code?: string;
  relatedTussCode?: string;
};
export type UpdateTissGuideItemRequest = TissGuideItemRequest & { id?: string };
export type CreateTissGuideRequest = {
  patientId: string;
  healthInsuranceId: string;
  serviceUnitId?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  guideType: number;
  items: TissGuideItemRequest[];
  notes?: string;
  clinical?: TissGuideClinicalRequest;
  clientRequestId?: string;
};
export type UpdateTissGuideRequest = {
  healthInsuranceId: string;
  serviceUnitId?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  guideType: number;
  notes?: string;
  clinical?: TissGuideClinicalRequest;
  items: UpdateTissGuideItemRequest[];
};
export type TissGuidePrefillDto = {
  healthInsuranceId?: string;
  healthInsuranceName?: string;
  beneficiaryCardNumber?: string;
  beneficiaryPlanName?: string;
  beneficiaryCns?: string;
  beneficiaryAccommodation?: string;
  authorizationPassword?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  surgeryId?: string;
  cid10Code?: string;
  cid10Description?: string;
  requestingProfessionalId?: string;
  requestingProfessionalName?: string;
  requestingProfessionalCrm?: string;
  executingProfessionalId?: string;
  executingProfessionalName?: string;
  admissionDate?: string;
  dischargeDate?: string;
  requestedBedType?: string;
  suggestedGuideType?: number;
  suggestedItems: TissGuideItemRequest[];
  operatorEligibilityStatus?: number;
  operatorEligibilityCheckedAt?: string;
  cardValidUntil?: string;
  operatorMessage?: string;
  operatorCoverageSummary?: string;
  operatorDataSource?: 'cache' | 'live';
};
export type GuidePrefillRequest = {
  patientId: string;
  guideType: number;
  healthInsuranceId?: string;
  includeOperatorData?: boolean;
  refreshOperatorData?: boolean;
  appointmentId?: string;
  hospitalizationId?: string;
  chemotherapySessionId?: string;
  surgeryId?: string;
  labOrderId?: string;
  imagingStudyId?: string;
};
export type TissClinicalSourceDto = {
  id: string;
  documentKind: number;
  patientId: string;
  patientName: string;
  healthInsuranceId?: string;
  healthInsuranceName?: string;
  guideType: number;
  reportCode?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  chemotherapySessionId?: string;
  surgeryId?: string;
  labOrderId?: string;
  imagingStudyId?: string;
  label: string;
  formDataJson: string;
  generatedTissGuideId?: string;
  generatedGuideNumber?: string;
  generatedArtifactJson?: string;
  generatedAt?: string;
  createdAt: string;
  updatedAt?: string;
};
export type UpsertTissClinicalSourceRequest = {
  documentKind: number;
  patientId: string;
  guideType: number;
  reportCode?: string;
  healthInsuranceId?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  chemotherapySessionId?: string;
  surgeryId?: string;
  labOrderId?: string;
  imagingStudyId?: string;
  label: string;
  formDataJson: string;
};
export type ClinicalSourceLookupRequest = {
  documentKind: number;
  patientId: string;
  guideType: number;
  reportCode?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  chemotherapySessionId?: string;
  surgeryId?: string;
  labOrderId?: string;
  imagingStudyId?: string;
};
export type ProcedureLookupDto = {
  code: string;
  description: string;
  source: string;
  referencePrice?: number;
  suggestedGuideType?: number;
  tussTableType?: number;
};
export type BillingCatalogSummaryDto = {
  tussCount: number;
  cbhpmCount: number;
  brasindiceCount: number;
  simproCount: number;
  cid10Count: number;
};
export type RegisterGlosaRequest = { tissGuideItemId?: string; reason: string; ansGlosaCode?: string; glosaAmount: number };
export type UpdateGlosaRequest = { tissGuideItemId?: string; reason: string; ansGlosaCode?: string; glosaAmount: number };
export type ContestGlosaRequest = { ansGlosaCode?: string; contestationNotes: string };
export type TussSearchResultDto = { tussCode: string; description: string; source: string; suggestedPrice?: number };
export type SuggestedGuideItemsRequest = {
  patientId: string;
  hospitalizationId?: string;
  appointmentId?: string;
  guideType?: number;
  surgeryId?: string;
};
export type EligibilityCheckRequest = { patientId: string; healthInsuranceId: string; cardNumber?: string };
export type EligibilityCheckDto = {
  id: string; patientId: string; patientName: string; healthInsuranceId: string; healthInsuranceName: string;
  cardNumber: string; status: number; planName?: string; coverageSummary?: string; validUntil?: string;
  responseMessage?: string; createdAt: string;
};
export type CreateAuthorizationRequest = {
  patientId: string; healthInsuranceId: string; authorizationType: number; authorizationNumber: string;
  validFrom?: string; validUntil?: string; procedureSummary?: string; tissGuideId?: string; notes?: string;
};
export type RequestOnlineAuthorizationRequest = {
  patientId: string;
  healthInsuranceId: string;
  authorizationType: number;
  procedureSummary?: string;
  tissGuideId?: string;
  notes?: string;
  validFrom?: string;
  validUntil?: string;
};
export type ImportTussResultDto = { imported: number; totalInFile: number; message: string };
export type ImportSigtapResultDto = { imported: number; totalInFile: number; competence: string; message: string };
export type SyncSigtapOfficialResultDto = {
  success: boolean;
  competence: string;
  remoteCompetence: string;
  sourceUrl: string;
  fileHash?: string;
  fileSizeBytes: number;
  inserted: number;
  updated: number;
  skipped: number;
  totalInFile: number;
  message: string;
  syncedAtUtc: string;
};
export type OperatorProfileDto = {
  operatorCode: string;
  names: string[];
  authorizationDeadlineDays: number;
  requiresOnlineAuthorization: boolean;
  businessRules: string;
  portalUrl: string;
};
export type UpdateAuthorizationRequest = {
  status: number; authorizationNumber?: string; validFrom?: string; validUntil?: string;
  procedureSummary?: string; notes?: string;
};
export type InsuranceAuthorizationDto = {
  id: string; patientId: string; patientName: string; healthInsuranceId: string; healthInsuranceName: string;
  authorizationType: number; status: number; authorizationNumber: string; validFrom?: string; validUntil?: string;
  procedureSummary?: string; tissGuideId?: string; notes?: string; createdAt: string;
};
export type CreateTissBatchRequest = { healthInsuranceId: string; competence: string; guideIds?: string[] };
export type TissBatchDto = {
  id: string; batchNumber: string; healthInsuranceId: string; healthInsuranceName: string; competence: string;
  status: number; protocolNumber?: string; sentAt?: string; totalAmount: number; guideCount: number; createdAt: string;
};
export type TissBatchDetailDto = TissBatchDto & {
  xmlContent?: string;
  guides: { id: string; guideNumber: string; patientName: string; totalAmount: number; status: number }[];
};
export type TissXmlValidationResultDto = {
  isValid: boolean;
  tissVersion?: string;
  hashValid: boolean;
  computedHash?: string;
  providedHash?: string;
  schemaValid?: boolean | null;
  schemaMessage?: string;
  errors: string[];
};
export type TissConvenioDashboardDto = {
  totalBilled: number; totalPaid: number; totalGlosaOpen: number; glosaRatePercent: number;
  guidesSentOver30Days: number; guidesSentOver60Days: number;
  byOperator: { operatorName: string; count: number; amount: number }[];
  glosaByOperator: { operatorName: string; count: number; amount: number }[];
};
export type TissReconciliationSummaryDto = {
  guidesWithReceivable: number; guidesPaidInFinance: number; totalReceivableOpen: number; totalReceivablePaid: number;
};
export type TussCatalogDto = {
  id: string; code: string; description: string; tableType: ApiEnum<1 | 2 | 3 | 4 | 5 | 6>; unit?: string;
  referencePrice?: number; validFrom?: string; validUntil?: string;
};
export type SigtapProcedureDto = {
  id: string; code: string; competence: string; description: string; groupName?: string; complexity?: string;
  hospitalAmount?: number; professionalAmount?: number;
};
export type SigtapCatalogSummaryDto = {
  totalCount: number;
  lastCompetence?: string;
  lastImportAt?: string;
};
export type TissDemonstrativoDto = {
  id: string; demonstrativoNumber: string; healthInsuranceId: string; healthInsuranceName: string;
  competence: string; status: number; totalBilled: number; totalPaid: number; totalGlosa: number;
  itemCount: number; matchedCount: number; createdAt: string; processedAt?: string;
};
export type TissDemonstrativoDetailDto = TissDemonstrativoDto & {
  sourceFileName?: string;
  items: TissDemonstrativoItemDto[];
};
export type TissDemonstrativoItemDto = {
  id: string; guideNumber: string; tussCode?: string; billedAmount: number; paidAmount: number;
  glosaAmount: number; glosaReason?: string; ansGlosaCode?: string; isMatched: boolean; tissGuideId?: string;
};
export type ImportDemonstrativoRequest = {
  healthInsuranceId: string; competence: string; sourceFileName?: string; csvContent: string;
};
export type FetchOperatorDemonstrativoRequest = { healthInsuranceId: string; tissBatchId?: string };
export type TissGuideAnnexDto = {
  id: string; tissGuideId: string; annexType: number; cid10Code?: string; clinicalIndication?: string;
  cycleInfo?: string; notes?: string; opmeItems: TissOpmeItemDto[];
};
export type TissOpmeItemDto = {
  id: string; tussCode: string; description: string; manufacturer?: string;
  authorizationNumber?: string; quantity: number; unitPrice: number; total: number;
};
export type CreateTissGuideAnnexRequest = {
  tissGuideId: string; annexType: number; cid10Code?: string; clinicalIndication?: string;
  cycleInfo?: string; notes?: string; opmeItems?: TissOpmeItemRequest[];
};
export type TissOpmeItemRequest = {
  tussCode: string; description: string; manufacturer?: string; authorizationNumber?: string;
  quantity: number; unitPrice: number;
};
export type HealthInsuranceIntegrationDto = {
  id: string; name: string; ansRegistration?: string; tissVersion?: string; operatorCode?: string;
  portalUrl?: string; webServiceUrl?: string; integrationUser?: string; useMockIntegration: boolean; isActive: boolean;
};
export type UpdateHealthInsuranceIntegrationRequest = {
  tissVersion?: string; operatorCode?: string; portalUrl?: string; webServiceUrl?: string;
  integrationUser?: string; integrationSecret?: string; useMockIntegration: boolean;
};
export type OperatorTransactionLogDto = {
  id: string; healthInsuranceId: string; healthInsuranceName: string; transactionType: number;
  status: number; referenceId?: string; errorMessage?: string; durationMs?: number; createdAt: string;
};
export const tissAnnexTypeLabels: Record<number, string> = {
  1: 'Quimioterapia', 2: 'Radioterapia', 3: 'OPME', 4: 'Solicitação especial',
};
export const tussTableTypeLabels: Record<number, string> = {
  1: 'Procedimento', 2: 'Material', 3: 'Medicamento', 4: 'Diária', 5: 'Taxa', 6: 'Pacote',
};
const tussTableTypeByName: Record<string, number> = {
  Procedure: 1, Material: 2, Medication: 3, Daily: 4, Fee: 5, Package: 6,
};
export function tussTableTypeLabel(type: ApiEnum<1 | 2 | 3 | 4 | 5 | 6>): string {
  const value = typeof type === 'number' ? type : tussTableTypeByName[type] ?? 0;
  return tussTableTypeLabels[value] ?? String(type);
}
export const demonstrativoStatusLabels: Record<number, string> = {
  1: 'Importado', 2: 'Processado', 3: 'Parcial', 4: 'Erro',
};
export const operatorTransactionTypeLabels: Record<number, string> = {
  1: 'Elegibilidade', 2: 'Autorização', 3: 'Envio lote', 4: 'Demonstrativo',
};

export const labOrderStatusLabels: Record<number, string> = { 1: 'Solicitado', 2: 'Em processamento', 3: 'Concluído', 4: 'Cancelado' };
export const labItemStatusLabels: Record<number, string> = { 1: 'Pendente', 2: 'Coletado', 3: 'Processando', 4: 'Concluído', 5: 'Cancelado' };
export const imagingModalityLabels: Record<number, string> = { 1: 'Raio-X', 2: 'Tomografia', 3: 'Ressonância', 4: 'Ultrassom', 5: 'Mamografia' };
export const imagingStatusLabels: Record<number, string> = { 1: 'Agendado', 2: 'Em andamento', 3: 'Concluído', 4: 'Cancelado' };
export const employeeRoleLabels: Record<number, string> = { 1: 'Enfermeiro(a)', 2: 'Técnico(a)', 3: 'Administrativo', 4: 'Gestor', 5: 'Outro' };
export const shiftTypeLabels: Record<number, string> = { 1: 'Manhã', 2: 'Tarde', 3: 'Noite' };
export const tissGuideTypeLabels: Record<number, string> = {
  1: 'Consulta',
  2: 'SP/SADT',
  3: 'Internação (legado)',
  4: 'Resumo de internação',
  5: 'Honorário individual',
  6: 'Solicitação de internação',
  7: 'Outras despesas',
  8: 'Comprovante presencial',
  9: 'Prorrogação de internação',
  10: 'Recurso de glosa',
  11: 'Demonstrativo de pagamento',
  12: 'Tratamento odontológico (GTO)',
  13: 'GTO — situação inicial',
  14: 'Demonstrativo odontológico',
  15: 'Recurso glosa odontológica',
  16: 'Anexo OPME',
  17: 'Anexo quimioterapia',
  18: 'Anexo radioterapia',
  19: 'Monitoramento / lote TISS',
};

export type TissGuideTypeCatalogDto = {
  code: number;
  slug: string;
  name: string;
  shortName: string;
  category: string;
  categoryLabel: string;
  description: string;
  whenToUse: string;
  isCreatable: boolean;
  isImplemented: boolean;
  linkedTab?: string;
  ansManualUrl?: string;
};
export const tissGuideStatusLabels: Record<number, string> = { 1: 'Rascunho', 2: 'Enviada', 3: 'Paga', 4: 'Glosa', 5: 'Cancelada' };
export const eligibilityStatusLabels: Record<number, string> = { 1: 'Elegível', 2: 'Inelegível', 3: 'Pendente', 4: 'Erro' };
export const authorizationTypeLabels: Record<number, string> = {
  1: 'Consulta', 2: 'SP/SADT', 3: 'Internação', 4: 'OPME', 5: 'Prorrogação',
};
export const authorizationStatusLabels: Record<number, string> = {
  1: 'Solicitada', 2: 'Aprovada', 3: 'Negada', 4: 'Parcial', 5: 'Expirada', 6: 'Cancelada',
};

export type GuidesHubFilterParams = {
  dateFrom?: string;
  dateTo?: string;
  patientId?: string;
  healthInsuranceId?: string;
  professionalId?: string;
  specialtyId?: string;
  procedureSearch?: string;
  guideNumber?: string;
  status?: string;
  guideType?: string;
  groupId?: string;
  serviceUnit?: string;
  serviceUnitId?: string;
  skip?: number;
  take?: number;
};

export type ServiceUnitDto = {
  id: string;
  name: string;
  code: string;
  cnes?: string;
  address?: string;
  isDefault: boolean;
  isActive: boolean;
};

export type CreateServiceUnitRequest = {
  name: string;
  code: string;
  cnes?: string;
  address?: string;
  isDefault?: boolean;
};

export type UpdateServiceUnitRequest = {
  name: string;
  code: string;
  cnes?: string;
  address?: string;
  isDefault: boolean;
  isActive: boolean;
};

export type SusGuideDto = {
  id: string;
  guideNumber: string;
  guideType: number;
  status: number;
  patientId: string;
  patientName: string;
  professionalId?: string;
  professionalName?: string;
  serviceUnitId?: string;
  serviceUnitName?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  cid10Code?: string;
  sigtapProcedureCode?: string;
  procedureDescription?: string;
  competence?: string;
  authorizationNumber?: string;
  authorizedAt?: string;
  submittedAt?: string;
  totalAmount?: number;
  notes?: string;
  createdAt: string;
};

export type CreateSusGuideRequest = {
  patientId: string;
  guideType: number;
  professionalId?: string;
  serviceUnitId?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  cid10Code?: string;
  sigtapProcedureCode?: string;
  procedureDescription?: string;
  competence?: string;
  notes?: string;
  totalAmount?: number;
};

export type UpdateSusGuideRequest = {
  professionalId?: string;
  serviceUnitId?: string;
  appointmentId?: string;
  hospitalizationId?: string;
  cid10Code?: string;
  sigtapProcedureCode?: string;
  procedureDescription?: string;
  competence?: string;
  notes?: string;
  totalAmount?: number;
};

export type SusGuideFilterParams = {
  dateFrom?: string;
  dateTo?: string;
  patientId?: string;
  professionalId?: string;
  serviceUnitId?: string;
  guideType?: number;
  status?: number;
  guideNumber?: string;
  procedureSearch?: string;
  skip?: number;
  take?: number;
};

export type SusGuideListResultDto = {
  total: number;
  items: SusGuideDto[];
};

export const susGuideTypeLabels: Record<number, string> = {
  1: 'BPA', 2: 'APAC', 3: 'AIH',
};

export const susGuideStatusLabels: Record<number, string> = {
  1: 'Rascunho', 2: 'Enviada', 3: 'Autorizada', 4: 'Faturada', 5: 'Glosa', 6: 'Cancelada',
};

export type GuideHubListItemDto = {
  id: string;
  guideNumber: string;
  patientName: string;
  healthInsuranceName: string;
  requestingProfessionalName?: string;
  specialtyName?: string;
  procedureSummary?: string;
  cid10Code?: string;
  createdAt: string;
  authorizedAt?: string;
  status: number;
  statusLabel: string;
  guideType: number;
  guideTypeLabel: string;
  serviceUnit: string;
  totalAmount: number;
  source: string;
};

export type GuidesHubListResultDto = {
  total: number;
  items: GuideHubListItemDto[];
};

export type GuidesHubProductionSliceDto = {
  label: string;
  count: number;
  amount: number;
};

export type GuidesHubDashboardDto = {
  issuedCount: number;
  authorizedCount: number;
  pendingCount: number;
  billedCount: number;
  glosaCount: number;
  avgAuthorizationHours?: number;
  byInsurance: GuidesHubProductionSliceDto[];
  byProfessional: GuidesHubProductionSliceDto[];
  bySpecialty: GuidesHubProductionSliceDto[];
};

export type GuideHistoryEntryDto = {
  occurredAt: string;
  action: string;
  userEmail?: string;
  details: string;
  source: string;
};
export const tissBatchStatusLabels: Record<number, string> = {
  1: 'Rascunho', 2: 'XML gerado', 3: 'Enviado', 4: 'Processado', 5: 'Rejeitado',
};
export const glosaContestationLabels: Record<number, string> = {
  0: '—', 1: 'Recurso enviado', 2: 'Aceito', 3: 'Rejeitado',
};

export type TriageRequestDto = {
  symptoms: string;
  patientId?: string;
  documentNumber?: string;
  susCardNumber?: string;
  healthInsuranceName?: string;
  systolicBp?: number;
  diastolicBp?: number;
  temperatureC?: number;
  heartRateBpm?: number;
  oxygenSaturationPct?: number;
  painLevel?: number;
  healthHistory?: string;
};
export type TriageResponseDto = {
  triageLogId: string;
  urgency: string;
  urgencyLabel: string;
  manchesterColor: string;
  manchesterColorHex: string;
  maxWaitMinutes: number;
  referral: string;
  referralLabel: string;
  recommendedSpecialty: string;
  suggestedCid10?: string;
  suggestedCid10Description?: string;
  guidance: string;
  relatedCodes: Cid10SuggestionDto[];
};
export type Cid10SuggestionRequestDto = { text: string; maxResults?: number };
export type Cid10SuggestionDto = { code: string; description: string; category?: string; score: number };
export type AiTriageLogDto = {
  id: string;
  patientName?: string;
  symptoms: string;
  urgency: string;
  urgencyLabel: string;
  manchesterColor: string;
  maxWaitMinutes: number;
  recommendedSpecialty: string;
  suggestedCid10?: string;
  createdAt: string;
};
export type TriageAdmissionSuggestionDto = {
  triageLogId: string;
  reason: string;
  diagnosis?: string;
  urgency: string;
  urgencyLabel: string;
  manchesterColor: string;
  recommendedSpecialty: string;
  suggestedCid10?: string;
  suggestedCid10Description?: string;
  createdAt: string;
};
export type AiInsightIndicatorDto = { label: string; value: string; severity?: string };
export type AiInsightReportDto = {
  id: string;
  type: 'Outbreak' | 'RecurrentPatient' | 'TriageOperational';
  title: string;
  summary: string;
  riskLevel: string;
  indicators: AiInsightIndicatorDto[];
  markdown: string;
  createdAt: string;
  patientId?: string;
  patientName?: string;
  groqEnriched?: boolean;
  aiModel?: string | null;
};
export type GroqStatusDto = { configured: boolean; enabled: boolean; model: string };
export type IntegrationMessageDto = {
  id: string;
  type: string;
  status: string;
  source: string;
  destination?: string;
  payloadPreview: string;
  errorMessage?: string;
  patientName?: string;
  createdAt: string;
};
export type Hl7InboundRequestDto = { rawMessage: string; source: string };
export type FhirPatientExportDto = { resourceType: string; id: string; json: string };
export type IntegrationProcessResultDto = {
  messageId: string;
  status: string;
  summary?: string;
  patientId?: string;
};
export type PatientPortalDashboardDto = {
  patientName: string;
  recordNumber?: string;
  upcomingAppointments: PatientAppointmentDto[];
  recentLabResults: PatientLabResultDto[];
};
export type PatientAppointmentDto = {
  id: string;
  scheduledAt: string;
  professionalName: string;
  specialtyName: string;
  status: number;
};
export type PatientLabResultDto = {
  examName: string;
  value: string;
  referenceRange?: string;
  isAbnormal: boolean;
  releasedAt?: string;
};
export type PatientMedicalRecordDto = {
  recordNumber: string;
  entries: PatientRecordEntryDto[];
};
export type PatientRecordEntryDto = {
  entryType: string;
  content: string;
  professionalName?: string;
  createdAt: string;
};

export type EmergencyVisitDto = {
  id: string;
  patientId: string;
  patientName: string;
  chiefComplaint: string;
  urgency: string;
  status: string;
  professionalName?: string;
  arrivedAt: string;
  startedAt?: string;
  dischargedAt?: string;
  notes?: string;
};
export type CreateEmergencyVisitRequest = {
  patientId: string;
  chiefComplaint: string;
  urgency: string;
  aiTriageLogId?: string;
  notes?: string;
};
export type UpdateEmergencyVisitStatusRequest = {
  status: string;
  professionalId?: string;
  notes?: string;
};
export type SupplierDto = {
  id: string;
  name: string;
  cnpj?: string;
  email?: string;
  phone?: string;
  contactName?: string;
};
export type CreateSupplierRequest = {
  name: string;
  cnpj?: string;
  email?: string;
  phone?: string;
  contactName?: string;
};
export type PurchaseOrderItemDto = {
  id: string;
  productId: string;
  productName: string;
  quantity: number;
  receivedQuantity: number;
  unitPrice: number;
  total: number;
};
export type PurchaseOrderDto = {
  id: string;
  orderNumber: string;
  supplierId: string;
  supplierName: string;
  sector: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13>;
  priority: ApiEnum<1 | 2 | 3>;
  requestedBy: string;
  justification?: string;
  status: string;
  orderedAt: string;
  expectedAt?: string;
  totalAmount: number;
  notes?: string;
  items: PurchaseOrderItemDto[];
};
export type PurchaseOrderItemRequest = { productId: string; quantity: number; unitPrice: number };
export type CreatePurchaseOrderRequest = {
  supplierId: string;
  sector: number;
  priority: number;
  requestedBy: string;
  justification?: string;
  expectedAt?: string;
  notes?: string;
  items: PurchaseOrderItemRequest[];
};

export type PurchaseSectorPresetDto = {
  sector: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13>;
  label: string;
  description: string;
  suggestedDeliveryDays: number;
};

export type PurchaseSuggestedItemDto = {
  productId: string;
  productName: string;
  sku: string;
  unit: string;
  quantityOnHand: number;
  minimumStock: number;
  isLowStock: boolean;
  suggestedQuantity: number;
  suggestedUnitPrice: number;
  reason: string;
};

export type PurchaseCreateSuggestionsDto = {
  sectors: PurchaseSectorPresetDto[];
  selectedSector: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13>;
  suggestedSupplierId?: string;
  suggestedDeliveryDays: number;
  lowStockItems: PurchaseSuggestedItemDto[];
  kitItems: PurchaseSuggestedItemDto[];
  suppliers: SupplierDto[];
};
export type ReceivePurchaseOrderRequest = {
  items: { itemId: string; quantity: number }[];
};
export type AuditLogDto = {
  id: string;
  userEmail: string;
  action: string;
  entityType: string;
  entityId?: string;
  details: string;
  ipAddress?: string;
  userAgent?: string;
  actionCategory?: string;
  isSensitive: boolean;
  createdAt: string;
};
export type LoginApiResponse = {
  token?: string;
  requiresMfa: boolean;
  mfaToken?: string;
  userId: string;
  fullName: string;
  email: string;
  role: UserRoleName;
  professionalId?: string;
  patientId?: string;
  permissions: string[];
  mfaEnabled: boolean;
};
export type ComplianceDashboardDto = {
  activeConsents: number;
  revokedConsents: number;
  openSubjectRequests: number;
  openPrivacyIncidents: number;
  failedLogins24h: number;
  activeSessions: number;
  usersWithMfa: number;
};
export type LoginAttemptDto = {
  id: string;
  email: string;
  success: boolean;
  failureReason?: string;
  ipAddress?: string;
  createdAt: string;
};
export type UserSessionDto = {
  id: string;
  userId: string;
  userEmail: string;
  userFullName: string;
  createdAt: string;
  expiresAt: string;
  revokedAt?: string;
  ipAddress?: string;
  userAgent?: string;
  isActive: boolean;
};
export type ConsentTermDto = {
  id: string;
  version: string;
  title: string;
  content: string;
  purposes: string[];
  effectiveFrom: string;
  isCurrent: boolean;
};
export type PatientConsentDto = {
  id: string;
  patientId: string;
  patientName: string;
  termVersion: string;
  termTitle: string;
  purposes: string[];
  readAt?: string;
  acknowledgedAt?: string;
  signerName?: string;
  hasSignature: boolean;
  grantedAt: string;
  revokedAt?: string;
  ipAddress?: string;
  recordedByName?: string;
};
export type RecordPatientConsentRequest = {
  patientId: string;
  consentTermId: string;
  purposes: string[];
  readAt: string;
  acknowledgedAt: string;
  signerName: string;
  signatureImage: string;
  notes?: string;
};
export type SignPatientConsentRequest = {
  consentTermId: string;
  purposes: string[];
  readAt: string;
  acknowledgedAt: string;
  signerName: string;
  signatureImage: string;
  notes?: string;
};
export type PatientConsentStatusDto = {
  patientId: string;
  pendingTerms: ConsentTermDto[];
  activeConsents: PatientConsentDto[];
};
export type PatientConsentDetailDto = {
  id: string;
  patientId: string;
  patientName: string;
  termVersion: string;
  termTitle: string;
  termContent: string;
  purposes: string[];
  readAt?: string;
  acknowledgedAt?: string;
  signerName?: string;
  signatureImage?: string;
  signatureHash?: string;
  grantedAt: string;
  revokedAt?: string;
  ipAddress?: string;
  recordedByName?: string;
  notes?: string;
};
export type DataSubjectRequestDto = {
  id: string;
  patientId: string;
  patientName: string;
  requestType: string;
  status: string;
  details?: string;
  requestedAt: string;
  completedAt?: string;
  handledByName?: string;
  responseNotes?: string;
};
export type CreateDataSubjectRequest = {
  patientId: string;
  requestType: string;
  details?: string;
};
export type UpdateDataSubjectRequestStatus = {
  status: string;
  responseNotes?: string;
};
export type PrivacyIncidentDto = {
  id: string;
  title: string;
  incidentType: string;
  severity: string;
  status: string;
  description: string;
  detectedAt: string;
  resolvedAt?: string;
  reportedByName?: string;
  investigationNotes?: string;
  notificationNotes?: string;
};
export type CreatePrivacyIncidentRequest = {
  title: string;
  incidentType: string;
  severity: string;
  description: string;
};
export type UpdatePrivacyIncidentRequest = {
  status: string;
  investigationNotes?: string;
  notificationNotes?: string;
};
export type MfaSetupResponse = {
  secret: string;
  qrCodeUri: string;
  manualEntryKey: string;
};
export type NotificationDto = {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  relatedEntityType?: string;
  relatedEntityId?: string;
  createdAt: string;
};

export type HubNotificationItemDto = {
  id: string;
  source: string;
  title: string;
  message: string;
  category: string;
  isRead: boolean;
  linkDestino?: string;
  priority: string;
  createdAt: string;
};

export type HubSummaryDto = {
  unreadNotifications: number;
  unreadMail: number;
  unreadChat: number;
  pendingGuides: number;
  pendingItemsCount: number;
  criticalCount: number;
  status: 'green' | 'yellow' | 'red';
  items: HubNotificationItemDto[];
};

export type PendencyDto = {
  id: string;
  titulo: string;
  descricao: string;
  modulo: string;
  tipo: string;
  status: string;
  prioridade: string;
  responsavel?: string;
  setor?: string;
  dataAbertura: string;
  dataLimite?: string;
  linkDestino?: string;
  usuarioResponsavelId?: string;
};

export type PendencySummaryDto = {
  total: number;
  abertas: number;
  vencidas: number;
  criticas: number;
  porModulo: Record<string, number>;
};

export type CommandCenterDashboardDto = {
  emergency: {
    waiting: number;
    inCare: number;
    critical: number;
    averageWaitMinutes: number;
    slaViolations: number;
  };
  beds: {
    total: number;
    occupied: number;
    available: number;
    cleaning: number;
    maintenance: number;
    reserved: number;
    occupancyRate: number;
  };
  warehouse: {
    lowStockProducts: number;
    expiringLots: number;
  };
  pendingRequisitions: number;
  surgeries: {
    total: number;
    scheduled: number;
    inProgress: number;
    completed: number;
    cancelled: number;
  };
  openPendencies: number;
  criticalClinicalAlerts: number;
  wards: {
    wardId: string;
    wardName: string;
    total: number;
    occupied: number;
    available: number;
    cleaning: number;
    maintenance: number;
    reserved: number;
  }[];
  operations: {
    pendingCleaning: number;
    pendingTransport: number;
    activeAmbulanceDispatches: number;
  };
  emergencyQueue: {
    id: string;
    patientName: string;
    chiefComplaint: string;
    urgency: string;
    arrivedAt: string;
    waitMinutes: number;
  }[];
  recentTvCalls: {
    ticketNumber: string;
    patientName?: string | null;
    destination: string;
    calledAt: string;
  }[];
  recentEvents: HospitalEventLogDto[];
  generatedAt: string;
};

export type OperationsQueueSnapshotDto = {
  emergency: CommandCenterDashboardDto['emergencyQueue'];
  recentTvCalls: CommandCenterDashboardDto['recentTvCalls'];
  generatedAt: string;
};

export type PatientTimelineEventDto = {
  type: string;
  title: string;
  description: string;
  at: string;
  professionalName?: string;
  link?: string;
};

export type PatientTimelineDto = {
  patientId: string;
  patientName: string;
  events: PatientTimelineEventDto[];
};

export const emergencyStatusLabels: Record<string, string> = {
  Aguardando: 'Aguardando',
  'Em atendimento': 'Em atendimento',
  Alta: 'Alta',
  Encaminhado: 'Encaminhado',
  Waiting: 'Aguardando',
  InCare: 'Em atendimento',
  Discharged: 'Alta',
  Referred: 'Encaminhado',
};
export const triageUrgencyLabels: Record<string, string> = {
  Emergência: 'Vermelho — Emergência',
  Alta: 'Laranja — Muito urgente',
  Média: 'Amarelo — Urgente',
  Baixa: 'Verde — Pouco urgente',
  'Não urgente': 'Azul — Não urgente',
  Emergency: 'Vermelho — Emergência',
  High: 'Laranja — Muito urgente',
  Medium: 'Amarelo — Urgente',
  Low: 'Verde — Pouco urgente',
  NonUrgent: 'Azul — Não urgente',
};

/** Classe CSS de badge/cor para urgência de triagem (PT-BR ou legado EN). */
export function triageUrgencyCssClass(urgency: string): string {
  switch (urgency) {
    case 'Emergency':
    case 'Emergência':
      return 'urgency-emergency';
    case 'High':
    case 'Alta':
      return 'urgency-high';
    case 'Medium':
    case 'Média':
      return 'urgency-medium';
    case 'Low':
    case 'Baixa':
      return 'urgency-low';
    case 'NonUrgent':
    case 'Não urgente':
      return 'urgency-nonurgent';
    default:
      return '';
  }
}

export const manchesterProtocolInfo = [
  { urgency: 'Emergency', color: 'Vermelho', hex: '#dc2626', wait: 'Imediato', description: 'Risco imediato de morte. Atendimento imediato.' },
  { urgency: 'High', color: 'Laranja', hex: '#ea580c', wait: '10 min', description: 'Risco moderado a alto de agravamento.' },
  { urgency: 'Medium', color: 'Amarelo', hex: '#ca8a04', wait: '60 min', description: 'Gravidade moderada sem risco iminente.' },
  { urgency: 'Low', color: 'Verde', hex: '#16a34a', wait: '120 min', description: 'Baixa complexidade clínica.' },
  { urgency: 'NonUrgent', color: 'Azul', hex: '#2563eb', wait: '240 min', description: 'Casos simples — considerar encaminhamento à UBS.' },
] as const;
export const purchaseOrderStatusLabels: Record<string, string> = {
  Rascunho: 'Rascunho',
  Enviado: 'Enviado',
  'Recebido parcial': 'Recebido parcial',
  Recebido: 'Recebido',
  Cancelado: 'Cancelado',
  Draft: 'Rascunho',
  Sent: 'Enviado',
  PartiallyReceived: 'Recebido parcial',
  Received: 'Recebido',
  Cancelled: 'Cancelado',
};

export const purchaseSectorLabels: Record<number, string> = {
  1: 'Farmácia',
  2: 'Laboratório',
  3: 'Imagem',
  4: 'Centro cirúrgico',
  5: 'UTI',
  6: 'Pronto-socorro',
  7: 'Nutrição',
  8: 'Lavanderia',
  9: 'Eng. clínica',
  10: 'CCIH',
  11: 'Hotelaria',
  12: 'Enfermagem',
  13: 'Administração',
};

export const purchasePriorityLabels: Record<number, string> = {
  1: 'Normal',
  2: 'Urgente',
  3: 'Crítica',
};

const purchaseSectorByName: Record<string, number> = {
  Pharmacy: 1, Laboratory: 2, Imaging: 3, SurgeryCenter: 4, Icu: 5, Emergency: 6,
  Nutrition: 7, Laundry: 8, ClinicalEngineering: 9, InfectionControl: 10,
  Hospitality: 11, Nursing: 12, Administration: 13,
};

const purchasePriorityByName: Record<string, number> = {
  Normal: 1, Urgent: 2, Critical: 3,
};

export function purchaseSectorValue(sector: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13>): number {
  if (typeof sector === 'number') return sector;
  return purchaseSectorByName[sector] ?? 13;
}

export function purchaseSectorLabel(sector: ApiEnum<1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 | 11 | 12 | 13>): string {
  return purchaseSectorLabels[purchaseSectorValue(sector)] ?? '—';
}

export function purchasePriorityValue(priority: ApiEnum<1 | 2 | 3>): number {
  if (typeof priority === 'number') return priority;
  return purchasePriorityByName[priority] ?? 1;
}

export function purchasePriorityLabel(priority: ApiEnum<1 | 2 | 3>): string {
  return purchasePriorityLabels[purchasePriorityValue(priority)] ?? '—';
}
export const notificationTypeLabels: Record<string, string> = {
  Info: 'Informação',
  Warning: 'Aviso',
  Alert: 'Alerta',
};

export type IcuDashboardDto = {
  totalIcuBeds: number;
  occupiedBeds: number;
  criticalAlerts: number;
  patients: IcuPatientDto[];
};
export type IcuPatientDto = {
  hospitalizationId: string;
  patientId: string;
  patientName: string;
  bedNumber: string;
  wardName: string;
  latestVitals?: VitalSignDto;
  alertLevel: string;
};
export type VitalSignDto = {
  id: string;
  heartRate: number;
  systolicBp: number;
  diastolicBp: number;
  spO2: number;
  temperature: number;
  respiratoryRate: number;
  recordedAt: string;
  recordedByName?: string;
};
export type RecordVitalSignsRequest = {
  hospitalizationId: string;
  heartRate: number;
  systolicBp: number;
  diastolicBp: number;
  spO2: number;
  temperature: number;
  respiratoryRate: number;
  recordedByProfessionalId?: string;
  notes?: string;
};
export type AmbulanceDto = { id: string; code: string; plate: string; status: string; baseLocation?: string };
export type AmbulanceDispatchDto = {
  id: string; ambulanceId?: string; ambulanceCode?: string; patientName: string;
  pickupAddress: string; destination: string; status: string;
  requestedAt: string; dispatchedAt?: string; completedAt?: string; notes?: string;
};
export type CreateAmbulanceDispatchRequest = {
  patientName: string; pickupAddress: string; destination: string; notes?: string;
};
export type ParkingZoneDto = {
  id: string; name: string; totalSpots: number; occupiedSpots: number; hourlyRate: number; description?: string;
};
export type ParkingSessionDto = {
  id: string; zoneId: string; zoneName: string; vehiclePlate: string; patientName?: string;
  enteredAt: string; exitedAt?: string; status: string; amountCharged?: number;
  isPaid: boolean; paidAt?: string; estimatedAmount?: number; qrPayload: string;
};
export type ParkingGateExitResultDto = {
  allowed: boolean; message: string; session?: ParkingSessionDto;
};
export type CheckInParkingRequest = { zoneId: string; vehiclePlate: string; patientId?: string };
export type DietOrderDto = {
  id: string; hospitalizationId: string; patientName: string; wardName: string; bedNumber: string;
  dietType: string; mealPeriod: string; status: string; mealDate: string; notes?: string; deliveredAt?: string;
};
export type CreateDietOrderRequest = {
  hospitalizationId: string; dietType: string; mealPeriod: string; mealDate: string; notes?: string;
};

export const ambulanceStatusLabels: Record<string, string> = {
  Available: 'Disponível', Dispatched: 'Despachada', OnScene: 'No local',
  Transporting: 'Transportando', Maintenance: 'Manutenção',
};
export const dispatchStatusLabels: Record<string, string> = {
  Requested: 'Solicitado', Dispatched: 'Despachado', OnScene: 'No local',
  Transporting: 'Transportando', Completed: 'Concluído', Cancelled: 'Cancelado',
};

export type TransportAssetDto = {
  id: string; code: string; assetTag: string; assetType: string; sector: string;
  status: string; trackingCode?: string; notes?: string;
};
export type TransportRequestDto = {
  id: string; patientId?: string; hospitalizationId?: string; patientName: string;
  originType: string; originDetail?: string; destinationType: string; destinationDetail?: string;
  status: string; priority: string; assignedEmployeeId?: string; assignedEmployeeName?: string;
  transportAssetId?: string; transportAssetCode?: string;
  requestedAt: string; acceptedAt?: string; arrivedAtOriginAt?: string;
  departedAt?: string; arrivedAtDestinationAt?: string; completedAt?: string;
  notes?: string; requestedBy?: string;
  slaDeadlineAt?: string; isSlaViolated: boolean;
};
export type TransportDashboardDto = {
  totalAssets: number; availableAssets: number; activeRequests: number;
  queuedRequests: number; inTransitRequests: number;
  avgAcceptMinutes?: number; avgCompleteMinutes?: number;
  topOriginSector?: string; mostProductivePorter?: string;
  liveQueue: TransportRequestDto[]; recentCompleted: TransportRequestDto[];
};
export type TransportMetricsDto = {
  avgAcceptMinutes?: number; avgCompleteMinutes?: number; avgTransitMinutes?: number;
  sectorDemand: { originType: string; requestCount: number }[];
  porterProductivity: { employeeId: string; employeeName: string; completedCount: number; avgCompleteMinutes?: number }[];
};
export type TransportPorterDto = { id: string; fullName: string; jobTitle?: string };
export type CreateTransportAssetRequest = {
  code: string; assetTag: string; assetType: string; sector: string;
  trackingCode?: string; notes?: string;
};
export type CreateTransportRequestRequest = {
  patientId?: string; hospitalizationId?: string; patientName: string;
  originType: string; originDetail?: string; destinationType: string; destinationDetail?: string;
  priority: string; notes?: string;
};
export type AcceptTransportRequestRequest = { employeeId: string; transportAssetId?: string };

export const transportAssetTypeLabels: Record<string, string> = {
  Stretcher: 'Maca', Wheelchair: 'Cadeira de rodas', ElectricVehicle: 'Veículo elétrico', Other: 'Outro',
};
export const transportAssetStatusLabels: Record<string, string> = {
  Available: 'Disponível', InUse: 'Em uso', Cleaning: 'Higienização', Maintenance: 'Manutenção',
};
export const transportLocationLabels: Record<string, string> = {
  Emergency: 'Pronto Atendimento', Icu: 'UTI', SurgeryCenter: 'Centro Cirúrgico',
  Hospitalization: 'Internação', ImagingTomography: 'Tomografia', ImagingXray: 'Raio-X',
  Laboratory: 'Laboratório', Discharge: 'Alta hospitalar', Other: 'Outro',
};
export const transportRequestStatusLabels: Record<string, string> = {
  Queued: 'Na fila', Accepted: 'Aceito', InTransit: 'Em deslocamento',
  Completed: 'Concluído', Cancelled: 'Cancelado',
};
export const transportPriorityLabels: Record<string, string> = {
  Normal: 'Normal', Urgent: 'Urgente',
};

export type CleaningChecklistItemDto = { id: string; label: string; done: boolean };
export type CleaningRequestDto = {
  id: string; bedId: string; wardName: string; bedNumber: string;
  hospitalizationId?: string; cleaningType: string; status: string; triggerReason: string;
  assignedTeam?: string; assignedEmployeeId?: string; assignedEmployeeName?: string;
  requestedAt: string; startedAt?: string; completedAt?: string;
  checklist: CleaningChecklistItemDto[]; notes?: string;
};
export type HotelariaNocDto = {
  totalBeds: number; availableBeds: number; occupiedBeds: number; cleaningBeds: number;
  maintenanceBeds: number; occupancyRate: number; pendingAdmissions: number;
  pendingCleanings: number; activeTransports: number; avgCleaningMinutes?: number;
  avgTransportAcceptMinutes?: number;
  pendingCleaningQueue: CleaningRequestDto[];
  activeTransportQueue: TransportRequestDto[];
  bedMap: { bedId: string; wardName: string; bedNumber: string; status: string; occupantName?: string }[];
};
export type CreateCleaningRequestRequest = { bedId: string; cleaningType: string; notes?: string };
export type StartCleaningRequestRequest = { assignedTeam?: string; assignedEmployeeId?: string };

export const cleaningTypeLabels: Record<string, string> = {
  Terminal: 'Terminal', Concurrent: 'Concorrente', Routine: 'Rotina',
};
export const cleaningStatusLabels: Record<string, string> = {
  Requested: 'Solicitada', InProgress: 'Em andamento', Completed: 'Concluída', Cancelled: 'Cancelada',
};
export const cleaningTriggerLabels: Record<string, string> = {
  Manual: 'Manual', Discharge: 'Alta', Transfer: 'Transferência', Routine: 'Rotina',
};

export type SyncMutationItemDto = {
  clientMutationId: string;
  entity: string;
  action: string;
  payload: Record<string, unknown>;
  clientTimestamp: string;
};
export type SyncPushRequest = {
  deviceId: string;
  mutations: SyncMutationItemDto[];
};
export type SyncMutationResultDto = {
  clientMutationId: string;
  status: string;
  message?: string;
  serverPayload?: unknown;
};
export type SyncPushResponse = {
  serverTimestamp: string;
  results: SyncMutationResultDto[];
};
export type SyncPullRequest = {
  since?: string;
  sector?: string;
  wardId?: string;
};
export type BedSyncDto = {
  id: string;
  wardId: string;
  wardName: string;
  bedNumber: string;
  status: string;
  statusReason?: string;
  updatedAt: string;
};
export type SyncPullResponse = {
  serverTimestamp: string;
  transportRequests: TransportRequestDto[];
  cleaningRequests: CleaningRequestDto[];
  transportAssets: TransportAssetDto[];
  porters: TransportPorterDto[];
  beds: BedSyncDto[];
};
export const dietTypeLabels: Record<string, string> = {
  Regular: 'Regular', Soft: 'Pastosa', Liquid: 'Líquida', Diabetic: 'Diabética', LowSodium: 'Baixo sódio',
};
export const mealPeriodLabels: Record<string, string> = {
  Breakfast: 'Café', Lunch: 'Almoço', Dinner: 'Jantar', Snack: 'Lanche',
};
export const dietOrderStatusLabels: Record<string, string> = {
  Pending: 'Pendente', InPreparation: 'Em preparo', Delivered: 'Entregue', Cancelled: 'Cancelado',
};
export const icuAlertLabels: Record<string, string> = {
  Normal: 'Normal', Warning: 'Atenção', Critical: 'Crítico', Unknown: 'Sem dados',
};

export type SpecialtyDto = { id: string; name: string; cboCode?: string };
export type ConsultingRoomDto = {
  id: string; name: string; floor?: string; building?: string;
  status: string; specialtyName?: string;
};
export type ConsultingRoomScheduleDto = {
  id: string; consultingRoomId: string; roomName: string;
  professionalId: string; professionalName: string; specialtyName: string;
  dayOfWeek: string; startTime: string; endTime: string;
};
export type CreateConsultingRoomRequest = {
  name: string; floor?: string; building?: string; specialtyId?: string;
};
export type CreateRoomScheduleRequest = {
  consultingRoomId: string; professionalId: string; dayOfWeek: string;
  startTime: string; endTime: string;
};
export type HospitalityRoomDto = {
  id: string; roomNumber: string; floor?: string; capacity: number;
  dailyRate: number; status: string;
};
export type HospitalityBookingDto = {
  id: string; roomId: string; roomNumber: string; guestName: string;
  patientName?: string; status: string; checkInDate: string; checkOutDate?: string;
  dailyRate: number; notes?: string;
};
export type CreateHospitalityBookingRequest = {
  roomId: string; patientId?: string; guestName: string; guestDocument?: string;
  guestPhone?: string; checkInDate: string; checkOutDate?: string; notes?: string;
};
export type MedicalEquipmentDto = {
  id: string; name: string; assetTag: string; manufacturer?: string; model?: string;
  location?: string; status: string; lastMaintenanceDate?: string; nextMaintenanceDate?: string;
};
export type MaintenanceWorkOrderDto = {
  id: string; equipmentId: string; equipmentName: string; title: string; description: string;
  status: string; technicianName?: string; createdAt: string; completedAt?: string;
};
export type CreateMedicalEquipmentRequest = {
  name: string; assetTag: string; manufacturer?: string; model?: string;
  location?: string; nextMaintenanceDate?: string;
};
export type CreateWorkOrderRequest = {
  equipmentId: string; title: string; description: string; technicianName?: string;
};
export type SecurityIncidentDto = {
  id: string; type: string; status: string; location: string; description: string;
  reportedBy?: string; createdAt: string; resolvedAt?: string;
};
export type VisitorLogDto = {
  id: string; visitorName: string; documentNumber?: string; patientName?: string;
  destination?: string; badgeNumber?: string; status: string;
  enteredAt: string; exitedAt?: string; photoData?: string; hasPhoto: boolean;
};
export type SecuritySettingsDto = {
  visitorPhotoRequired: boolean;
};
export type CreateSecurityIncidentRequest = {
  type: string; location: string; description: string; reportedBy?: string;
};
export type RegisterVisitorRequest = {
  visitorName: string; documentNumber?: string; patientId?: string;
  destination?: string; badgeNumber?: string; photoData?: string;
};
export type SecurityDashboardDto = {
  visitorsInside: number; openIncidents: number;
  recentVisitors: VisitorLogDto[]; openIncidentsList: SecurityIncidentDto[];
};

export type PhysicalAccessDashboardDto = {
  peopleInsideEstimate: number;
  accessGrantedToday: number;
  accessDeniedToday: number;
  activeCompanions: number;
  vehiclesInside: number;
  facialEnrollments: number;
  recentAccess: AccessControlRecordDto[];
  recentLpr: LprReadEventDto[];
};
export type AccessZoneDto = {
  id: string; code: string; name: string; building?: string; floor?: string;
  requiresAuthorization: boolean;
};
export type AccessTurnstileDto = {
  id: string; code: string; name: string; zoneId?: string; zoneName?: string;
  integrationVendor?: string; isEntry: boolean;
};
export type AccessControlRecordDto = {
  id: string; personType: string; personName: string; method: string;
  direction: string; result: string; location?: string; details?: string; occurredAt: string;
};
export type AccessCredentialDto = {
  id: string; personType: string; holderName: string; credentialType: string;
  status: string; token: string; zoneName?: string; validUntil?: string;
};
export type FacialBiometricDto = {
  id: string; personType: string; personName: string; status: string;
  enrolledAt: string; hasPhoto: boolean;
};
export type RegisteredVehicleDto = {
  id: string; plate: string; model?: string; color?: string;
  ownerCategory: string; ownerName: string; parkingExempt: boolean;
};
export type LprReadEventDto = {
  id: string; plate: string; cameraLocation: string; direction: string;
  gateOpened: boolean; ownerName?: string; ownerCategory?: string; readAt: string;
};
export type KioskTicketDto = {
  id: string; ticketType: string; ticketNumber: string; patientName?: string;
  sector?: string; issuedAt: string; called: boolean;
};
export type AccessIntegrationProfileDto = {
  vendor: string; category: string; description: string; mockEnabled: boolean; endpoint?: string;
};
export type EmployeeSectorAccessDto = {
  employeeId: string; employeeName: string; department: string;
  allowedZone?: string; onShift: boolean; lastAccess?: string;
};
export type AppointmentQrDto = {
  appointmentId: string; qrPayload: string; patientName: string; scheduledAt: string;
};
export type TurnstileValidationRequest = {
  turnstileCode: string; method: string; payload: string; direction?: string;
};
export type TurnstileValidationResultDto = {
  granted: boolean; result: string; message: string;
  personName?: string; personType?: string; recordId?: string;
};
export type IssueCompanionCredentialRequest = {
  patientId: string; companionName: string; documentNumber?: string;
  credentialType: string; allowedZoneId?: string;
  visitStartTime?: string; visitEndTime?: string; validUntil?: string;
};
export type EnrollFacialRequest = {
  personType: string; personName: string; patientId?: string;
  employeeId?: string; professionalId?: string; photoData?: string; templatePayload: string;
};
export type FacialValidationRequest = {
  turnstileCode: string; personId?: string; personType?: string; templatePayload?: string;
};
export type KioskCheckInRequest = { cpf?: string; qrPayload?: string; facialTemplateId?: string };
export type KioskCheckInResultDto = {
  success: boolean; message: string; appointmentId?: string;
  patientName?: string; ticket?: KioskTicketDto;
};
export type IssueKioskTicketRequest = {
  ticketType: string; patientId?: string; patientName?: string; sector?: string;
};
export type RegisterVehicleRequest = {
  plate: string; model?: string; color?: string; ownerCategory: string;
  ownerName: string; patientId?: string; employeeId?: string; parkingExempt?: boolean;
};
export type LprReadRequest = { plate: string; cameraLocation: string; direction: string };
export type LprReadResultDto = {
  gateOpened: boolean; message: string; vehicle?: RegisteredVehicleDto; event: LprReadEventDto;
};

export const accessPersonTypeLabels: Record<string, string> = {
  Patient: 'Paciente', Companion: 'Acompanhante', Employee: 'Funcionário',
  Visitor: 'Visitante', Contractor: 'Prestador', Doctor: 'Médico', Nurse: 'Enfermeiro',
};
export const accessMethodLabels: Record<string, string> = {
  Facial: 'Facial', QrCode: 'QR Code', Rfid: 'RFID', Password: 'Senha',
  Biometric: 'Biometria', PlateLpr: 'LPR',
};
export const accessValidationLabels: Record<string, string> = {
  Granted: 'Liberado', Denied: 'Negado', Expired: 'Expirado', WrongZone: 'Setor incorreto',
  MaxCompanions: 'Limite acompanhantes', NoAppointment: 'Sem consulta', OutsideHours: 'Fora do horário',
};
export const kioskTicketTypeLabels: Record<string, string> = {
  Consultation: 'Consulta', Exam: 'Exame', Hospitalization: 'Internação',
  Emergency: 'Emergência', Laboratory: 'Laboratório',
};
export const vehicleOwnerLabels: Record<string, string> = {
  Patient: 'Paciente', Doctor: 'Médico', Employee: 'Funcionário',
  Visitor: 'Visitante', Contractor: 'Prestador',
};

export const consultingRoomStatusLabels: Record<string, string> = {
  Available: 'Disponível', Occupied: 'Ocupado', Maintenance: 'Manutenção',
};
export const hospitalityRoomStatusLabels: Record<string, string> = {
  Available: 'Disponível', Occupied: 'Ocupado', Cleaning: 'Limpeza', Maintenance: 'Manutenção',
};
export const hospitalityBookingStatusLabels: Record<string, string> = {
  Reserved: 'Reservado', CheckedIn: 'Hospedado', CheckedOut: 'Check-out', Cancelled: 'Cancelado',
};
export const medicalEquipmentStatusLabels: Record<string, string> = {
  Operational: 'Operacional', Maintenance: 'Manutenção', OutOfService: 'Fora de serviço', CalibrationDue: 'Calibração pendente',
};
export const workOrderStatusLabels: Record<string, string> = {
  Open: 'Aberta', InProgress: 'Em andamento', Completed: 'Concluída', Cancelled: 'Cancelada',
};
export const securityIncidentTypeLabels: Record<string, string> = {
  AccessDenied: 'Acesso negado', VisitorIssue: 'Visitante', AssetAlert: 'Patrimônio',
  Emergency: 'Emergência', Other: 'Outro',
};
export const securityIncidentStatusLabels: Record<string, string> = {
  Open: 'Aberta', Investigating: 'Investigando', Resolved: 'Resolvida',
};
export const visitorLogStatusLabels: Record<string, string> = {
  Inside: 'No hospital', Exited: 'Saída registrada',
};
export const dayOfWeekLabels: Record<string, string> = {
  Sunday: 'Domingo', Monday: 'Segunda', Tuesday: 'Terça', Wednesday: 'Quarta',
  Thursday: 'Quinta', Friday: 'Sexta', Saturday: 'Sábado',
};

export type InstrumentKitDto = {
  id: string; name: string; code: string; description?: string;
  status: string; sterilityExpiration?: string;
};
export type SterilizationCycleDto = {
  id: string; instrumentKitId: string; kitName: string; kitCode: string;
  method: string; status: string; sterilizerName: string; operatorName?: string;
  startedAt?: string; completedAt?: string; expirationDate?: string;
};
export type CreateInstrumentKitRequest = { name: string; code: string; description?: string };
export type CreateSterilizationCycleRequest = {
  instrumentKitId: string; method: string; sterilizerName: string; operatorName?: string;
};
export type BloodUnitDto = {
  id: string; unitCode: string; bloodType: string; component: string;
  volumeMl: number; collectedAt: string; expiresAt: string; status: string;
};
export type TransfusionRequestDto = {
  id: string; patientId: string; patientName: string; wardName?: string; bedNumber?: string;
  requestingProfessionalName: string; bloodTypeRequired: string; component: string;
  unitsRequested: number; status: string; bloodUnitCode?: string; notes?: string;
  createdAt: string; transfusedAt?: string;
};
export type CreateBloodUnitRequest = {
  unitCode: string; bloodType: string; component: string; volumeMl: number;
  collectedAt: string; expiresAt: string;
};
export type CreateTransfusionRequestRequest = {
  patientId: string; requestingProfessionalId: string; hospitalizationId?: string;
  bloodTypeRequired: string; component: string; unitsRequested: number; notes?: string;
};
export type DialysisSessionDto = {
  id: string; patientId: string; patientName: string; wardName?: string;
  machineNumber: string; status: string; scheduledAt: string;
  startedAt?: string; completedAt?: string; dryWeightKg?: number;
  nurseName?: string; notes?: string;
};
export type CreateDialysisSessionRequest = {
  patientId: string; hospitalizationId?: string; machineNumber: string;
  scheduledAt: string; dryWeightKg?: number; nurseName?: string; notes?: string;
};
export type LaundryBatchDto = {
  id: string; batchNumber: string; origin: string; originDetail?: string;
  itemCount: number; weightKg: number; status: string;
  collectedAt: string; deliveredAt?: string; notes?: string;
};
export type CreateLaundryBatchRequest = {
  origin: string; originDetail?: string; itemCount: number; weightKg: number; notes?: string;
};

export const instrumentKitStatusLabels: Record<string, string> = {
  Available: 'Disponível', InSterilization: 'Em esterilização', Sterile: 'Estéril',
  Expired: 'Vencido', InUse: 'Em uso',
};
export const sterilizationMethodLabels: Record<string, string> = {
  Steam: 'Vapor', Eto: 'Óxido de etileno', Plasma: 'Plasma',
};
export const sterilizationCycleStatusLabels: Record<string, string> = {
  Pending: 'Pendente', InProgress: 'Em andamento', Completed: 'Concluído', Failed: 'Falhou',
};
export const bloodTypeLabels: Record<string, string> = {
  APositive: 'A+', ANegative: 'A-', BPositive: 'B+', BNegative: 'B-',
  ABPositive: 'AB+', ABNegative: 'AB-', OPositive: 'O+', ONegative: 'O-',
};
export const bloodComponentLabels: Record<string, string> = {
  WholeBlood: 'Sangue total', PackedRedCells: 'Hemácias', Platelets: 'Plaquetas', Plasma: 'Plasma',
};
export const bloodUnitStatusLabels: Record<string, string> = {
  Available: 'Disponível', Reserved: 'Reservada', Transfused: 'Transfundida',
  Discarded: 'Descartada', Expired: 'Vencida',
};
export const transfusionStatusLabels: Record<string, string> = {
  Requested: 'Solicitada', Matched: 'Compatibilizada', Transfused: 'Transfundida', Cancelled: 'Cancelada',
};
export const dialysisStatusLabels: Record<string, string> = {
  Scheduled: 'Agendada', InProgress: 'Em andamento', Completed: 'Concluída', Cancelled: 'Cancelada',
};
export const laundryOriginLabels: Record<string, string> = {
  Ward: 'Enfermaria', Icu: 'UTI', Surgery: 'Centro Cirúrgico', Emergency: 'Emergência', Other: 'Outro',
};
export const laundryBatchStatusLabels: Record<string, string> = {
  Collected: 'Coletado', Washing: 'Lavando', Drying: 'Secando', Delivered: 'Entregue',
};

export type ChemotherapySessionDto = {
  id: string; patientId: string; patientName: string; wardName?: string;
  professionalName: string; protocolName: string; drugRegimen: string;
  cycleNumber: number; totalCycles: number; status: string;
  scheduledAt: string; administeredAt?: string; notes?: string;
};
export type CreateChemotherapySessionRequest = {
  patientId: string; professionalId: string; hospitalizationId?: string;
  protocolName: string; drugRegimen: string; cycleNumber: number; totalCycles: number;
  scheduledAt: string; notes?: string;
};
export type PhysiotherapySessionDto = {
  id: string; patientId: string; patientName: string; wardName?: string;
  therapistName: string; sessionType: string; status: string;
  scheduledAt: string; durationMinutes: number; goals?: string; notes?: string;
};
export type CreatePhysiotherapySessionRequest = {
  patientId: string; hospitalizationId?: string; therapistName: string;
  sessionType: string; scheduledAt: string; durationMinutes: number;
  goals?: string; notes?: string;
};
export type TelemedicineAppointmentDto = {
  id: string; patientId: string; patientName: string; professionalName: string;
  specialtyName: string; scheduledAt: string; status: string; meetingUrl?: string;
  chiefComplaint: string; notes?: string; startedAt?: string; completedAt?: string;
};
export type CreateTelemedicineAppointmentRequest = {
  patientId: string; professionalId: string; scheduledAt: string;
  chiefComplaint: string; notes?: string;
};
export type InfectionSurveillanceDto = {
  id: string; patientId?: string; patientName?: string; wardName?: string;
  location: string; infectionType: string; organism: string; site?: string;
  status: string; detectedAt: string; reportedBy?: string; notes?: string; resolvedAt?: string;
};
export type IsolationPrecautionDto = {
  id: string; patientId: string; patientName: string; wardName?: string;
  precautionType: string; status: string; startDate: string; endDate?: string; reason: string;
};
export type InfectionControlDashboardDto = {
  activeIsolations: number; openSurveillanceCases: number;
  recentCases: InfectionSurveillanceDto[]; activePrecautions: IsolationPrecautionDto[];
};
export type CreateInfectionSurveillanceRequest = {
  patientId?: string; hospitalizationId?: string; location: string;
  infectionType: string; organism: string; site?: string; reportedBy?: string; notes?: string;
};
export type CreateIsolationPrecautionRequest = {
  patientId: string; hospitalizationId?: string; precautionType: string;
  startDate: string; reason: string;
};

export const chemotherapyStatusLabels: Record<string, string> = {
  Scheduled: 'Agendada', InPreparation: 'Em preparo', Administered: 'Administrada',
  Completed: 'Concluída', Cancelled: 'Cancelada',
};
export const physiotherapyTypeLabels: Record<string, string> = {
  Mobility: 'Motora', Respiratory: 'Respiratória', Neurological: 'Neurológica',
  PostOperative: 'Pós-operatória', Other: 'Outra',
};
export const physiotherapyStatusLabels: Record<string, string> = {
  Scheduled: 'Agendada', InProgress: 'Em andamento', Completed: 'Concluída', Cancelled: 'Cancelada',
};
export const telemedicineStatusLabels: Record<string, string> = {
  Scheduled: 'Agendada', Waiting: 'Aguardando', InProgress: 'Em consulta',
  Completed: 'Concluída', Cancelled: 'Cancelada', NoShow: 'Faltou',
};
export const infectionTypeLabels: Record<string, string> = {
  Urinary: 'Urinária', Respiratory: 'Respiratória', SurgicalSite: 'Sítio cirúrgico',
  Bloodstream: 'Corrente sanguínea', Other: 'Outra',
};
export const infectionSurveillanceStatusLabels: Record<string, string> = {
  Suspected: 'Suspeita', Confirmed: 'Confirmada', Resolved: 'Resolvida',
};
export const isolationPrecautionTypeLabels: Record<string, string> = {
  Contact: 'Contato', Droplet: 'Gotículas', Airborne: 'Aerossóis', Protective: 'Protetora',
};
export const isolationPrecautionStatusLabels: Record<string, string> = {
  Active: 'Ativa', Lifted: 'Suspensa',
};

export type ConnectDashboardDto = {
  activeConversations: number;
  messagesToday: number;
  pendingReminders: number;
  waitlistWaiting: number;
  surveysThisMonth: number;
  averageNps: number;
  checkInsToday: number;
};

export type ConnectIntegrationStatusDto = {
  whatsAppEnabled: boolean;
  useMockProvider: boolean;
  providerName: string;
  metaConfigured: boolean;
  webhookSecretConfigured: boolean;
  verifyTokenConfigured: boolean;
  liveMode: boolean;
  ready: boolean;
  webhookPath: string;
  overdueAccounts: number;
  collectionRemindersSentToday: number;
  collectionAgentEnabled: boolean;
  publicWebhookUrl?: string | null;
  templateLanguageCode: string;
  reminderTemplateName: string;
  billingTemplateName: string;
  confirmationTemplateName: string;
  failedMessagesToday: number;
  healthIssues: string[];
};

export type IntegrationConfigVarDto = {
  envKey: string;
  appsettingsPath: string;
  description: string;
  isConfigured: boolean;
  requiredForProduction: boolean;
};

export type WhatsAppReadinessDto = {
  enabled: boolean;
  demoMode: boolean;
  modeLabel: string;
  ready: boolean;
  liveMode: boolean;
  providerName: string;
  webhookPath: string;
  publicWebhookUrl?: string | null;
  configVars: IntegrationConfigVarDto[];
  issues: string[];
};

export type PixReadinessDto = {
  enabled: boolean;
  demoMode: boolean;
  modeLabel: string;
  ready: boolean;
  autoConfirmEnabled: boolean;
  webhookPath: string;
  configVars: IntegrationConfigVarDto[];
  issues: string[];
};

export type TissOperatorReadinessDto = {
  id: string;
  name: string;
  demoMode: boolean;
  webServiceConfigured: boolean;
  tissVersion?: string | null;
};

export type TissReadinessDto = {
  totalOperators: number;
  demoOperators: number;
  liveOperators: number;
  configuredOperators: number;
  modeLabel: string;
  ready: boolean;
  operators: TissOperatorReadinessDto[];
  configVars: IntegrationConfigVarDto[];
  issues: string[];
};

export type IntegrationReadinessDto = {
  whatsApp: WhatsAppReadinessDto;
  pix: PixReadinessDto;
  tiss: TissReadinessDto;
};

export type IntegrationTestResultDto = {
  integration: string;
  success: boolean;
  message: string;
  details: string[];
  testedAt: string;
};

export type ConnectInboxSummaryDto = {
  awaitingHuman: number;
  assignedOpen: number;
  messagesToday: number;
  failedMessagesToday: number;
};

export type ConnectConversationQuery = {
  limit?: number;
  botStep?: string;
  queue?: string;
  awaitingHumanOnly?: boolean;
};

export type ConnectAssignRequest = {
  userId?: string | null;
  queue?: string | null;
};

export type ConnectConversationDto = {
  id: string;
  patientId?: string;
  patientName?: string;
  channel: string;
  contactPhone: string;
  botStep: string;
  lastMessageAt?: string;
  lastMessagePreview?: string;
  queue: string;
  assignedUserId?: string;
  assignedUserName?: string;
  humanRequestedAt?: string;
  resolvedAt?: string;
};

export type ConnectMessageDto = {
  id: string;
  conversationId: string;
  direction: string;
  status: string;
  body: string;
  createdAt: string;
  reminderType?: string;
};

export type ConnectConversationDetailDto = {
  conversation: ConnectConversationDto;
  messages: ConnectMessageDto[];
};

export type ConnectWaitlistDto = {
  id: string;
  patientId: string;
  patientName: string;
  specialtyName: string;
  professionalName?: string;
  status: string;
  priority: number;
  createdAt: string;
};

export type ConnectKnowledgeArticleDto = {
  id: string;
  category: string;
  question: string;
  answer: string;
  keywords?: string;
};

export type ConnectSatisfactionStatsDto = {
  averageScore: number;
  totalResponses: number;
  byProfessional: { name: string; averageScore: number; count: number }[];
  bySpecialty: { name: string; averageScore: number; count: number }[];
};

export type SimulateInboundRequest = { phone: string; message: string; contactName?: string };
export type SimulateInboundResponse = { reply: string; conversationId: string };
export type BlockProfessionalScheduleRequest = { professionalId: string; date: string; reason: string };
export type BlockProfessionalScheduleResult = { affectedAppointments: number; notificationsSent: number };

export type MessagePriority = 'Baixa' | 'Normal' | 'Alta' | 'Urgente' | 'Critica';
export type MailFolder = 'Inbox' | 'Sent' | 'Drafts' | 'Trash' | 'Archive';
export type MessageRecipientType = 'To' | 'Cc' | 'Bcc';
export type ChatRoomType = 'Private' | 'Sector' | 'Group';
export type ConnectNotificationCategory = 'Info' | 'Alert' | 'System';

export type ConnectCommSummaryDto = {
  unreadMailCount: number;
  unreadChatCount: number;
  unreadNotificationCount: number;
  unviewedBulletinCount: number;
};

export type MailListItemDto = {
  id: string;
  subject: string;
  preview: string;
  priority: MessagePriority;
  senderName: string;
  createdAt: string;
  isRead: boolean;
  attachmentCount: number;
};

export type MailRecipientInputDto = { userId: string; type: MessageRecipientType };
export type MailAttachmentInputDto = {
  fileName: string;
  contentBase64?: string;
  mimeType: string;
  sizeBytes: number;
};

export type CreateMailRequest = {
  subject: string;
  content: string;
  priority: MessagePriority;
  recipients: MailRecipientInputDto[];
  attachments?: MailAttachmentInputDto[];
  sendNow: boolean;
  context?: MailContextInputDto;
};

export type MailContextInputDto = {
  patientId?: string;
  tissGuideId?: string;
  susGuideId?: string;
  appointmentId?: string;
  ticketId?: string;
};

export type UpdateMailRequest = {
  subject: string;
  content: string;
  priority: MessagePriority;
  recipients: MailRecipientInputDto[];
  attachments?: MailAttachmentInputDto[];
};

export type MailDetailDto = {
  id: string;
  subject: string;
  content: string;
  priority: MessagePriority;
  status: 'Draft' | 'Sent';
  senderId: string;
  senderName: string;
  createdAt: string;
  isRead: boolean;
  readAt?: string;
  folder: MailFolder;
  recipients: { userId: string; userName: string; type: MessageRecipientType; isRead: boolean; readAt?: string }[];
  attachments: { id: string; fileName: string; mimeType: string; sizeBytes: number }[];
};

export type ChatRoomDto = {
  id: string;
  name: string;
  roomType: ChatRoomType;
  lastMessageAt?: string;
  lastMessagePreview?: string;
  unreadCount: number;
};

export type CreateChatRoomRequest = {
  name: string;
  roomType: ChatRoomType;
  sectorId?: string;
  participantUserIds: string[];
};

export type ChatMessageDto = {
  id: string;
  roomId: string;
  senderId: string;
  senderName: string;
  content: string;
  createdAt: string;
  isRead: boolean;
};

export type SendChatMessageRequest = { content: string };

export type ConnectNotificationDto = {
  id: string;
  title: string;
  message: string;
  category: ConnectNotificationCategory;
  isRead: boolean;
  relatedEntityType?: string;
  relatedEntityId?: string;
  createdAt: string;
};

export type BulletinPostDto = {
  id: string;
  title: string;
  content: string;
  authorName: string;
  publishedAt?: string;
  isPinned: boolean;
  isViewed: boolean;
  viewCount: number;
  createdAt: string;
};

export type CreateBulletinPostRequest = {
  title: string;
  content: string;
  isPinned: boolean;
  publishNow: boolean;
};

export type UpdateBulletinPostRequest = CreateBulletinPostRequest;

export type ConnectTicketCategory =
  | 'TI'
  | 'Infraestrutura'
  | 'Compras'
  | 'RH'
  | 'Financeiro'
  | 'EngenhariaClinica'
  | 'Manutencao';

export type ConnectTicketStatus =
  | 'Aberto'
  | 'EmAndamento'
  | 'Aguardando'
  | 'Resolvido'
  | 'Cancelado';

export type ConnectTaskStatus =
  | 'Aberta'
  | 'EmAndamento'
  | 'Aguardando'
  | 'Concluida'
  | 'Cancelada';

export type WorkflowType = 'SolicitacaoCompra' | 'AprovacaoGenerica';

export type WorkflowInstanceStatus = 'Pendente' | 'Aprovado' | 'Rejeitado' | 'Cancelado';

export type WorkflowStepStatus = 'Pendente' | 'Aprovado' | 'Rejeitado';

export const connectTicketCategoryLabels: Record<ConnectTicketCategory, string> = {
  TI: 'TI',
  Infraestrutura: 'Infraestrutura',
  Compras: 'Compras',
  RH: 'RH',
  Financeiro: 'Financeiro',
  EngenhariaClinica: 'Engenharia Clínica',
  Manutencao: 'Manutenção',
};

export const connectTicketStatusLabels: Record<ConnectTicketStatus, string> = {
  Aberto: 'Aberto',
  EmAndamento: 'Em andamento',
  Aguardando: 'Aguardando',
  Resolvido: 'Resolvido',
  Cancelado: 'Cancelado',
};

export const connectTaskStatusLabels: Record<ConnectTaskStatus, string> = {
  Aberta: 'Aberta',
  EmAndamento: 'Em andamento',
  Aguardando: 'Aguardando',
  Concluida: 'Concluída',
  Cancelada: 'Cancelada',
};

export const workflowTypeLabels: Record<WorkflowType, string> = {
  SolicitacaoCompra: 'Solicitação de compra',
  AprovacaoGenerica: 'Aprovação genérica',
};

export const workflowInstanceStatusLabels: Record<WorkflowInstanceStatus, string> = {
  Pendente: 'Pendente',
  Aprovado: 'Aprovado',
  Rejeitado: 'Rejeitado',
  Cancelado: 'Cancelado',
};

export const workflowStepStatusLabels: Record<WorkflowStepStatus, string> = {
  Pendente: 'Pendente',
  Aprovado: 'Aprovado',
  Rejeitado: 'Rejeitado',
};

export const messagePriorityLabels: Record<MessagePriority, string> = {
  Baixa: 'Baixa',
  Normal: 'Normal',
  Alta: 'Alta',
  Urgente: 'Urgente',
  Critica: 'Crítica',
};

export type ConnectTicketSummaryDto = {
  totalAbertos: number;
  totalEmAndamento: number;
  totalAguardando: number;
  totalVencidos: number;
};

export type ConnectTicketListItemDto = {
  id: string;
  protocolo: string;
  titulo: string;
  categoria: ConnectTicketCategory;
  status: ConnectTicketStatus;
  prioridade: MessagePriority;
  solicitanteName: string;
  responsavelName?: string;
  dueAt?: string;
  isOverdue: boolean;
  createdAt: string;
};

export type ConnectTicketCommentDto = {
  id: string;
  userId: string;
  userName: string;
  content: string;
  createdAt: string;
};

export type ConnectTicketDetailDto = ConnectTicketListItemDto & {
  descricao: string;
  solicitanteId: string;
  responsavelId?: string;
  resolvedAt?: string;
  comments: ConnectTicketCommentDto[];
};

export type CreateConnectTicketRequest = {
  titulo: string;
  descricao: string;
  categoria: ConnectTicketCategory;
  prioridade: MessagePriority;
  responsavelId?: string;
};

export type ConnectTaskSummaryDto = {
  minhasAbertas: number;
  delegadasAbertas: number;
  vencidas: number;
  concluidasMes: number;
};

export type ConnectTaskListItemDto = {
  id: string;
  titulo: string;
  status: ConnectTaskStatus;
  prioridade: MessagePriority;
  criadorName: string;
  responsavelName?: string;
  prazo?: string;
  isOverdue: boolean;
  createdAt: string;
};

export type ConnectTaskDetailDto = ConnectTaskListItemDto & {
  descricao: string;
  criadorId: string;
  responsavelId?: string;
};

export type CreateConnectTaskRequest = {
  titulo: string;
  descricao: string;
  responsavelId?: string;
  prazo?: string;
  prioridade: MessagePriority;
};

export type WorkflowSummaryDto = {
  pendentesParaMim: number;
  minhasPendentes: number;
  aprovadasMes: number;
  rejeitadasMes: number;
};

export type WorkflowStepDto = {
  id: string;
  ordem: number;
  aprovadorId: string;
  aprovadorName: string;
  status: WorkflowStepStatus;
  justificativa?: string;
  respondedAt?: string;
};

export type WorkflowInstanceListItemDto = {
  id: string;
  tipo: WorkflowType;
  titulo: string;
  status: WorkflowInstanceStatus;
  solicitanteName: string;
  createdAt: string;
  pendingForMe: boolean;
};

export type WorkflowInstanceDetailDto = {
  id: string;
  tipo: WorkflowType;
  titulo: string;
  descricao: string;
  referencia?: string;
  status: WorkflowInstanceStatus;
  solicitanteId: string;
  solicitanteName: string;
  completedAt?: string;
  createdAt: string;
  steps: WorkflowStepDto[];
};

export type CreateWorkflowInstanceRequest = {
  tipo: WorkflowType;
  titulo: string;
  descricao: string;
  referencia?: string;
  aprovadorIds: string[];
};

export type ConnectCalendarRecurrenceRule = 'None' | 'Daily' | 'Weekly';

export const connectCalendarRecurrenceLabels: Record<ConnectCalendarRecurrenceRule, string> = {
  None: 'Não repetir',
  Daily: 'Diário',
  Weekly: 'Semanal',
};

export const connectCalendarParticipantResponseLabels: Record<ConnectCalendarParticipantResponse, string> = {
  Pendente: 'Pendente',
  Aceito: 'Aceito',
  Recusado: 'Recusado',
  Talvez: 'Talvez',
};

export type ConnectCalendarEventType = 'Reuniao' | 'Evento' | 'Escala' | 'Treinamento';

export const connectCalendarEventTypeLabels: Record<ConnectCalendarEventType, string> = {
  Reuniao: 'Reunião',
  Evento: 'Evento',
  Escala: 'Escala',
  Treinamento: 'Treinamento',
};

export type ConnectCalendarParticipantResponse = 'Pendente' | 'Aceito' | 'Recusado' | 'Talvez';

export type ConnectCalendarEventListItemDto = {
  id: string;
  titulo: string;
  descricao?: string;
  inicio: string;
  fim: string;
  local?: string;
  tipo: ConnectCalendarEventType;
  allDay: boolean;
  recurrenceRule: ConnectCalendarRecurrenceRule;
  color?: string;
  reminderMinutes?: number;
  organizadorName: string;
  setorId?: string;
  setorName?: string;
  participantCount: number;
  isOrganizer: boolean;
  myResponse?: ConnectCalendarParticipantResponse;
  isRecurrenceInstance?: boolean;
};

export type ConnectCalendarParticipantDto = {
  userId: string;
  userName: string;
  response?: ConnectCalendarParticipantResponse;
};

export type ConnectCalendarEventDetailDto = ConnectCalendarEventListItemDto & {
  organizadorId: string;
  participants: ConnectCalendarParticipantDto[];
  createdAt: string;
};

export type CreateConnectCalendarEventRequest = {
  titulo: string;
  descricao?: string;
  inicio: string;
  fim: string;
  local?: string;
  tipo: ConnectCalendarEventType;
  allDay: boolean;
  recurrenceRule?: ConnectCalendarRecurrenceRule;
  color?: string;
  reminderMinutes?: number;
  setorId?: string;
  participantUserIds?: string[];
};

export type UpdateConnectCalendarEventRequest = CreateConnectCalendarEventRequest;

export type ConnectContextMessageDto = {
  id: string;
  subject: string;
  content: string;
  priority: MessagePriority;
  senderName: string;
  createdAt: string;
  contextType: string;
  contextId: string;
  contextLabel?: string;
};

export type ConnectAiQuickQueryDto = {
  id: string;
  label: string;
  question: string;
};

export type ConnectAiAskResponse = {
  question: string;
  answer: string;
  intent: string;
  data?: Record<string, unknown>;
  usedLlm?: boolean;
};

export type ConnectAiStreamChunk = {
  type: 'token' | 'done';
  text?: string;
  intent?: string;
  usedLlm?: boolean;
  data?: Record<string, unknown>;
};

export type HelpArticleType = 'Faq' | 'Article' | 'Video' | 'Manual' | 'Training';

export type HelpSuggestionStatus = 'Pendente' | 'EmAnalise' | 'Aceita' | 'Rejeitada' | 'Implementada';

export const helpArticleTypeLabels: Record<HelpArticleType, string> = {
  Faq: 'FAQ',
  Article: 'Artigo',
  Video: 'Vídeo',
  Manual: 'Manual',
  Training: 'Treinamento',
};

export const helpSuggestionStatusLabels: Record<HelpSuggestionStatus, string> = {
  Pendente: 'Pendente',
  EmAnalise: 'Em análise',
  Aceita: 'Aceita',
  Rejeitada: 'Rejeitada',
  Implementada: 'Implementada',
};

export type HelpSummaryDto = {
  totalArticles: number;
  totalFaqs: number;
  totalVideos: number;
  totalTrainings: number;
  totalManuals: number;
  openTickets: number;
  myOpenTickets: number;
  totalViews: number;
  pendingSuggestions: number;
};

export type HelpCategoryDto = {
  id: string;
  code: string;
  name: string;
  icon?: string;
  articleCount: number;
};

export type HelpArticleListItemDto = {
  id: string;
  slug: string;
  title: string;
  summary?: string;
  type: HelpArticleType;
  categoryCode: string;
  categoryName: string;
  viewCount: number;
  trainingCompleted: boolean;
};

export type HelpArticleDetailDto = HelpArticleListItemDto & {
  content: string;
  videoUrl?: string;
  downloadUrl?: string;
};

export type HelpSearchResultDto = {
  items: HelpArticleListItemDto[];
  total: number;
};

export type HelpContextDto = {
  route: string;
  moduleLabel?: string;
  articles: HelpArticleListItemDto[];
  faqs: HelpArticleListItemDto[];
};

export type HelpAskRequest = {
  question: string;
  route?: string;
};

export type HelpAskResponse = {
  question: string;
  answer: string;
  relatedArticles: HelpArticleListItemDto[];
};

export type CreateHelpSuggestionRequest = {
  title: string;
  description: string;
  module?: string;
};

export type HelpSuggestionDto = {
  id: string;
  title: string;
  description: string;
  module?: string;
  status: HelpSuggestionStatus;
  createdAt: string;
};

export type HospitalEventLogDto = {
  id: string;
  eventType: string;
  routingKey: string;
  status: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  createdAt: string;
  processedAt?: string;
  errorMessage?: string;
};

export const hospitalEventStatusLabels: Record<string, string> = {
  Pending: 'Pendente',
  Processed: 'Processado',
  Failed: 'Falha',
  Partial: 'Parcial',
};

export const hospitalEventTypeLabels: Record<string, string> = {
  'patient.discharged': 'Alta hospitalar',
  'prescription.signed': 'Prescrição assinada',
  'stock.low': 'Estoque baixo',
};

export type UserMissionDto = {
  id: string;
  title: string;
  description: string;
  type: string;
  priority: string;
  linkDestino?: string;
  dataAbertura: string;
  dataLimite?: string;
  setor?: string;
  isPendingItem: boolean;
};

export type UserMissionsDto = {
  total: number;
  highPriority: number;
  missions: UserMissionDto[];
};

export type WasteCollectionDto = {
  id: string;
  code: string;
  wasteType: string;
  sectorName: string;
  quantityKg: number;
  containerCode: string;
  collectedAt: string;
  collectedBy: string;
  status: string;
  manifestNumber?: string;
  notes?: string;
};

export type WasteKpiDto = { wasteType: string; count: number; totalKg: number };

export type WasteDashboardDto = {
  totalCollections: number;
  totalKg: number;
  byType: WasteKpiDto[];
  recent: WasteCollectionDto[];
};

export type CreateWasteCollectionRequest = {
  wasteType: string;
  sectorName: string;
  quantityKg: number;
  containerCode: string;
  collectedBy: string;
  manifestNumber?: string;
  notes?: string;
};

export type UpdateWasteCollectionRequest = {
  status?: string;
  manifestNumber?: string;
  notes?: string;
};

export const wasteTypeLabels: Record<string, string> = {
  Infectious: 'Infectante',
  Sharps: 'Perfurocortante',
  Common: 'Comum',
  Chemical: 'Químico',
  Pharmaceutical: 'Farmacêutico',
};

export const wasteStatusLabels: Record<string, string> = {
  Registered: 'Registrado',
  Stored: 'Armazenado',
  PickedUp: 'Coletado',
  Disposed: 'Destinado',
};

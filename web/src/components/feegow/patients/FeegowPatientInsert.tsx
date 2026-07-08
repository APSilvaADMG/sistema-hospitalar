import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../../../api/client';
import { fetchAddressByCep, formatCep, normalizeCepDigits } from '../../../utils/cepLookup';
import {
  formatCnsInput,
  formatCpfInput,
  formatHeightInput,
  formatPhoneInput,
  formatRgInput,
  formatStateInput,
  formatWeightInput,
  isValidCns,
  onlyDigits,
} from '../../../utils/inputMasks';
import {
  birthLabelFromDate,
  computeImc,
  type FeegowPatientFormState,
} from './feegowPatientForm';
import { FeegowPatientInsuranceTable } from './FeegowPatientInsuranceTable';
import { EligibilityPanel } from '../../EligibilityPanel';
import { FeegowPatientPhotoCapture } from './FeegowPatientPhotoCapture';
import { FeegowPatientSchedulingTable } from './FeegowPatientSchedulingTable';
import {
  emptyLegalResponsible,
  FeegowLegalResponsibleModal,
  legalResponsibleRelationshipLabel,
} from './FeegowLegalResponsibleModal';

type Props = {
  form: FeegowPatientFormState;
  onChange: (patch: Partial<FeegowPatientFormState>) => void;
  onSubmit: (event: FormEvent) => void;
  saving?: boolean;
  birthLabel?: string;
  patientId?: string;
};

const GENDER_OPTIONS = [
  { value: 0, label: 'Selecione' },
  { value: 1, label: 'Masculino' },
  { value: 2, label: 'Feminino' },
  { value: 3, label: 'Outro' },
];

const PRIORITY_OPTIONS = ['', 'Baixa', 'Normal', 'Alta', 'Urgente'];
const EDUCATION_OPTIONS = ['', 'Fundamental incompleto', 'Fundamental completo', 'Médio incompleto', 'Médio completo', 'Superior incompleto', 'Superior completo', 'Pós-graduação'];
const ORIGIN_OPTIONS = ['', 'Indicação', 'Internet', 'Panfleto', 'Outro'];
const MARITAL_OPTIONS = ['', 'Solteiro(a)', 'Casado(a)', 'Divorciado(a)', 'Viúvo(a)', 'União estável'];

const ID_COLORS = [
  '#1a1a1a', '#e53935', '#fb8c00', '#fdd835', '#43a047', '#1e88e5', '#8e24aa', '#6d4c41',
];

export function FeegowPatientInsert({ form, onChange, onSubmit, saving, birthLabel: birthLabelProp, patientId }: Props) {
  const [cepLoading, setCepLoading] = useState(false);
  const [activeToggle, setActiveToggle] = useState(true);
  const [showLegalResponsibleModal, setShowLegalResponsibleModal] = useState(false);
  const [cnsHint, setCnsHint] = useState('');
  const [maritalOptions, setMaritalOptions] = useState<string[]>(MARITAL_OPTIONS.filter(Boolean));
  const [religionOptions, setReligionOptions] = useState<string[]>([]);

  useEffect(() => {
    Promise.all([
      api.getPatientReferenceCatalog(4),
      api.getPatientReferenceCatalog(3),
    ])
      .then(([marital, religion]) => {
        if (marital.length > 0) {
          setMaritalOptions(marital.map((item) => item.name));
        }
        if (religion.length > 0) {
          setReligionOptions(religion.map((item) => item.name));
        }
      })
      .catch(console.error);
  }, []);

  function patch(partial: Partial<FeegowPatientFormState>) {
    onChange(partial);
  }

  function handleNoCpfChange(checked: boolean) {
    if (checked) {
      setShowLegalResponsibleModal(true);
      return;
    }

    patch({ noCpf: false, legalResponsible: emptyLegalResponsible() });
  }

  function handleLegalResponsibleConfirm(value: FeegowPatientFormState['legalResponsible']) {
    patch({ noCpf: true, cpf: '', legalResponsible: value });
    setShowLegalResponsibleModal(false);
  }

  function handleLegalResponsibleClose() {
    setShowLegalResponsibleModal(false);
    if (!form.legalResponsible.name.trim()) {
      patch({ noCpf: false });
    }
  }

  function handleHeightWeightChange(heightCm: string, weightKg: string) {
    patch({
      heightCm,
      weightKg,
      imc: computeImc(heightCm, weightKg),
    });
  }

  async function handleCepBlur() {
    const digits = normalizeCepDigits(form.addressZipCode ?? '');
    if (digits.length !== 8) return;
    setCepLoading(true);
    try {
      const address = await fetchAddressByCep(digits);
      patch({
        addressZipCode: formatCep(digits),
        addressStreet: address.addressStreet,
        addressNeighborhood: address.addressNeighborhood,
        addressCity: address.addressCity,
        addressState: address.addressState,
        addressComplement: form.addressComplement || address.addressComplement || '',
      });
    } catch {
      /* ignore */
    } finally {
      setCepLoading(false);
    }
  }

  const birthLabel = birthLabelProp ?? birthLabelFromDate(form.birthDate);

  return (
    <form className="feegow-patient-card" onSubmit={onSubmit}>
      <header className="feegow-patient-card-head">
        <div className="feegow-patient-breadcrumb">
          <span className="feegow-patient-crumb-icon" aria-hidden>⌂</span>
          <span className="feegow-patient-crumb-sep">/</span>
          <span className="feegow-patient-crumb-icon feegow-patient-crumb-user" aria-hidden>👤</span>
          <span className="feegow-patient-crumb-sep">/</span>
          <span className="feegow-patient-crumb-label">{birthLabel}</span>
        </div>

        <div className="feegow-patient-toolbar">
          <label className="feegow-patient-toggle" title="Paciente ativo">
            <input
              type="checkbox"
              checked={activeToggle}
              onChange={(e) => setActiveToggle(e.target.checked)}
            />
            <span className="feegow-patient-toggle-track" />
          </label>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Anterior">‹</button>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Próximo">›</button>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Lista">☰</button>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Novo">+</button>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Imprimir">🖨</button>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Compartilhar">↗</button>
          <button type="button" className="feegow-patient-tool-btn" aria-label="Histórico">🕐</button>
          <button type="submit" className="feegow-patient-save-btn" disabled={saving}>
            💾 SALVAR
          </button>
        </div>
      </header>

      <div className="feegow-patient-form-body">
        <section className="feegow-patient-section">
          <h2 className="feegow-patient-section-title">Dados Principais</h2>

          <div className="feegow-patient-form-layout">
            <FeegowPatientPhotoCapture
              name={form.fullName || 'Paciente'}
              photoData={form.photoData}
              onChange={(photoData) => patch({ photoData })}
            />

            <div className="feegow-patient-fields-stack">
            <div className="feegow-patient-identity-grid feegow-patient-grid-head">
              <label className="feegow-field feegow-field-grow">
                <span>Nome</span>
                <div className="feegow-input-wrap">
                  <input
                    required
                    value={form.fullName}
                    onChange={(e) => patch({ fullName: e.target.value })}
                    placeholder=""
                  />
                  <span className="feegow-input-icon" aria-hidden>🔍</span>
                </div>
              </label>

              <label className="feegow-field">
                <span>Nascimento</span>
                <div className="feegow-input-wrap">
                  <input
                    type="date"
                    required
                    value={form.birthDate}
                    onChange={(e) => patch({ birthDate: e.target.value })}
                  />
                  <span className="feegow-input-icon" aria-hidden>📅</span>
                </div>
              </label>

              <div className="feegow-field feegow-field-check">
                <span>Estrangeiro</span>
                <label className="feegow-check-pill">
                  <input
                    type="checkbox"
                    checked={form.isForeigner}
                    onChange={(e) => patch({ isForeigner: e.target.checked })}
                  />
                  <span>{form.isForeigner ? 'SIM' : 'NÃO'}</span>
                </label>
              </div>

              <label className="feegow-field">
                <span>CPF<span className="feegow-req">*</span></span>
                <input
                  required={!form.noCpf}
                  disabled={form.noCpf}
                  value={form.cpf}
                  onChange={(e) => patch({ cpf: formatCpfInput(e.target.value) })}
                  placeholder="000.000.000-00"
                  inputMode="numeric"
                  autoComplete="off"
                />
              </label>

              <div className="feegow-field feegow-field-check">
                <span>Sem CPF</span>
                <label className="feegow-check-pill feegow-check-warn">
                  <input
                    type="checkbox"
                    checked={form.noCpf}
                    onChange={(e) => handleNoCpfChange(e.target.checked)}
                  />
                  <span>{form.noCpf ? 'SIM' : 'NÃO'}</span>
                </label>
              </div>

              {form.noCpf && form.legalResponsible.name ? (
                <div className="feegow-field feegow-field-span-full feegow-legal-responsible-summary">
                  <span>Responsável legal</span>
                  <div className="feegow-legal-responsible-summary-box">
                    <strong>{form.legalResponsible.name}</strong>
                    <span>
                      CPF {formatCpfInput(form.legalResponsible.cpf)} · {legalResponsibleRelationshipLabel(form.legalResponsible.relationship)}
                    </span>
                    <button
                      type="button"
                      className="feegow-cep-link"
                      onClick={() => setShowLegalResponsibleModal(true)}
                    >
                      Editar responsável
                    </button>
                  </div>
                </div>
              ) : null}

              <label className="feegow-field">
                <span>Cor de Identificação</span>
                <div className="feegow-color-select">
                  <span
                    className="feegow-color-swatch"
                    style={{ background: form.identificationColor }}
                  />
                  <select
                    value={form.identificationColor}
                    onChange={(e) => patch({ identificationColor: e.target.value })}
                  >
                    {ID_COLORS.map((c) => (
                      <option key={c} value={c}>{c}</option>
                    ))}
                  </select>
                </div>
              </label>

              <label className="feegow-field">
                <span>Sexo</span>
                <select
                  value={form.gender}
                  onChange={(e) => patch({ gender: Number(e.target.value) })}
                >
                  {GENDER_OPTIONS.map((o) => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </label>

              <label className="feegow-field feegow-field-grow">
                <span>Nome Social</span>
                <input
                  value={form.socialName ?? ''}
                  onChange={(e) => patch({ socialName: e.target.value })}
                />
              </label>

              <div className="feegow-field feegow-field-triple">
                <span>Altura / Peso / IMC</span>
                <div className="feegow-triple-inputs">
                  <input
                    placeholder="cm"
                    value={form.heightCm}
                    onChange={(e) => handleHeightWeightChange(formatHeightInput(e.target.value), form.weightKg)}
                    inputMode="numeric"
                  />
                  <input
                    placeholder="kg"
                    value={form.weightKg}
                    onChange={(e) => handleHeightWeightChange(form.heightCm, formatWeightInput(e.target.value))}
                    inputMode="decimal"
                  />
                  <input readOnly placeholder="IMC" value={form.imc} />
                </div>
              </div>

              <label className="feegow-field">
                <span>Prontuário<span className="feegow-req">*</span></span>
                <input
                  value={form.chartNumber}
                  onChange={(e) => patch({ chartNumber: e.target.value })}
                  placeholder="7"
                />
              </label>
            </div>

          <div className="feegow-patient-fields-grid feegow-patient-grid-body">
            <label className="feegow-field">
              <span>Prioridade</span>
              <select
                value={form.priority}
                onChange={(e) => patch({ priority: e.target.value })}
              >
                <option value="">Selecione</option>
                {PRIORITY_OPTIONS.filter(Boolean).map((p) => (
                  <option key={p} value={p}>{p}</option>
                ))}
              </select>
            </label>

            <label className="feegow-field">
              <span>Cep</span>
              <div className="feegow-cep-row">
                <input
                  value={form.addressZipCode ?? ''}
                  onChange={(e) => patch({ addressZipCode: formatCep(e.target.value) })}
                  onBlur={() => { handleCepBlur().catch(console.error); }}
                  placeholder="00000-000"
                  inputMode="numeric"
                />
                <button type="button" className="feegow-cep-link">Não sei o CEP</button>
              </div>
              {cepLoading ? <small className="feegow-field-hint">Buscando CEP…</small> : null}
            </label>

            <label className="feegow-field feegow-field-end">
              <span>Endereço</span>
              <input
                value={form.addressStreet ?? ''}
                onChange={(e) => patch({ addressStreet: e.target.value })}
              />
            </label>

            <label className="feegow-field feegow-field-num">
              <span>Número</span>
              <input
                value={form.addressNumber ?? ''}
                onChange={(e) => patch({ addressNumber: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Compl.</span>
              <input
                value={form.addressComplement ?? ''}
                onChange={(e) => patch({ addressComplement: e.target.value })}
              />
            </label>

            <label className="feegow-field feegow-field-bairro">
              <span>Bairro</span>
              <input
                value={form.addressNeighborhood ?? ''}
                onChange={(e) => patch({ addressNeighborhood: e.target.value })}
              />
            </label>

            <label className="feegow-field feegow-field-cidade">
              <span>Cidade</span>
              <input
                value={form.addressCity ?? ''}
                onChange={(e) => patch({ addressCity: e.target.value })}
              />
            </label>

            <label className="feegow-field feegow-field-estado">
              <span>Estado</span>
              <input
                value={form.addressState ?? ''}
                onChange={(e) => patch({ addressState: formatStateInput(e.target.value) })}
                maxLength={2}
              />
            </label>

            <label className="feegow-field feegow-field-pais">
              <span>País</span>
              <input
                value={form.country}
                onChange={(e) => patch({ country: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Telefone</span>
              <div className="feegow-input-wrap">
                <input
                  value={form.phone ?? ''}
                  onChange={(e) => patch({ phone: formatPhoneInput(e.target.value) })}
                  placeholder="(00) 0000-0000"
                  inputMode="tel"
                />
                <span className="feegow-input-icon" aria-hidden>📞</span>
              </div>
            </label>

            <label className="feegow-field">
              <span>Telefone 2</span>
              <div className="feegow-input-wrap">
                <input
                  value={form.phone2}
                  onChange={(e) => patch({ phone2: formatPhoneInput(e.target.value) })}
                  placeholder="(00) 0000-0000"
                  inputMode="tel"
                />
                <span className="feegow-input-icon" aria-hidden>📞</span>
              </div>
            </label>

            <label className="feegow-field">
              <span>Celular</span>
              <div className="feegow-input-wrap">
                <input
                  value={form.mobilePhone ?? ''}
                  onChange={(e) => patch({ mobilePhone: formatPhoneInput(e.target.value) })}
                  placeholder="(00) 00000-0000"
                  inputMode="tel"
                />
                <span className="feegow-input-icon" aria-hidden>📱</span>
              </div>
            </label>

            <label className="feegow-field">
              <span>Celular 2</span>
              <div className="feegow-input-wrap">
                <input
                  value={form.mobilePhone2}
                  onChange={(e) => patch({ mobilePhone2: formatPhoneInput(e.target.value) })}
                  placeholder="(00) 00000-0000"
                  inputMode="tel"
                />
                <span className="feegow-input-icon" aria-hidden>📱</span>
              </div>
            </label>

            <label className="feegow-field feegow-field-grow2">
              <span>E-mail</span>
              <div className="feegow-input-wrap">
                <input
                  type="email"
                  value={form.email ?? ''}
                  onChange={(e) => patch({ email: e.target.value })}
                />
                <span className="feegow-input-icon" aria-hidden>✉</span>
              </div>
            </label>

            <label className="feegow-field feegow-field-grow2">
              <span>E-mail 2</span>
              <div className="feegow-input-wrap">
                <input
                  type="email"
                  value={form.email2}
                  onChange={(e) => patch({ email2: e.target.value })}
                />
                <span className="feegow-input-icon" aria-hidden>✉</span>
              </div>
            </label>

            <label className="feegow-field">
              <span>Profissão</span>
              <input
                value={form.occupation ?? ''}
                onChange={(e) => patch({ occupation: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Escolaridade</span>
              <select
                value={form.education}
                onChange={(e) => patch({ education: e.target.value })}
              >
                <option value="">Selecione</option>
                {EDUCATION_OPTIONS.filter(Boolean).map((e) => (
                  <option key={e} value={e}>{e}</option>
                ))}
              </select>
            </label>

            <label className="feegow-field">
              <span>Naturalidade</span>
              <input
                value={form.birthPlace ?? ''}
                onChange={(e) => patch({ birthPlace: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Estado Civil</span>
              <select
                value={form.maritalStatus ?? ''}
                onChange={(e) => patch({ maritalStatus: e.target.value })}
              >
                <option value="">Selecione</option>
                {maritalOptions.map((m) => (
                  <option key={m} value={m}>{m}</option>
                ))}
              </select>
            </label>

            <label className="feegow-field">
              <span>Nacionalidade</span>
              <input
                value={form.nationality ?? ''}
                onChange={(e) => patch({ nationality: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>RG</span>
              <input
                value={form.rg ?? ''}
                onChange={(e) => patch({ rg: formatRgInput(e.target.value) })}
                placeholder="00.000.000-0"
              />
            </label>

            <label className="feegow-field">
              <span>Origem</span>
              <select
                value={form.origin}
                onChange={(e) => patch({ origin: e.target.value })}
              >
                <option value="">Selecione</option>
                {ORIGIN_OPTIONS.filter(Boolean).map((o) => (
                  <option key={o} value={o}>{o}</option>
                ))}
              </select>
            </label>

            <label className="feegow-field">
              <span>Indicação</span>
              <input
                value={form.referral}
                onChange={(e) => patch({ referral: e.target.value })}
              />
            </label>

            <label className="feegow-field">
              <span>Religião</span>
              {religionOptions.length > 0 ? (
                <select
                  value={form.religion}
                  onChange={(e) => patch({ religion: e.target.value })}
                >
                  <option value="">Selecione</option>
                  {religionOptions.map((item) => (
                    <option key={item} value={item}>{item}</option>
                  ))}
                </select>
              ) : (
                <input
                  value={form.religion}
                  onChange={(e) => patch({ religion: e.target.value })}
                />
              )}
            </label>

            <label className="feegow-field">
              <span>CNS</span>
              <input
                value={form.cns}
                onChange={(e) => {
                  patch({ cns: formatCnsInput(e.target.value) });
                  setCnsHint('');
                }}
                onBlur={() => {
                  const digits = onlyDigits(form.cns, 15);
                  if (!digits) {
                    setCnsHint('');
                    return;
                  }
                  setCnsHint(isValidCns(digits) ? 'CNS válido.' : 'CNS inválido — verifique os dígitos.');
                }}
                placeholder="000 0000 0000 0000"
                inputMode="numeric"
              />
              {cnsHint ? (
                <span className={`feegow-field-hint${cnsHint.includes('inválido') ? ' is-error' : ''}`}>{cnsHint}</span>
              ) : null}
            </label>
          </div>
            </div>
          </div>

          <div className="feegow-patient-notes-row">
            <label className="feegow-field feegow-field-notes">
              <span>Observações</span>
              <textarea
                rows={4}
                value={form.notes ?? ''}
                onChange={(e) => patch({ notes: e.target.value })}
              />
            </label>

            <label className="feegow-field feegow-field-warnings">
              <span>Avisos e Pendências</span>
              <div className="feegow-warnings-row">
                <textarea
                  rows={4}
                  value={form.warnings}
                  onChange={(e) => patch({ warnings: e.target.value })}
                />
                <span className="feegow-warnings-flag" aria-hidden>🚩</span>
              </div>
            </label>
          </div>
        </section>

        <FeegowPatientInsuranceTable
          value={form.insurances ?? []}
          onChange={(insurances) => patch({ insurances })}
        />

        {patientId && form.insurances?.[0]?.healthInsuranceId && (
          <EligibilityPanel
            patientId={patientId}
            healthInsuranceId={form.insurances[0].healthInsuranceId}
            cardNumber={form.insurances[0].cardNumber}
            compact
          />
        )}

        <FeegowPatientSchedulingTable
          value={form.schedulingPrograms ?? []}
          onChange={(schedulingPrograms) => patch({ schedulingPrograms })}
        />
      </div>

      <FeegowLegalResponsibleModal
        open={showLegalResponsibleModal}
        patientName={form.fullName}
        initialValue={form.legalResponsible}
        onClose={handleLegalResponsibleClose}
        onConfirm={handleLegalResponsibleConfirm}
      />
    </form>
  );
}

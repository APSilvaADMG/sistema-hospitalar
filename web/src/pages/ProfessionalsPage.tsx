import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  type CreateProfessionalRequest,
  type ProfessionalListDto,
  type ProfessionalDetailDto,
  type SpecialtyDto,
} from '../api/client';
import { AddressFields } from '../components/AddressFields';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation } from 'react-router-dom';
import { PersonAvatar } from '../components/PersonAvatar';
import { PhotoCapture } from '../components/PhotoCapture';
import { useAuth } from '../auth/AuthContext';

const emptyForm: CreateProfessionalRequest = {
  fullName: '',
  socialName: '',
  crm: '',
  councilUf: '',
  cpf: '',
  rg: '',
  birthDate: '',
  gender: 0,
  email: '',
  phone: '',
  mobilePhone: '',
  addressStreet: '',
  addressNumber: '',
  addressComplement: '',
  addressNeighborhood: '',
  addressCity: '',
  addressState: '',
  addressZipCode: '',
  notes: '',
  photoData: undefined,
  specialtyId: '',
};

function detailToForm(d: ProfessionalDetailDto): CreateProfessionalRequest & { isActive: boolean } {
  return {
    fullName: d.fullName,
    socialName: d.socialName ?? '',
    crm: d.crm ?? '',
    councilUf: d.councilUf ?? '',
    cpf: d.cpf ?? '',
    rg: d.rg ?? '',
    birthDate: d.birthDate ?? '',
    gender: d.gender,
    email: d.email ?? '',
    phone: d.phone ?? '',
    mobilePhone: d.mobilePhone ?? '',
    addressStreet: d.addressStreet ?? '',
    addressNumber: d.addressNumber ?? '',
    addressComplement: d.addressComplement ?? '',
    addressNeighborhood: d.addressNeighborhood ?? '',
    addressCity: d.addressCity ?? '',
    addressState: d.addressState ?? '',
    addressZipCode: d.addressZipCode ?? '',
    notes: d.notes ?? '',
    photoData: d.photoData,
    specialtyId: d.specialtyId,
    isActive: d.isActive,
  };
}

export function ProfessionalsPage() {
  const { hasPermission } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [items, setItems] = useState<ProfessionalListDto[]>([]);
  const [specialties, setSpecialties] = useState<SpecialtyDto[]>([]);
  const [search, setSearch] = useState('');
  const [form, setForm] = useState(emptyForm);
  const [isActive, setIsActive] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showModal, setShowModal] = useState(false);

  async function load() {
    const [list, specs] = await Promise.all([api.getProfessionalList(), api.getSpecialties()]);
    setItems(list);
    setSpecialties(specs);
  }

  useEffect(() => { load().catch(console.error); }, []);

  const filtered = items.filter((p) => {
    if (!search.trim()) return true;
    const term = search.toLowerCase();
    return p.fullName.toLowerCase().includes(term)
      || (p.crm?.toLowerCase().includes(term) ?? false)
      || p.specialtyName.toLowerCase().includes(term);
  });

  function openCreate() {
    setEditingId(null);
    setForm(emptyForm);
    setIsActive(true);
    setShowModal(true);
  }

  async function openEdit(id: string) {
    const detail = await api.getProfessional(id);
    const mapped = detailToForm(detail);
    setEditingId(id);
    setForm(mapped);
    setIsActive(mapped.isActive);
    setShowModal(true);
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');

    const payload: CreateProfessionalRequest = {
      ...form,
      specialtyId: form.specialtyId,
      birthDate: form.birthDate || undefined,
      photoData: form.photoData || undefined,
      socialName: form.socialName || undefined,
      crm: form.crm || undefined,
      councilUf: form.councilUf || undefined,
      cpf: form.cpf || undefined,
      rg: form.rg || undefined,
      email: form.email || undefined,
      phone: form.phone || undefined,
      mobilePhone: form.mobilePhone || undefined,
      addressStreet: form.addressStreet || undefined,
      addressNumber: form.addressNumber || undefined,
      addressComplement: form.addressComplement || undefined,
      addressNeighborhood: form.addressNeighborhood || undefined,
      addressCity: form.addressCity || undefined,
      addressState: form.addressState || undefined,
      addressZipCode: form.addressZipCode || undefined,
      notes: form.notes || undefined,
    };

    try {
      if (editingId) {
        await api.updateProfessional(editingId, { ...payload, isActive });
        setSuccess('Profissional atualizado.');
      } else {
        await api.createProfessional(payload);
        setSuccess('Profissional cadastrado.');
      }
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.');
    }
  }

  if (!hasPermission('patients.create', 'reports.read')) {
    return <div className="card">Acesso restrito.</div>;
  }

  return (
    <>
      <PageHeader eyebrow="Cadastros" title={breadcrumb.title || 'Profissionais / Médicos'} subtitle="Cadastro completo de médicos e profissionais de saúde com foto e documentação.">
        <button className="btn" type="button" onClick={openCreate}>+ Novo profissional</button>
      </PageHeader>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Profissionais" value={items.length} variant="primary" />
        <KpiCard label="Com foto" value={items.filter((p) => p.hasPhoto).length} variant="info" />
        <KpiCard label="Especialidades" value={specialties.length} variant="neutral" />
      </div>

      <div className="card-panel appt-panel">
        <FilterBar>
          <div className="filter-field grow-lg">
            <label htmlFor="profSearch">Buscar</label>
            <input id="profSearch" placeholder="Nome, CRM ou especialidade..." value={search} onChange={(e) => setSearch(e.target.value)} />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr><th>Foto</th><th>Nome</th><th>CRM</th><th>UF</th><th>Especialidade</th><th>Ações</th></tr>
            </thead>
            <tbody>
              {filtered.map((p) => (
                <tr key={p.id}>
                  <td><PersonAvatar name={p.fullName} size={36} /></td>
                  <td><strong>{p.fullName}</strong></td>
                  <td>{p.crm ?? '—'}</td>
                  <td>{p.councilUf ?? '—'}</td>
                  <td>{p.specialtyName}</td>
                  <td><button className="btn btn-secondary btn-sm" type="button" onClick={() => openEdit(p.id)}>Editar</button></td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr><td colSpan={6} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>Nenhum profissional encontrado.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal open={showModal} onClose={() => setShowModal(false)} title={editingId ? 'Editar profissional' : 'Novo profissional'} subtitle="Dados profissionais e pessoais." width="lg">
        <form className="form-grid" onSubmit={handleSubmit}>
          <div className="form-field full">
            <div className="form-section-title">Foto</div>
            <PhotoCapture name={form.fullName || 'Profissional'} value={form.photoData} onChange={(photoData) => setForm({ ...form, photoData: photoData ?? undefined })} />
          </div>
          <div className="form-field full"><div className="form-section-title">Dados profissionais</div></div>
          <div className="form-field"><label>Nome completo *</label><input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></div>
          <div className="form-field"><label>Nome social</label><input value={form.socialName} onChange={(e) => setForm({ ...form, socialName: e.target.value })} /></div>
          <div className="form-field">
            <label>Especialidade *</label>
            <select required value={form.specialtyId} onChange={(e) => setForm({ ...form, specialtyId: e.target.value })}>
              <option value="">Selecione</option>
              {specialties.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
            </select>
          </div>
          <div className="form-field"><label>CRM</label><input value={form.crm} onChange={(e) => setForm({ ...form, crm: e.target.value })} /></div>
          <div className="form-field"><label>UF do conselho</label><input maxLength={2} value={form.councilUf} onChange={(e) => setForm({ ...form, councilUf: e.target.value.toUpperCase() })} /></div>
          <div className="form-field full"><div className="form-section-title">Documentos e dados pessoais</div></div>
          <div className="form-field"><label>CPF</label><input value={form.cpf} onChange={(e) => setForm({ ...form, cpf: e.target.value })} /></div>
          <div className="form-field"><label>RG</label><input value={form.rg} onChange={(e) => setForm({ ...form, rg: e.target.value })} /></div>
          <div className="form-field"><label>Data de nascimento</label><input type="date" value={form.birthDate} onChange={(e) => setForm({ ...form, birthDate: e.target.value })} /></div>
          <div className="form-field">
            <label>Sexo</label>
            <select value={form.gender} onChange={(e) => setForm({ ...form, gender: Number(e.target.value) })}>
              <option value={0}>Não informado</option>
              <option value={1}>Masculino</option>
              <option value={2}>Feminino</option>
              <option value={3}>Outro</option>
            </select>
          </div>
          <div className="form-field full"><div className="form-section-title">Contato</div></div>
          <div className="form-field"><label>E-mail</label><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} /></div>
          <div className="form-field"><label>Telefone</label><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
          <div className="form-field"><label>Celular</label><input value={form.mobilePhone} onChange={(e) => setForm({ ...form, mobilePhone: e.target.value })} /></div>
          <div className="form-field full"><div className="form-section-title">Endereço</div></div>
          <AddressFields values={form} onChange={(patch) => setForm({ ...form, ...patch })} prefix="prof-" />
          <div className="form-field full"><label>Observações</label><textarea rows={3} value={form.notes} onChange={(e) => setForm({ ...form, notes: e.target.value })} /></div>
          {editingId && (
            <div className="form-field full checkbox">
              <label>
                <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
                Cadastro ativo
              </label>
            </div>
          )}
          <div className="form-field full modal-actions">
            <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
            <button className="btn" type="submit">Salvar</button>
          </div>
        </form>
      </Modal>
    </>
  );
}

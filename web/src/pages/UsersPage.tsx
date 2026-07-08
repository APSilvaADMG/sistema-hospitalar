import { useEffect, useState, type FormEvent } from 'react';
import {
  api,
  roleLabels,
  type CreateUserRequest,
  type PatientDto,
  type ProfessionalListDto,
  type UserListDto,
  type UserRoleName,
} from '../api/client';
import { FilterBar } from '../components/FilterBar';
import { KpiCard } from '../components/KpiCard';
import { Modal } from '../components/Modal';
import { PageHeader } from '../components/PageHeader';
import { findMenuBreadcrumb } from '../navigation/sidebarMenu';
import { useLocation, useSearchParams } from 'react-router-dom';
import { PersonAvatar } from '../components/PersonAvatar';
import { formatBrDateTime } from '../utils/dateUtils';
import { useAuth } from '../auth/AuthContext';

const roleOptions: UserRoleName[] = ['Admin', 'Reception', 'Doctor', 'Patient'];

const emptyForm: CreateUserRequest = {
  fullName: '',
  email: '',
  password: '',
  role: 'Reception',
};

export function UsersPage() {
  const { hasPermission, user: currentUser } = useAuth();
  const { pathname } = useLocation();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const [items, setItems] = useState<UserListDto[]>([]);
  const [professionals, setProfessionals] = useState<ProfessionalListDto[]>([]);
  const [patientResults, setPatientResults] = useState<PatientDto[]>([]);
  const [patientSearch, setPatientSearch] = useState('');
  const [search, setSearch] = useState('');
  const [form, setForm] = useState(emptyForm);
  const [isActive, setIsActive] = useState(true);
  const [newPassword, setNewPassword] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [showModal, setShowModal] = useState(false);
  const [searchParams, setSearchParams] = useSearchParams();

  async function load() {
    const [users, profs] = await Promise.all([api.getUsers(), api.getProfessionalList()]);
    setItems(users);
    setProfessionals(profs);
  }

  useEffect(() => {
    if (!hasPermission('users.manage')) return;
    load().catch(console.error);
  }, [hasPermission]);

  useEffect(() => {
    if (!hasPermission('users.manage') || form.role !== 'Patient') return;
    const term = patientSearch.trim();
    if (term.length < 2) {
      setPatientResults([]);
      return;
    }
    const timer = window.setTimeout(() => {
      api.getPatients(term, 1).then((r) => setPatientResults(r.items)).catch(console.error);
    }, 300);
    return () => window.clearTimeout(timer);
  }, [hasPermission, form.role, patientSearch]);

  const filtered = items.filter((u) => {
    if (!search.trim()) return true;
    const term = search.toLowerCase();
    return u.fullName.toLowerCase().includes(term)
      || u.email.toLowerCase().includes(term)
      || (roleLabels[u.role]?.toLowerCase().includes(term) ?? false);
  });

  function openCreate() {
    setEditingId(null);
    setForm(emptyForm);
    setIsActive(true);
    setNewPassword('');
    setPatientSearch('');
    setPatientResults([]);
    setShowModal(true);
  }

  async function openEdit(id: string) {
    const detail = await api.getUser(id);
    setEditingId(id);
    setForm({
      fullName: detail.fullName,
      email: detail.email,
      password: '',
      role: detail.role,
      professionalId: detail.professionalId,
      patientId: detail.patientId,
    });
    setIsActive(detail.isActive);
    setNewPassword('');
    setPatientSearch(detail.patientName ?? '');
    setPatientResults([]);
    setShowModal(true);
  }

  useEffect(() => {
    if (!hasPermission('users.manage') || items.length === 0) return;
    const userId = searchParams.get('usuario');
    if (userId) {
      void openEdit(userId);
      const next = new URLSearchParams(searchParams);
      next.delete('usuario');
      setSearchParams(next, { replace: true });
      return;
    }
    if (searchParams.get('novo') === '1') {
      const role = searchParams.get('role') as UserRoleName | null;
      openCreate();
      if (role && role in roleLabels) {
        setForm((prev) => ({ ...prev, role }));
      }
      const next = new URLSearchParams(searchParams);
      next.delete('novo');
      next.delete('role');
      setSearchParams(next, { replace: true });
    }
  }, [hasPermission, items, searchParams, setSearchParams]);

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    setSuccess('');

    const links = {
      professionalId: form.role === 'Doctor' ? form.professionalId || undefined : undefined,
      patientId: form.role === 'Patient' ? form.patientId || undefined : undefined,
    };

    try {
      if (editingId) {
        await api.updateUser(editingId, {
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          role: form.role,
          isActive,
          ...links,
        });
        if (newPassword.trim()) {
          await api.resetUserPassword(editingId, newPassword.trim());
        }
        setSuccess('Usuário atualizado.');
      } else {
        await api.createUser({
          fullName: form.fullName.trim(),
          email: form.email.trim(),
          password: form.password,
          role: form.role,
          ...links,
        });
        setSuccess('Usuário cadastrado.');
      }
      setShowModal(false);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar.');
    }
  }

  if (!hasPermission('users.manage')) {
    return <div className="card">Acesso restrito ao administrador.</div>;
  }

  const activeCount = items.filter((u) => u.isActive).length;

  return (
    <>
      <PageHeader
        eyebrow="Administrativo"
        title={breadcrumb.title || 'Usuários e acessos'}
        subtitle="Cadastre quem pode entrar no sistema e defina o perfil de cada pessoa."
      >
        <button className="btn" type="button" onClick={openCreate}>+ Novo usuário</button>
      </PageHeader>

      {error && <div className="alert alert-error">{error}</div>}
      {success && <div className="alert alert-success">{success}</div>}

      <div className="kpi-grid">
        <KpiCard label="Usuários" value={items.length} variant="primary" />
        <KpiCard label="Ativos" value={activeCount} variant="info" />
        <KpiCard label="Inativos" value={items.length - activeCount} variant="neutral" />
      </div>

      <div className="card-panel appt-panel">
        <FilterBar>
          <div className="filter-field grow-lg">
            <label htmlFor="userSearch">Buscar</label>
            <input
              id="userSearch"
              placeholder="Nome, e-mail ou perfil..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
        </FilterBar>
        <div className="card-panel-body" style={{ padding: 0 }}>
          <table className="data-table">
            <thead>
              <tr>
                <th>Usuário</th>
                <th>E-mail</th>
                <th>Perfil</th>
                <th>Vínculo</th>
                <th>Status</th>
                <th>Cadastro</th>
                <th>Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((u) => (
                <tr key={u.id}>
                  <td>
                    <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
                      <PersonAvatar name={u.fullName} size={32} />
                      <strong>{u.fullName}</strong>
                      {u.id === currentUser?.userId && (
                        <span className="badge badge-info">Você</span>
                      )}
                    </div>
                  </td>
                  <td>{u.email}</td>
                  <td>{roleLabels[u.role] ?? u.role}</td>
                  <td>{u.professionalName ?? u.patientName ?? '—'}</td>
                  <td>
                    <span className={`badge ${u.isActive ? 'badge-success' : 'badge-neutral'}`}>
                      {u.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td>{formatBrDateTime(u.createdAt)}</td>
                  <td>
                    <button className="btn btn-secondary btn-sm" type="button" onClick={() => openEdit(u.id)}>
                      Editar
                    </button>
                  </td>
                </tr>
              ))}
              {filtered.length === 0 && (
                <tr>
                  <td colSpan={7} style={{ textAlign: 'center', padding: 28, color: 'var(--muted)' }}>
                    Nenhum usuário encontrado.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      <Modal
        open={showModal}
        title={editingId ? 'Editar usuário' : 'Novo usuário'}
        subtitle="Defina e-mail, perfil de acesso e vínculos do usuário."
        onClose={() => setShowModal(false)}
      >
        <form onSubmit={handleSubmit} className="form-grid">
            <div className="form-field">
              <label htmlFor="userFullName">Nome completo</label>
              <input
                id="userFullName"
                required
                value={form.fullName}
                onChange={(e) => setForm({ ...form, fullName: e.target.value })}
              />
            </div>
            <div className="form-field">
              <label htmlFor="userEmail">E-mail de acesso</label>
              <input
                id="userEmail"
                type="email"
                required
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </div>
            {!editingId && (
              <div className="form-field">
                <label htmlFor="userPassword">Senha inicial</label>
                <input
                  id="userPassword"
                  type="password"
                  required
                  minLength={6}
                  value={form.password}
                  onChange={(e) => setForm({ ...form, password: e.target.value })}
                />
              </div>
            )}
            {editingId && (
              <div className="form-field">
                <label htmlFor="userNewPassword">Nova senha (opcional)</label>
                <input
                  id="userNewPassword"
                  type="password"
                  minLength={6}
                  placeholder="Deixe em branco para manter a atual"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                />
              </div>
            )}
            <div className="form-field">
              <label htmlFor="userRole">Perfil de acesso</label>
              <select
                id="userRole"
                value={form.role}
                onChange={(e) => setForm({
                  ...form,
                  role: e.target.value as UserRoleName,
                  professionalId: undefined,
                  patientId: undefined,
                })}
              >
                {roleOptions.map((role) => (
                  <option key={role} value={role}>{roleLabels[role]}</option>
                ))}
              </select>
            </div>
            {form.role === 'Doctor' && (
              <div className="form-field">
                <label htmlFor="userProfessional">Profissional vinculado</label>
                <select
                  id="userProfessional"
                  required
                  value={form.professionalId ?? ''}
                  onChange={(e) => setForm({ ...form, professionalId: e.target.value || undefined })}
                >
                  <option value="">Selecione...</option>
                  {professionals.map((p) => (
                    <option key={p.id} value={p.id}>{p.fullName}</option>
                  ))}
                </select>
              </div>
            )}
            {form.role === 'Patient' && (
              <div className="form-field">
                <label htmlFor="userPatientSearch">Paciente vinculado</label>
                <input
                  id="userPatientSearch"
                  placeholder="Digite nome ou CPF para buscar..."
                  value={patientSearch}
                  onChange={(e) => setPatientSearch(e.target.value)}
                />
                {form.patientId && (
                  <small style={{ color: 'var(--muted)' }}>
                    Selecionado: {patientResults.find((p) => p.id === form.patientId)?.fullName ?? (patientSearch || 'Paciente vinculado')}
                  </small>
                )}
                {patientResults.length > 0 && (
                  <div className="card" style={{ marginTop: 8, padding: 8, maxHeight: 160, overflow: 'auto' }}>
                    {patientResults.map((p) => (
                      <button
                        key={p.id}
                        type="button"
                        className="btn btn-secondary btn-sm"
                        style={{ display: 'block', width: '100%', marginBottom: 4, textAlign: 'left' }}
                        onClick={() => {
                          setForm({ ...form, patientId: p.id });
                          setPatientSearch(p.fullName);
                          setPatientResults([]);
                        }}
                      >
                        {p.fullName} {p.cpf ? `· ${p.cpf}` : ''}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            )}
            {editingId && (
              <div className="form-field">
                <label htmlFor="userActive">
                  <input
                    id="userActive"
                    type="checkbox"
                    checked={isActive}
                    disabled={editingId === currentUser?.userId}
                    onChange={(e) => setIsActive(e.target.checked)}
                  />
                  {' '}Usuário ativo
                </label>
                {editingId === currentUser?.userId && (
                  <small style={{ color: 'var(--muted)' }}>Você não pode desativar o seu próprio acesso.</small>
                )}
              </div>
            )}
            <div className="form-actions">
              <button className="btn btn-secondary" type="button" onClick={() => setShowModal(false)}>Cancelar</button>
              <button className="btn" type="submit">{editingId ? 'Salvar' : 'Cadastrar'}</button>
            </div>
          </form>
      </Modal>
    </>
  );
}

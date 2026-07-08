import { type FormEvent, useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { AppearanceSettings } from '../../components/AppearanceSettings';
import { IntegrationStatusPanel } from '../../components/integrations/IntegrationStatusPanel';
import {
  HOSPITAL_PARAMS_KEY,
  loadHospitalParams,
  loadLayoutPrefs,
  type HospitalParams,
  type LayoutPrefs,
} from '../../config/clinicOnDoctorProfile';
import { ModuleNav } from '../../components/ModuleNav';
import { PageHeader } from '../../components/PageHeader';
import { configTabs } from '../../navigation/moduleSections';
import { findMenuBreadcrumb } from '../../navigation/sidebarMenu';
import { useModuleSection } from '../../navigation/useModuleSection';

const LAYOUT_PREFS_KEY = 'hms-layout-prefs';

const AUX_CADASTROS = [
  { label: 'Pacientes', path: '/recepcao/pacientes', desc: 'Cadastro completo de pacientes' },
  { label: 'Responsáveis', path: '/recepcao/pacientes/responsaveis', desc: 'Responsáveis legais e contatos de emergência' },
  { label: 'Convênios', path: '/convenios', desc: 'Operadoras e planos de saúde' },
  { label: 'Profissionais', path: '/profissionais', desc: 'Médicos e equipe assistencial' },
  { label: 'Consultórios / Salas', path: '/ambulatorio/consultorios', desc: 'Salas e agendas' },
  { label: 'Usuários do sistema', path: '/usuarios', desc: 'Contas e perfis de acesso' },
  { label: 'Central de Ajuda', path: '/ajuda', desc: 'FAQ, manuais, treinamentos e suporte' },
  { label: 'Catálogo Hospitalar', path: '/configuracoes/catalogo-hospitalar', desc: 'Referência ERP — setores, alas, exames, menu e perfis' },
  { label: 'Regras de negócio', path: '/configuracoes/regras-negocio', desc: 'Catálogo RN-xxx' },
  { label: 'Catálogo hospitalar ERP', path: '/configuracoes/catalogo-hospitalar', desc: 'Tipos de usuário, setores, menu, exames e perfis' },
  { label: 'Produtos / estoque', path: '/estoque', desc: 'Medicamentos e insumos' },
  { label: 'Vias de administração', path: '/pep/vias-administracao', desc: 'Catálogo clínico MADRE (oral, IV, IM, etc.)' },
];

function loadParams(): HospitalParams {
  return loadHospitalParams();
}

export function ConfigHubPage() {
  const { pathname } = useLocation();
  const navigate = useNavigate();
  const breadcrumb = findMenuBreadcrumb(pathname);
  const { section } = useModuleSection('/configuracoes');
  const active = section || 'parametros';

  useEffect(() => {
    if (pathname === '/configuracoes' || pathname === '/configuracoes/') {
      navigate('/configuracoes/parametros', { replace: true });
      return;
    }
    if (pathname === '/configuracoes/primeiros-passos') {
      navigate('/configuracoes/parametros', { replace: true });
    }
  }, [pathname, navigate]);

  const [params, setParams] = useState<HospitalParams>(loadParams);
  const [layout, setLayout] = useState<LayoutPrefs>(loadLayoutPrefs);
  const [msg, setMsg] = useState('');

  function saveParams(e: FormEvent) {
    e.preventDefault();
    localStorage.setItem(HOSPITAL_PARAMS_KEY, JSON.stringify(params));
    setMsg('Parâmetros salvos. O nome institucional aparece no topo após recarregar a página.');
  }

  return (
    <>
      <PageHeader
        eyebrow="Configurações"
        title={breadcrumb.title}
        subtitle="Parâmetros institucionais, cadastros auxiliares, APIs e layout de impressão."
      />

      <ModuleNav basePath="/configuracoes" tabs={configTabs} />

      {msg && <div className="alert alert-success" style={{ marginTop: 12 }}>{msg}</div>}

      {active === 'aparencia' && <AppearanceSettings />}

      {active === 'parametros' && (
        <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={saveParams}>
          <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Parâmetros gerais do hospital</h3>
          <div className="form-field"><label>Nome institucional</label>
            <input value={params.hospitalName} onChange={(e) => setParams({ ...params, hospitalName: e.target.value })} placeholder="Nome da clínica ou hospital" required />
          </div>
          <div className="form-field"><label>CNES</label>
            <input value={params.cnes} onChange={(e) => setParams({ ...params, cnes: e.target.value })} placeholder="7 dígitos" />
          </div>
          <div className="form-field"><label>CNPJ</label>
            <input value={params.cnpj} onChange={(e) => setParams({ ...params, cnpj: e.target.value })} />
          </div>
          <div className="form-field"><label>Fuso horário</label>
            <select value={params.timezone} onChange={(e) => setParams({ ...params, timezone: e.target.value })}>
              <option value="America/Sao_Paulo">America/Sao_Paulo (Brasília)</option>
              <option value="America/Manaus">America/Manaus</option>
              <option value="America/Belem">America/Belem</option>
            </select>
          </div>
          <div className="form-field"><label>Idioma padrão</label>
            <select value={params.defaultLocale} onChange={(e) => setParams({ ...params, defaultLocale: e.target.value })}>
              <option value="pt-BR">Português (Brasil)</option>
              <option value="en-US">English (US)</option>
            </select>
          </div>
          <div className="form-field"><label>Duração padrão de consulta (min)</label>
            <input type="number" min={10} max={120} value={params.appointmentSlotMinutes}
              onChange={(e) => setParams({ ...params, appointmentSlotMinutes: Number(e.target.value) })} />
          </div>
          <div className="form-field align-end">
            <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input type="checkbox" checked={params.autoGenerateMrn}
                onChange={(e) => setParams({ ...params, autoGenerateMrn: e.target.checked })} />
              Gerar prontuário (MRN) automaticamente
            </label>
          </div>
          <h4 style={{ gridColumn: '1 / -1', margin: '8px 0 0' }}>Módulos visíveis no menu</h4>
          <p className="form-hint" style={{ gridColumn: '1 / -1', margin: 0 }}>
            Oculta áreas do menu e atalhos quando desligado.
          </p>
          {([
            ['financial', 'Financeiro'],
            ['billing', 'Faturamento / TISS'],
            ['inventory', 'Estoque e farmácia'],
            ['bi', 'BI e relatórios'],
            ['marketing', 'Marketing'],
          ] as const).map(([key, label]) => (
            <div key={key} className="form-field">
              <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <input
                  type="checkbox"
                  checked={params.modules[key]}
                  onChange={(e) => setParams({
                    ...params,
                    modules: { ...params.modules, [key]: e.target.checked },
                  })}
                />
                {label}
              </label>
            </div>
          ))}
          <div className="form-actions">
            <button className="btn" type="submit">Salvar parâmetros</button>
            <Link to="/ajuda" className="btn btn-secondary">Central de Ajuda</Link>
            <Link to="/configuracoes/regras-negocio" className="btn btn-secondary">Regras de negócio</Link>
          </div>
        </form>
      )}

      {active === 'cadastros' && (
        <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
          <div className="card-panel-header">Cadastros auxiliares</div>
          <table className="data-table">
            <thead><tr><th>Módulo</th><th>Descrição</th><th /></tr></thead>
            <tbody>
              {AUX_CADASTROS.map((item) => (
                <tr key={item.path}>
                  <td><strong>{item.label}</strong></td>
                  <td>{item.desc}</td>
                  <td><Link to={item.path} className="btn btn-secondary btn-sm">Abrir</Link></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {active === 'integracoes' && (
        <div style={{ marginTop: 16 }}>
          <IntegrationStatusPanel />
        </div>
      )}

      {active === 'layout' && (
        <form className="card form-grid" style={{ marginTop: 16 }} onSubmit={(e) => {
          e.preventDefault();
          localStorage.setItem(LAYOUT_PREFS_KEY, JSON.stringify(layout));
          setMsg('Preferências de layout salvas.');
        }}>
          <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Layout e impressões</h3>
          {[
            ['showLogoOnLabels', 'Exibir logotipo em etiquetas de paciente'],
            ['showLogoOnWristband', 'Exibir logotipo na pulseira'],
            ['wristbandBarcode', 'Código de barras na pulseira'],
            ['visitorBadgePhoto', 'Foto no crachá de visitante'],
          ].map(([key, label]) => (
            <div key={key} className="form-field full">
              <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                <input
                  type="checkbox"
                  checked={layout[key as keyof typeof layout] as boolean}
                  onChange={(e) => setLayout({ ...layout, [key]: e.target.checked })}
                />
                {label}
              </label>
            </div>
          ))}
          <div className="form-field full">
            <label>Rodapé de relatórios impressos</label>
            <input value={layout.reportFooter} onChange={(e) => setLayout({ ...layout, reportFooter: e.target.value })} />
          </div>
          <div className="form-actions">
            <button className="btn" type="submit">Salvar layout</button>
            <Link to="/pacientes" className="btn btn-secondary">Testar etiqueta</Link>
          </div>
        </form>
      )}
    </>
  );
}

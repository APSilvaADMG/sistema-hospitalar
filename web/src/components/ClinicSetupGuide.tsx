import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  applyBayannoProfile,
  applyOnDoctorClinicProfile,
  bayannoChecklistDone,
  onDoctorChecklistDone,
} from '../config/clinicOnDoctorProfile';
import { useAppearance } from '../theme/AppearanceProvider';

const CHECKLIST_KEY = 'hms-setup-checklist';

type SetupStep = {
  id: string;
  ondoctor: string;
  title: string;
  description: string;
  path: string;
};

const SETUP_STEPS: SetupStep[] = [
  {
    id: 'env',
    ondoctor: '—',
    title: 'Subir o ambiente',
    description: 'API em :8080 e frontend em :5173. Login admin: admin@hospital.local / Admin123!',
    path: '/',
  },
  {
    id: 'empresa',
    ondoctor: 'Dados da Empresa',
    title: 'Parâmetros institucionais',
    description: 'Nome da clínica, CNES, CNPJ, fuso horário e duração padrão da consulta (30 min).',
    path: '/configuracoes/parametros',
  },
  {
    id: 'aparencia',
    ondoctor: 'Personalizar',
    title: 'Visual estilo Bayanno (SGHC)',
    description: 'Perfil Bayanno · Sidebar teal escura, topbar escura e tiles no dashboard.',
    path: '/configuracoes/aparencia',
  },
  {
    id: 'equipe',
    ondoctor: 'Dados da Equipe',
    title: 'Profissionais e usuários',
    description: 'Cadastre médicos (CRM, especialidade) e contas de recepção/médico com perfil de acesso.',
    path: '/profissionais',
  },
  {
    id: 'convenios',
    ondoctor: 'Convênios',
    title: 'Operadoras e planos',
    description: 'Particular, SUS e convênios privados. O seed já traz exemplos — revise e inclua os seus.',
    path: '/convenios',
  },
  {
    id: 'salas',
    ondoctor: 'Salas',
    title: 'Consultórios e salas',
    description: 'Salas físicas e vínculo com agenda de cada profissional (ex.: Sala 1).',
    path: '/ambulatorio/consultorios',
  },
  {
    id: 'procedimentos',
    ondoctor: 'Procedimentos',
    title: 'Tabela de procedimentos / TISS',
    description: 'Catálogo TUSS/CBHPM para faturamento e guias. Seed expandido via API TISS.',
    path: '/faturamento',
  },
  {
    id: 'exames',
    ondoctor: 'Exames',
    title: 'Exames laboratoriais e imagem',
    description: 'Catálogo de exames e fluxo de solicitação no PEP e hubs de laboratório/imagem.',
    path: '/laboratorio',
  },
  {
    id: 'agenda',
    ondoctor: 'Agenda',
    title: 'Agenda do dia',
    description: 'Timeline vertical por profissional, check-in na recepção e status do atendimento.',
    path: '/recepcao/agendamentos',
  },
  {
    id: 'pacientes',
    ondoctor: 'Pessoas / Clientes',
    title: 'Cadastro de pacientes',
    description: 'Pacientes, responsáveis, documentos e prontuário eletrônico.',
    path: '/recepcao/pacientes',
  },
  {
    id: 'impressao',
    ondoctor: 'Impressão',
    title: 'Layout e impressões',
    description: 'Etiquetas, pulseira, crachá de visitante e rodapé de relatórios.',
    path: '/configuracoes/layout',
  },
  {
    id: 'financeiro',
    ondoctor: 'Financeiro',
    title: 'Receitas, despesas e faturamento',
    description: 'Contas a receber, produção por convênio e fechamento financeiro.',
    path: '/faturamento',
  },
];

function loadChecked(): Record<string, boolean> {
  try {
    const raw = localStorage.getItem(CHECKLIST_KEY);
    return raw ? (JSON.parse(raw) as Record<string, boolean>) : {};
  } catch {
    return {};
  }
}

export function ClinicSetupGuide() {
  const { updateAppearance } = useAppearance();
  const [checked, setChecked] = useState<Record<string, boolean>>(loadChecked);
  const [applyMsg, setApplyMsg] = useState('');

  useEffect(() => {
    localStorage.setItem(CHECKLIST_KEY, JSON.stringify(checked));
  }, [checked]);

  const done = SETUP_STEPS.filter((s) => checked[s.id]).length;
  const total = SETUP_STEPS.length;
  const pct = Math.round((done / total) * 100);

  function handleApplyBayanno() {
    applyBayannoProfile();
    updateAppearance({
      density: 'comfortable',
      showTestBanner: false,
      bannerMessage: '',
    });
    setChecked(bayannoChecklistDone);
    setApplyMsg('Perfil institucional aplicado (layout Feegow). Recarregue a página se algo não atualizar.');
  }

  function handleApplyOnDoctor() {
    applyOnDoctorClinicProfile();
    updateAppearance({
      density: 'comfortable',
      showTestBanner: false,
      bannerMessage: '',
    });
    setChecked(onDoctorChecklistDone);
    setApplyMsg('Perfil Clínica (OnDoctor) aplicado com layout Feegow. Recarregue a página se algo não atualizar.');
  }

  return (
    <div className="clinic-setup-guide">
      {applyMsg && <div className="alert alert-success" style={{ marginTop: 16 }}>{applyMsg}</div>}

      <div className="card" style={{ marginTop: 16 }}>
        <h3 style={{ margin: '0 0 8px' }}>Configuração inicial — mapa OnDoctor → APSMedCore</h3>
        <p className="form-hint" style={{ margin: '0 0 12px' }}>
          Referência extraída do snapshot OnDoctor (menu Configurações). Marque cada etapa conforme concluir.
          Progresso: <strong>{done}/{total}</strong> ({pct}%).
        </p>
        <div className="form-actions" style={{ marginBottom: 12 }}>
          <button type="button" className="btn" onClick={handleApplyBayanno}>
            Aplicar perfil Bayanno agora
          </button>
          <button type="button" className="btn btn-secondary" onClick={handleApplyOnDoctor}>
            Perfil Clínica (OnDoctor)
          </button>
          <Link to="/recepcao/agendamentos" className="btn btn-secondary">
            Ver agenda
          </Link>
        </div>
        <div className="clinic-setup-progress" aria-hidden>
          <span style={{ width: `${pct}%` }} />
        </div>
      </div>

      <div className="card-panel appt-panel" style={{ marginTop: 16 }}>
        <div className="card-panel-header">Checklist de implantação</div>
        <table className="data-table">
          <thead>
            <tr>
              <th style={{ width: 40 }} />
              <th>OnDoctor</th>
              <th>No APSMedCore</th>
              <th>Descrição</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {SETUP_STEPS.map((step) => (
              <tr key={step.id} className={checked[step.id] ? 'clinic-setup-done' : ''}>
                <td>
                  <input
                    type="checkbox"
                    checked={!!checked[step.id]}
                    onChange={(e) => setChecked({ ...checked, [step.id]: e.target.checked })}
                    aria-label={`Concluído: ${step.title}`}
                  />
                </td>
                <td><span className="clinic-setup-ondoctor">{step.ondoctor}</span></td>
                <td><strong>{step.title}</strong></td>
                <td style={{ color: 'var(--muted)', fontSize: '0.84rem' }}>{step.description}</td>
                <td>
                  <Link to={step.path} className="btn btn-secondary btn-sm">Abrir</Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="card form-grid" style={{ marginTop: 16 }}>
        <h3 style={{ gridColumn: '1 / -1', margin: 0 }}>Valores recomendados (perfil clínica ambulatorial)</h3>
        <ul className="bi-progress-list" style={{ gridColumn: '1 / -1', margin: 0 }}>
          <li><strong>Nome institucional:</strong> nome fantasia exibido no topo (como no OnDoctor)</li>
          <li><strong>Consulta:</strong> 30 minutos por slot</li>
          <li><strong>Fuso:</strong> America/Sao_Paulo</li>
          <li><strong>Aparência:</strong> Clínica · Claro · Menu só ícones · Sidebar escura teal</li>
          <li><strong>Usuários demo já no banco:</strong> recepcao@, medico@, admin@ (ver README)</li>
        </ul>
      </div>
    </div>
  );
}

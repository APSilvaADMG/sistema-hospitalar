import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { HospitalLogo } from '../components/HospitalLogo';
import { useAuth } from '../auth/AuthContext';

const HERO_SLIDES = [
  {
    title: 'Prontuário eletrônico: menos papel, mais segurança',
    text: 'Dados organizados, protegidos e acessíveis para toda a equipe assistencial — do ambulatório à internação.',
  },
  {
    title: 'Visão operacional em tempo real',
    text: 'Dashboard com ocupação de leitos, filas de atendimento e indicadores para a gestão do dia.',
  },
  {
    title: 'Jornada completa do paciente',
    text: 'Do agendamento à alta: histórico clínico, prescrições, exames e faturamento em uma única plataforma.',
  },
  {
    title: 'Operação hospitalar conectada',
    text: 'Emergência, UTI, centro cirúrgico, laboratório, farmácia e faturamento TISS integrados.',
  },
] as const;

function getTimeGreeting(): string {
  const hour = new Date().getHours();
  if (hour < 12) return 'Bom dia';
  if (hour < 18) return 'Boa tarde';
  return 'Boa noite';
}

export function LoginPage() {
  const { login, verifyMfa, isLoading, token, user, mfaPending, cancelMfa, authReady } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@hospital.local');
  const [password, setPassword] = useState('Admin123!');
  const [mfaCode, setMfaCode] = useState('');
  const [error, setError] = useState('');
  const [slideIndex, setSlideIndex] = useState(0);

  const greeting = useMemo(() => getTimeGreeting(), []);

  useEffect(() => {
    const interval = window.setInterval(() => {
      setSlideIndex((current) => (current + 1) % HERO_SLIDES.length);
    }, 6000);
    return () => window.clearInterval(interval);
  }, []);

  const slide = HERO_SLIDES[slideIndex];

  if (!authReady) {
    return (
      <div className="login-page login-page--professional login-page--bootstrapping">
        <div className="login-shell">
          <div className="login-shell-card">
            <div className="login-panel login-panel--form">
              <p>Verificando sessão…</p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (token && user && !mfaPending) {
    return <Navigate to={user.role === 'Patient' ? '/portal-paciente' : '/'} replace />;
  }

  async function handleSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      const result = await login(email, password);
      if (!result.requiresMfa) {
        navigate(email === 'paciente@hospital.local' ? '/portal-paciente' : '/');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao entrar');
    }
  }

  async function handleMfaSubmit(event: FormEvent) {
    event.preventDefault();
    setError('');
    try {
      await verifyMfa(mfaCode);
      navigate(user?.role === 'Patient' ? '/portal-paciente' : '/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Código inválido');
    }
  }

  return (
    <div className="login-page login-page--professional">
      <div className="login-shell">
        <div className="login-shell-card">
          <div className="login-panel login-panel--form">
            <HospitalLogo height={93} className="login-logo" />

            <p className="login-panel-tagline">
              Acesso exclusivo para equipes credenciadas
            </p>

            {!mfaPending ? (
              <>
                <h2>{greeting}, entre no sistema</h2>
                <p>Utilize o e-mail e a senha fornecidos pela sua instituição.</p>
                {error && <div className="alert alert-error">{error}</div>}
                <form onSubmit={handleSubmit} className="login-form">
                  <div className="form-field full">
                    <label htmlFor="email">E-mail</label>
                    <input
                      id="email"
                      type="email"
                      required
                      autoComplete="username"
                      value={email}
                      onChange={(e) => setEmail(e.target.value)}
                    />
                  </div>
                  <div className="form-field full">
                    <label htmlFor="password">Senha</label>
                    <input
                      id="password"
                      type="password"
                      required
                      autoComplete="current-password"
                      value={password}
                      onChange={(e) => setPassword(e.target.value)}
                    />
                  </div>
                  <button className="btn full-width" type="submit" disabled={isLoading}>
                    {isLoading ? 'Entrando…' : 'Entrar'}
                  </button>
                </form>
                <span className="login-forgot">Esqueceu sua senha? Contate o administrador.</span>
              </>
            ) : (
              <>
                <h2>Verificação MFA</h2>
                <p>Informe o código de 6 dígitos do seu autenticador.</p>
                {error && <div className="alert alert-error">{error}</div>}
                <form onSubmit={handleMfaSubmit} className="login-form">
                  <div className="form-field full">
                    <label htmlFor="mfaCode">Código</label>
                    <input
                      id="mfaCode"
                      inputMode="numeric"
                      autoComplete="one-time-code"
                      required
                      value={mfaCode}
                      onChange={(e) => setMfaCode(e.target.value)}
                    />
                  </div>
                  <button className="btn full-width" type="submit" disabled={isLoading}>
                    {isLoading ? 'Verificando…' : 'Confirmar'}
                  </button>
                  <button type="button" className="btn btn-secondary full-width" onClick={cancelMfa}>
                    Voltar
                  </button>
                </form>
              </>
            )}

            <div className="login-hints">
              <p><strong>Admin:</strong> admin@hospital.local / Admin123!</p>
              <p><strong>Recepção:</strong> recepcao@hospital.local / Recepcao123!</p>
              <p><strong>Médico:</strong> medico@hospital.local / Medico123!</p>
            </div>
          </div>

          <div className="login-panel login-panel--hero" aria-hidden={false}>
            <div className="login-hero-content" aria-live="polite">
              <h3>{slide.title}</h3>
              <p>{slide.text}</p>
            </div>
            <div className="login-hero-dots" aria-hidden>
              {HERO_SLIDES.map((_, index) => (
                <span key={index} className={index === slideIndex ? 'is-active' : ''} />
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

import { useState, type FormEvent } from 'react';
import { Navigate, useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { FeegowFooter } from '../components/feegow/FeegowFooter';
import { HospitalLogo } from '../components/HospitalLogo';
import { getInstitutionName } from '../config/iasghBranding';

export function FeegowLoginPage() {
  const { login, verifyMfa, isLoading, token, user, mfaPending, cancelMfa, authReady } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [remember, setRemember] = useState(false);
  const [mfaCode, setMfaCode] = useState('');
  const [error, setError] = useState('');

  if (!authReady) {
    return (
      <div className="feegow-login-shell">
        <div className="feegow-login-page">
          <div className="feegow-login-card">
            <div className="feegow-login-form-col">
              <p>Verificando sessão…</p>
            </div>
          </div>
        </div>
        <FeegowFooter />
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
    <div className="feegow-login-shell">
    <div className="feegow-login-page">
      <div className="feegow-login-card">
        <div className="feegow-login-form-col">
          <div className="feegow-login-logo-wrap">
            <HospitalLogo variant="full" height={113} className="feegow-login-logo" />
          </div>

          {!mfaPending ? (
            <form onSubmit={handleSubmit}>
              <span className="feegow-login-title">Faça o login na sua conta</span>

              <div className="feegow-login-field">
                <label htmlFor="feegow-email">E-mail</label>
                <input
                  id="feegow-email"
                  type="email"
                  name="User"
                  required
                  autoFocus
                  autoComplete="username"
                  placeholder="digite seu e-mail de acesso"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>

              <div className="feegow-login-field">
                <label htmlFor="feegow-password">Senha</label>
                <input
                  id="feegow-password"
                  type="password"
                  name="password"
                  required
                  autoComplete="current-password"
                  placeholder="senha"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </div>

              <div className="feegow-login-row">
                <label className="feegow-login-remember">
                  <input
                    type="checkbox"
                    checked={remember}
                    onChange={(e) => setRemember(e.target.checked)}
                  />
                  Lembrar dados de acesso
                </label>
                <span className="feegow-login-forgot">Esqueceu sua senha? / primeiro acesso?</span>
              </div>

              <button type="submit" className="feegow-login-submit" disabled={isLoading}>
                {isLoading ? 'Entrando…' : 'Entrar'}
                <span aria-hidden>→</span>
              </button>

              {error ? <div className="feegow-login-erro">{error}</div> : null}
            </form>
          ) : (
            <form onSubmit={handleMfaSubmit}>
              <span className="feegow-login-title">Verificação em duas etapas</span>
              <div className="feegow-login-field">
                <label htmlFor="feegow-mfa">Código</label>
                <input
                  id="feegow-mfa"
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  required
                  value={mfaCode}
                  onChange={(e) => setMfaCode(e.target.value)}
                />
              </div>
              <button type="submit" className="feegow-login-submit" disabled={isLoading}>
                {isLoading ? 'Verificando…' : 'Confirmar'}
              </button>
              <button type="button" className="feegow-login-submit" style={{ marginTop: 8, background: '#888' }} onClick={cancelMfa}>
                Voltar
              </button>
              {error ? <div className="feegow-login-erro">{error}</div> : null}
            </form>
          )}

          <div className="feegow-login-copyright">
            © {new Date().getFullYear()} {getInstitutionName()}
          </div>

          <div className="feegow-login-hints">
            <p><strong>Demo local:</strong> admin@hospital.local / Admin123!</p>
          </div>
        </div>

        <div className="feegow-login-hero" aria-hidden>
          <div className="feegow-login-hero-bg">
            <img
              className="feegow-login-hero-photo"
              src="/iasgh-login-hero.png"
              alt=""
              decoding="async"
              draggable={false}
            />
          </div>
          <div className="feegow-login-hero-overlay" />
          <div className="feegow-login-hero-content">
            <p className="feegow-login-hero-welcome">Bem-vindo!</p>
            <p className="feegow-login-hero-brand">IASGH</p>
            <p className="feegow-login-hero-sub">Sistema de Gestão Hospitalar</p>
          </div>
        </div>
      </div>
    </div>
    <FeegowFooter />
    </div>
  );
}

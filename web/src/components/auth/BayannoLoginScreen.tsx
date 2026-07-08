import { type FormEvent } from 'react';
import { HospitalLogo } from '../HospitalLogo';

type BayannoLoginScreenProps = {
  systemName: string;
  email: string;
  password: string;
  mfaCode: string;
  mfaPending: boolean;
  isLoading: boolean;
  error: string;
  onEmailChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onMfaCodeChange: (value: string) => void;
  onSubmit: (event: FormEvent) => void;
  onMfaSubmit: (event: FormEvent) => void;
  onCancelMfa: () => void;
};

const ACCOUNT_TYPES = [
  { value: '', label: 'Tipo de conta' },
  { value: 'admin', label: 'Administrador' },
  { value: 'doctor', label: 'Médico' },
  { value: 'patient', label: 'Paciente' },
  { value: 'nurse', label: 'Enfermeiro' },
  { value: 'pharmacist', label: 'Farmacêutico' },
  { value: 'laboratorist', label: 'Laboratorista' },
  { value: 'accountant', label: 'Contador' },
];

/**
 * Cópia fiel de application/views/login.php (Bayanno HMS).
 */
export function BayannoLoginScreen({
  systemName,
  email,
  password,
  mfaCode,
  mfaPending,
  isLoading,
  error,
  onEmailChange,
  onPasswordChange,
  onMfaCodeChange,
  onSubmit,
  onMfaSubmit,
  onCancelMfa,
}: BayannoLoginScreenProps) {
  return (
    <div className="bayanno-php-screen bayanno-login-php">
      <div className="navbar navbar-top navbar-inverse">
        <div className="navbar-inner">
          <div className="container-fluid">
            <a className="brand" href="/login">{systemName}</a>
          </div>
        </div>
      </div>

      <div className="container">
        <div className="bayanno-login-column">
          <div className="padded">
            <div className="bayanno-login-logo-wrap">
              <HospitalLogo height={202} className="login-logo" />
            </div>

            <div className="login box" style={{ marginTop: 10 }}>
              <div className="box-header">
                <span className="title">{mfaPending ? 'Verificação MFA' : 'Login'}</span>
              </div>
              <div className="box-content padded">
                {error && <div className="alert alert-error">{error}</div>}

                {!mfaPending ? (
                  <form onSubmit={onSubmit} className="separate-sections">
                    <div>
                      <select
                        className="validate[required]"
                        name="login_type"
                        style={{ width: '100%' }}
                        defaultValue=""
                        aria-label="Tipo de conta"
                      >
                        {ACCOUNT_TYPES.map((opt) => (
                          <option key={opt.value} value={opt.value}>{opt.label}</option>
                        ))}
                      </select>
                    </div>
                    <div className="input-prepend">
                      <span className="add-on">
                        <i className="icon-envelope" aria-hidden />
                      </span>
                      <input
                        name="email"
                        type="email"
                        placeholder="E-mail"
                        required
                        autoComplete="username"
                        value={email}
                        onChange={(e) => onEmailChange(e.target.value)}
                      />
                    </div>
                    <div className="input-prepend">
                      <span className="add-on">
                        <i className="icon-key" aria-hidden />
                      </span>
                      <input
                        name="password"
                        type="password"
                        placeholder="Senha"
                        required
                        autoComplete="current-password"
                        value={password}
                        onChange={(e) => onPasswordChange(e.target.value)}
                      />
                    </div>
                    <div>
                      <button type="submit" className="btn btn-blue btn-block" disabled={isLoading}>
                        {isLoading ? 'Entrando…' : 'Login'}
                      </button>
                    </div>
                  </form>
                ) : (
                  <form onSubmit={onMfaSubmit} className="separate-sections">
                    <div className="input-prepend">
                      <span className="add-on">
                        <i className="icon-key" aria-hidden />
                      </span>
                      <input
                        inputMode="numeric"
                        autoComplete="one-time-code"
                        placeholder="Código MFA"
                        required
                        value={mfaCode}
                        onChange={(e) => onMfaCodeChange(e.target.value)}
                      />
                    </div>
                    <div>
                      <button type="submit" className="btn btn-blue btn-block" disabled={isLoading}>
                        {isLoading ? 'Verificando…' : 'Confirmar'}
                      </button>
                    </div>
                    <div>
                      <button type="button" className="btn btn-block" onClick={onCancelMfa}>
                        Voltar
                      </button>
                    </div>
                  </form>
                )}

                {!mfaPending && (
                  <div>
                    <span>Esqueceu a senha? Contate o administrador.</span>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

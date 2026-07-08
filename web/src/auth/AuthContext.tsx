import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';

import type { LoginApiResponse, UserRoleName } from '../api/client';



export type AuthUser = {

  userId: string;

  fullName: string;

  email: string;

  role: UserRoleName;

  professionalId?: string;

  patientId?: string;

  permissions: string[];

  mfaEnabled: boolean;

};



type AuthContextValue = {

  user: AuthUser | null;

  token: string | null;

  isLoading: boolean;

  authReady: boolean;

  login: (email: string, password: string) => Promise<{ requiresMfa: boolean }>;

  verifyMfa: (code: string) => Promise<void>;

  logout: () => void;

  hasRole: (...roles: UserRoleName[]) => boolean;

  hasPermission: (...permissions: string[]) => boolean;

  mfaPending: boolean;

  cancelMfa: () => void;

};



const AuthContext = createContext<AuthContextValue | null>(null);



const TOKEN_KEY = 'hospital_token';

const USER_KEY = 'hospital_user';

const MFA_TOKEN_KEY = 'hospital_mfa_token';



function mapLoginResponse(data: LoginApiResponse): AuthUser {

  return {

    userId: data.userId,

    fullName: data.fullName,

    email: data.email,

    role: data.role,

    professionalId: data.professionalId,

    patientId: data.patientId,

    permissions: data.permissions ?? [],

    mfaEnabled: data.mfaEnabled,

  };

}



function mapProfileResponse(profile: {

  id: string;

  fullName: string;

  email: string;

  role: string;

  professionalId?: string | null;

  patientId?: string | null;

  permissions?: string[];

  mfaEnabled?: boolean;

}): AuthUser {

  return {

    userId: profile.id,

    fullName: profile.fullName,

    email: profile.email,

    role: profile.role as UserRoleName,

    professionalId: profile.professionalId ?? undefined,

    patientId: profile.patientId ?? undefined,

    permissions: profile.permissions ?? [],

    mfaEnabled: profile.mfaEnabled ?? false,

  };

}



function clearStoredSession() {

  localStorage.removeItem(TOKEN_KEY);

  localStorage.removeItem(USER_KEY);

  localStorage.removeItem(MFA_TOKEN_KEY);

}



export function AuthProvider({ children }: { children: ReactNode }) {

  const [user, setUser] = useState<AuthUser | null>(() => {

    const stored = localStorage.getItem(USER_KEY);

    return stored ? (JSON.parse(stored) as AuthUser) : null;

  });

  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY));

  const [mfaToken, setMfaToken] = useState<string | null>(() => localStorage.getItem(MFA_TOKEN_KEY));

  const [isLoading, setIsLoading] = useState(false);

  const [authReady, setAuthReady] = useState(() => !localStorage.getItem(TOKEN_KEY));



  useEffect(() => {

    if (token) {

      localStorage.setItem(TOKEN_KEY, token);

    } else {

      localStorage.removeItem(TOKEN_KEY);

    }

  }, [token]);



  useEffect(() => {

    if (user) {

      localStorage.setItem(USER_KEY, JSON.stringify(user));

    } else {

      localStorage.removeItem(USER_KEY);

    }

  }, [user]);



  useEffect(() => {

    if (mfaToken) {

      localStorage.setItem(MFA_TOKEN_KEY, mfaToken);

    } else {

      localStorage.removeItem(MFA_TOKEN_KEY);

    }

  }, [mfaToken]);



  useEffect(() => {

    const storedToken = localStorage.getItem(TOKEN_KEY);

    if (!storedToken) {

      setAuthReady(true);

      return;

    }



    let cancelled = false;



    fetch('/api/auth/me', { headers: { Authorization: `Bearer ${storedToken}` } })

      .then(async (res) => {

        if (cancelled) return;

        if (!res.ok) {

          clearStoredSession();

          setToken(null);

          setUser(null);

          setMfaToken(null);

          return;

        }

        const profile = await res.json();

        setUser(mapProfileResponse(profile));

      })

      .catch(() => {

        if (cancelled) return;

      })

      .finally(() => {

        if (!cancelled) setAuthReady(true);

      });



    return () => {

      cancelled = true;

    };

  }, []);



  async function login(email: string, password: string) {

    setIsLoading(true);

    try {

      const response = await fetch('/api/auth/login', {

        method: 'POST',

        headers: { 'Content-Type': 'application/json' },

        body: JSON.stringify({ email, password }),

      });



      if (response.status === 423) {

        const error = await response.json().catch(() => ({ message: 'Conta bloqueada' }));

        throw new Error(error.message ?? 'Conta bloqueada temporariamente');

      }



      if (!response.ok) {

        const error = await response.json().catch(() => ({ message: 'Falha no login' }));

        throw new Error(error.message ?? 'Falha no login');

      }



      const data = (await response.json()) as LoginApiResponse;



      if (data.requiresMfa && data.mfaToken) {

        setMfaToken(data.mfaToken);

        setUser(mapLoginResponse(data));

        return { requiresMfa: true };

      }



      if (!data.token) {

        throw new Error('Resposta de login inválida');

      }



      setToken(data.token);

      setUser(mapLoginResponse(data));

      setMfaToken(null);

      return { requiresMfa: false };

    } finally {

      setIsLoading(false);

    }

  }



  async function verifyMfa(code: string) {

    if (!mfaToken) {

      throw new Error('Sessão MFA expirada. Faça login novamente.');

    }



    setIsLoading(true);

    try {

      const response = await fetch('/api/auth/mfa/verify', {

        method: 'POST',

        headers: { 'Content-Type': 'application/json' },

        body: JSON.stringify({ mfaToken, code }),

      });



      if (!response.ok) {

        const error = await response.json().catch(() => ({ message: 'Código inválido' }));

        throw new Error(error.message ?? 'Código MFA inválido');

      }



      const data = (await response.json()) as LoginApiResponse;

      if (!data.token) {

        throw new Error('Falha ao concluir MFA');

      }



      setToken(data.token);

      setUser(mapLoginResponse(data));

      setMfaToken(null);

    } finally {

      setIsLoading(false);

    }

  }



  function cancelMfa() {

    setMfaToken(null);

    setUser(null);

  }



  function logout() {

    if (token) {

      fetch('/api/auth/logout', {

        method: 'POST',

        headers: { Authorization: `Bearer ${token}` },

      }).catch(() => undefined);

    }

    clearStoredSession();

    setToken(null);

    setUser(null);

    setMfaToken(null);

  }



  function hasRole(...roles: UserRoleName[]) {

    return user ? roles.includes(user.role) : false;

  }



  function hasPermission(...permissions: string[]) {

    if (!user) return false;

    return permissions.some((p) => user.permissions.includes(p));

  }



  return (

    <AuthContext.Provider value={{

      user,

      token,

      isLoading,

      authReady,

      login,

      verifyMfa,

      logout,

      hasRole,

      hasPermission,

      mfaPending: !!mfaToken,

      cancelMfa,

    }}>

      {children}

    </AuthContext.Provider>

  );

}



export function useAuth() {

  const context = useContext(AuthContext);

  if (!context) {

    throw new Error('useAuth deve ser usado dentro de AuthProvider');

  }

  return context;

}



export function getStoredToken() {

  return localStorage.getItem(TOKEN_KEY);

}



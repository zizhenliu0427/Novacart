'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import type { AuthResponse, LoginRequest, RegisterRequest, UserDto } from '@/types/auth';
import { apiCall } from '@/lib/api';
import { clearToken, getToken, setToken } from '@/lib/auth';

/* ─── Types ─────────────────────────────────────────────────── */

interface AuthContextValue {
  user: UserDto | null;
  token: string | null;
  isLoading: boolean;
  login: (req: LoginRequest) => Promise<void>;
  register: (req: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

/* ─── Context ────────────────────────────────────────────────── */

const AuthContext = createContext<AuthContextValue | null>(null);

/* ─── Provider ───────────────────────────────────────────────── */

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [token, setTokenState] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true); // true until initial /me resolves

  /** Persist token + update state together. */
  const applyToken = useCallback((t: string | null) => {
    if (t) {
      setToken(t);
    } else {
      clearToken();
    }
    setTokenState(t);
  }, []);

  /** On mount, re-hydrate from localStorage and verify the token is still valid. */
  useEffect(() => {
    const stored = getToken();
    if (!stored) {
      setIsLoading(false);
      return;
    }
    setTokenState(stored);
    apiCall<UserDto>('/auth/me', { token: stored })
      .then(setUser)
      .catch(() => {
        // Token expired or invalid — clear silently.
        applyToken(null);
        setUser(null);
      })
      .finally(() => setIsLoading(false));
  }, [applyToken]);

  const login = useCallback(
    async (req: LoginRequest) => {
      const res = await apiCall<AuthResponse>('/auth/login', {
        method: 'POST',
        body: req,
      });
      applyToken(res.token);
      setUser(res.user);
    },
    [applyToken],
  );

  const register = useCallback(
    async (req: RegisterRequest) => {
      const res = await apiCall<AuthResponse>('/auth/register', {
        method: 'POST',
        body: req,
      });
      applyToken(res.token);
      setUser(res.user);
    },
    [applyToken],
  );

  const logout = useCallback(async () => {
    const t = token;
    // Optimistically clear before the network call so the UI snaps immediately.
    applyToken(null);
    setUser(null);
    if (t) {
      await apiCall('/auth/logout', { method: 'POST', token: t }).catch(() => {
        // Best-effort — token is already gone client-side.
      });
    }
  }, [token, applyToken]);

  const value = useMemo<AuthContextValue>(
    () => ({ user, token, isLoading, login, register, logout }),
    [user, token, isLoading, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/* ─── Hook ───────────────────────────────────────────────────── */

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}

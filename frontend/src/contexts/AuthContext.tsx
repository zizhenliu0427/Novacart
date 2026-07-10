'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import type { LoginRequest, RegisterRequest, UserDto } from '@/types/auth';
import { apiCall } from '@/lib/api';
import { clearToken, setToken } from '@/lib/auth';

/* ─── Types ─────────────────────────────────────────────────── */

interface AuthContextValue {
  user: UserDto | null;
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
  const [isLoading, setIsLoading] = useState(true); // true until initial /me resolves

  /**
   * On mount, call GET /auth/me (cookie sent automatically) to determine
   * if the user has a valid session.
   */
  useEffect(() => {
    apiCall<UserDto>('/auth/me')
      .then((u) => {
        setUser(u);
        // Ensure the flag cookie is in sync (e.g. after hard refresh).
        setToken('');
      })
      .catch(() => {
        // No valid session — clear user state and flag cookie silently.
        setUser(null);
        clearToken();
      })
      .finally(() => setIsLoading(false));
  }, []);

  const login = useCallback(async (req: LoginRequest) => {
    // POST to /auth/login — backend sets the HttpOnly JWT cookie via Set-Cookie.
    await apiCall<void>('/auth/login', {
      method: 'POST',
      body: req,
    });
    // Fetch user profile now that the cookie is set.
    const me = await apiCall<UserDto>('/auth/me');
    setUser(me);
    // Set the lightweight flag cookie for Edge middleware.
    setToken('');
  }, []);

  const register = useCallback(async (req: RegisterRequest) => {
    // POST to /auth/register — backend sets the HttpOnly JWT cookie via Set-Cookie.
    await apiCall<void>('/auth/register', {
      method: 'POST',
      body: req,
    });
    // Fetch user profile now that the cookie is set.
    const me = await apiCall<UserDto>('/auth/me');
    setUser(me);
    // Set the lightweight flag cookie for Edge middleware.
    setToken('');
  }, []);

  const logout = useCallback(async () => {
    // Optimistically clear the UI immediately.
    setUser(null);
    clearToken();
    // POST to /auth/logout — backend clears the HttpOnly JWT cookie.
    await apiCall('/auth/logout', { method: 'POST' }).catch(() => {
      // Best-effort — flag cookie is already gone client-side.
    });
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user, isLoading, login, register, logout }),
    [user, isLoading, login, register, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/* ─── Hook ───────────────────────────────────────────────────── */

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
  return ctx;
}

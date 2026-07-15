import { clearToken } from './auth';

const API_BASE = '/api';

interface ApiOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
}

interface ApiErrorPayload {
  message?: string;
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

function buildHeaders(): HeadersInit {
  return { 'Content-Type': 'application/json' };
}

function buildInit(options: ApiOptions): RequestInit {
  const { method = 'GET', body } = options;
  return {
    method,
    headers: buildHeaders(),
    body: body ? JSON.stringify(body) : undefined,
    credentials: 'include', // Send HttpOnly cookies automatically.
  };
}

// ── Refresh-token coordination ──────────────────────────────
// When the access token expires, multiple in-flight requests may 401 at once.
// We coalesce them onto a single refresh call so we only rotate once.

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = (async () => {
      try {
        const res = await fetch(`${API_BASE}/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          credentials: 'include', // carries the novacart_refresh cookie
        });
        return res.ok;
      } catch {
        return false;
      } finally {
        // Allow the next refresh attempt if this one is used again later.
        refreshPromise = null;
      }
    })();
  }
  return refreshPromise;
}

function bailToLogin(): never {
  if (typeof window !== 'undefined') {
    clearToken();
    window.location.href = '/login';
  }
  throw new Error('Authentication failed');
}

/**
 * Core fetch wrapper with cookie auth, automatic access-token refresh on 401,
 * and structured error parsing.
 */
export async function apiCall<T>(endpoint: string, options: ApiOptions = {}): Promise<T> {
  let res = await fetch(`${API_BASE}${endpoint}`, buildInit(options));

  // Access token expired? Try to rotate it once, then retry the original request.
  if (res.status === 401 && !endpoint.startsWith('/auth/')) {
    const refreshed = await tryRefresh();
    if (refreshed) {
      res = await fetch(`${API_BASE}${endpoint}`, buildInit(options));
    } else {
      bailToLogin();
    }
  }

  if (res.status === 401) {
    bailToLogin();
  }

  if (res.status === 403) {
    throw new Error('You do not have permission to perform this action.');
  }

  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: res.statusText })) as ApiErrorPayload;
    const validationMessage = error.errors
      ? Object.values(error.errors).flat().join(' ')
      : undefined;
    throw new Error(validationMessage || error.detail || error.message || error.title || 'API request failed');
  }

  if (res.status === 204) return undefined as T;
  return res.json();
}

import { clearToken } from './auth';

const API_BASE = '/api';

interface ApiOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  token?: string;
}

interface ApiErrorPayload {
  message?: string;
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

export async function apiCall<T>(endpoint: string, options: ApiOptions = {}): Promise<T> {
  const { method = 'GET', body, token } = options;

  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const res = await fetch(`${API_BASE}${endpoint}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (res.status === 401) {
    // An invalid/expired token requires re-authentication.
    if (typeof window !== 'undefined') {
      clearToken();
      window.location.href = '/login';
    }
    throw new Error('Authentication failed');
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

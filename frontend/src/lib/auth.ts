/**
 * Token helpers — thin wrappers around localStorage so the rest of the app
 * never imports localStorage directly (easier to swap to cookie storage later).
 *
 * Also maintains a lightweight "novacart_authed=1" session cookie so the
 * Next.js Edge middleware can guard protected routes without accessing localStorage.
 */

const TOKEN_KEY = 'novacart_token';
const AUTH_COOKIE = 'novacart_authed';

export function getToken(): string | null {
  if (typeof window === 'undefined') return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
  // Set a flag cookie (no token data) for Edge middleware route guarding.
  document.cookie = `${AUTH_COOKIE}=1; path=/; samesite=strict`;
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
  // Expire the flag cookie.
  document.cookie = `${AUTH_COOKIE}=; path=/; max-age=0; samesite=strict`;
}

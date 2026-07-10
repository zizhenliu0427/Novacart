/**
 * Auth helpers — HttpOnly cookie-based authentication.
 *
 * The JWT is now stored in an HttpOnly cookie (`novacart_jwt`) set by the
 * backend on login/register and cleared on logout. The browser sends it
 * automatically with every request — the frontend never sees or handles
 * the raw token.
 *
 * These helpers only manage a lightweight `novacart_authed=1` flag cookie
 * so the Next.js Edge middleware can guard protected routes without
 * needing access to the HttpOnly JWT cookie.
 */

const AUTH_COOKIE = 'novacart_authed';

/**
 * Returns `null` always — the JWT lives in an HttpOnly cookie that
 * JavaScript cannot (and should not) access.
 */
export function getToken(): string | null {
  return null;
}

/**
 * Sets the `novacart_authed` flag cookie so Edge middleware knows the
 * user has authenticated. Does NOT store the JWT — the backend handles
 * that via a Set-Cookie header.
 */
export function setToken(_token: string): void {
  document.cookie = `${AUTH_COOKIE}=1; path=/; samesite=strict`;
}

/**
 * Expires the `novacart_authed` flag cookie. The backend is responsible
 * for clearing the HttpOnly JWT cookie via its logout endpoint.
 */
export function clearToken(): void {
  document.cookie = `${AUTH_COOKIE}=; path=/; max-age=0; samesite=strict`;
}

import { NextResponse, type NextRequest } from 'next/server';

/** Routes that require authentication. (Admin also does a client-side role check in /admin/layout.) */
const PROTECTED = ['/orders', '/cart', '/checkout', '/account', '/wishlist', '/admin'];

/** Routes that should redirect to home when already authenticated. */
const AUTH_ONLY = ['/login', '/register'];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // We can't read localStorage in Edge middleware, so we rely on the
  // Authorization header or a lightweight cookie that the client sets.
  // For now (Bearer-token model) we just protect routes client-side via
  // useAuth isLoading check. This middleware blocks obvious direct navigation.
  //
  // To make server-side guarding work with Bearer tokens: set a short-lived
  // "novacart_authed" cookie (no real token inside, just a flag) when
  // logging in and clear it on logout.
  const authedFlag = request.cookies.get('novacart_authed')?.value === '1';

  const isProtected = PROTECTED.some((p) => pathname.startsWith(p));
  const isAuthOnly = AUTH_ONLY.some((p) => pathname.startsWith(p));

  if (isProtected && !authedFlag) {
    const url = request.nextUrl.clone();
    url.pathname = '/login';
    url.searchParams.set('next', pathname);
    return NextResponse.redirect(url);
  }

  if (isAuthOnly && authedFlag) {
    return NextResponse.redirect(new URL('/', request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    '/orders/:path*',
    '/cart/:path*',
    '/checkout/:path*',
    '/account/:path*',
    '/wishlist/:path*',
    '/admin/:path*',
    '/login',
    '/register',
  ],
};

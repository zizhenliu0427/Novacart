import createMiddleware from 'next-intl/middleware';
import { NextRequest, NextResponse } from 'next/server';
import { routing } from './i18n/routing';

const handleI18nRouting = createMiddleware(routing);

const PROTECTED = ['/orders', '/checkout', '/account', '/wishlist', '/admin'];
const AUTH_ONLY = ['/login', '/register'];

function pathWithoutLocale(pathname: string): { locale: string; path: string } {
  const segments = pathname.split('/').filter(Boolean);
  const first = segments[0];
  if (first && routing.locales.includes(first as typeof routing.locales[number])) {
    const rest = segments.slice(1).join('/');
    return { locale: first, path: rest ? `/${rest}` : '/' };
  }
  return { locale: routing.defaultLocale, path: pathname };
}

export default function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const { locale, path } = pathWithoutLocale(pathname);
  const authedFlag = request.cookies.get('novacart_authed')?.value === '1';

  if (PROTECTED.some((p) => path.startsWith(p)) && !authedFlag) {
    const url = request.nextUrl.clone();
    url.pathname = `/${locale}/login`;
    url.searchParams.set('next', pathname);
    return NextResponse.redirect(url);
  }

  if (AUTH_ONLY.some((p) => path.startsWith(p)) && authedFlag) {
    const url = request.nextUrl.clone();
    url.pathname = `/${locale}`;
    return NextResponse.redirect(url);
  }

  const response = handleI18nRouting(request);
  if (response) {
    response.headers.set('x-next-pathname', path);
  }
  return response;
}

export const config = {
  matcher: ['/', '/(en|zh)/:path*'],
};

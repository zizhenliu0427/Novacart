import { routing, type AppLocale } from '@/i18n/routing';

/** Read the locale prefix from a browser pathname (e.g. `/en/cart` → `en`). */
export function localeFromPathname(pathname: string): AppLocale {
  const segment = pathname.split('/').filter(Boolean)[0];
  if (segment && routing.locales.includes(segment as AppLocale)) {
    return segment as AppLocale;
  }
  return routing.defaultLocale;
}

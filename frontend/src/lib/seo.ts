import { routing } from '@/i18n/routing';

const HREFLANG: Record<(typeof routing.locales)[number], string> = {
  en: 'en-AU',
  zh: 'zh-CN',
};

/** Build canonical + hreflang alternates for a locale-neutral path (e.g. `/products`). */
export function buildHreflangAlternates(pathWithoutLocale: string, currentLocale: string) {
  const base = (process.env.NEXT_PUBLIC_SITE_URL ?? 'http://localhost:3000').replace(/\/$/, '');
  const path = pathWithoutLocale === '/' ? '' : pathWithoutLocale;

  const languages: Record<string, string> = {};
  for (const loc of routing.locales) {
    languages[HREFLANG[loc]] = `${base}/${loc}${path}`;
  }
  languages['x-default'] = `${base}/${routing.defaultLocale}${path}`;

  const canonicalLocale = routing.locales.includes(currentLocale as (typeof routing.locales)[number])
    ? currentLocale
    : routing.defaultLocale;

  return {
    canonical: `${base}/${canonicalLocale}${path}`,
    languages,
  };
}

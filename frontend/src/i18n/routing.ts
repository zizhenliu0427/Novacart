import { defineRouting } from 'next-intl/routing';

/**
 * URL locales: `en` = Australian English (en-AU spelling & AUD formatting),
 * `zh` = Simplified Chinese (zh-CN).
 */
export const routing = defineRouting({
  locales: ['en', 'zh'],
  defaultLocale: 'en',
  localePrefix: 'always',
});

export type AppLocale = (typeof routing.locales)[number];

/** BCP 47 tag for Intl APIs (dates, numbers). */
export function intlLocaleFor(locale: AppLocale): string {
  return locale === 'zh' ? 'zh-CN' : 'en-AU';
}

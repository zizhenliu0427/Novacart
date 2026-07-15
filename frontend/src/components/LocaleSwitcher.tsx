'use client';

import { useLocale, useTranslations } from 'next-intl';
import { usePathname, useRouter } from '@/i18n/navigation';
import { routing, type AppLocale } from '@/i18n/routing';

/** Switch between `/en/...` and `/zh/...` while preserving the current path. */
export function LocaleSwitcher() {
  const t = useTranslations('nav');
  const locale = useLocale() as AppLocale;
  const pathname = usePathname();
  const router = useRouter();

  function switchTo(next: AppLocale) {
    if (next === locale) return;
    router.replace(pathname, { locale: next });
  }

  return (
    <div
      className="flex items-center gap-0.5 rounded-lg border border-border bg-surface p-0.5 text-xs"
      role="group"
      aria-label={t('language')}
    >
      {routing.locales.map((loc) => (
        <button
          key={loc}
          type="button"
          onClick={() => switchTo(loc)}
          className={`rounded-md px-2 py-1 font-medium transition ${
            loc === locale
              ? 'bg-accent text-accent-contrast'
              : 'text-ink-muted hover:bg-bg-subtle hover:text-ink'
          }`}
          aria-pressed={loc === locale}
        >
          {loc === 'en' ? t('english') : t('chinese')}
        </button>
      ))}
    </div>
  );
}

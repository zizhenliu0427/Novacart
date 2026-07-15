'use client';

import { useTranslations } from 'next-intl';

export function Footer() {
  const t = useTranslations('footer');
  return (
    <div className="mx-auto max-w-content px-4 py-8 text-sm text-ink-muted sm:px-6">
      {t('tagline', { year: new Date().getFullYear() })}
    </div>
  );
}

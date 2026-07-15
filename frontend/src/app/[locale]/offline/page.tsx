'use client';

import { useTranslations } from 'next-intl';
import { Link } from '@/i18n/navigation';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';

export default function OfflinePage() {
  const t = useTranslations('offline');

  return (
    <div className="flex min-h-[70vh] flex-col items-center justify-center px-4 py-12">
      <Card className="w-full max-w-md space-y-6 border border-border p-8 text-center shadow-md">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-bg-subtle text-ink-muted">
          <svg className="h-10 w-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2"
              d="M18.364 5.636a9 9 0 010 12.728m0 0l-2.829-2.829m2.829 2.829L21 21M21 3L3 21M8.464 8.464a5 5 0 017.072 0M12 12a1 1 0 100 2 1 1 0 000-2z"
            />
          </svg>
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl font-semibold tracking-tight text-ink">{t('title')}</h1>
          <p className="text-sm text-ink-muted">{t('offlineDetail')}</p>
        </div>

        <div className="flex flex-col justify-center gap-3 pt-4">
          <Button onClick={() => window.location.reload()} className="w-full">
            {t('retryConnection')}
          </Button>
          <Link href="/" className="w-full">
            <Button variant="secondary" className="w-full">
              {t('goHome')}
            </Button>
          </Link>
        </div>
      </Card>
    </div>
  );
}

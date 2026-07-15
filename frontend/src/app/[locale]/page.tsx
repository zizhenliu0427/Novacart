'use client';

import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Link } from '@/i18n/navigation';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { GridIcon, PackageIcon, SparkIcon } from '@/components/icons';

interface HealthStatus {
  status: string;
  timestamp: string;
  environment: string;
}

export default function HomePage() {
  const t = useTranslations('home');
  const tc = useTranslations('common');
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/health')
      .then((res) => res.json())
      .then(setHealth)
      .catch((err) => setError(err.message));
  }, []);

  const features = [
    { Icon: GridIcon, title: t('feature1Title'), body: t('feature1Body') },
    { Icon: PackageIcon, title: t('feature2Title'), body: t('feature2Body') },
    { Icon: SparkIcon, title: t('feature3Title'), body: t('feature3Body') },
  ];

  return (
    <div className="space-y-16">
      <section className="mx-auto max-w-2xl py-12 text-center sm:py-16">
        <span className="mb-5 inline-flex items-center gap-1.5 rounded-full border border-border bg-surface px-3 py-1 text-xs font-medium text-ink-muted">
          <SparkIcon className="h-3.5 w-3.5 text-accent" />
          {t('badge')}
        </span>
        <h1 className="text-4xl font-semibold tracking-tight text-ink sm:text-5xl">
          {t('heroTitle')}
          <br />
          {t('heroTitleLine2')}
        </h1>
        <p className="mx-auto mt-4 max-w-lg text-lg text-ink-muted">{t('heroSubtitle')}</p>
        <div className="mt-8 flex justify-center gap-3">
          <Link href="/products">
            <Button>{t('browseProducts')}</Button>
          </Link>
          <Link href="/orders">
            <Button variant="secondary">{t('viewOrders')}</Button>
          </Link>
        </div>
      </section>

      <section className="grid gap-5 sm:grid-cols-3">
        {features.map(({ Icon, title, body }) => (
          <Card key={title} className="p-6">
            <span className="mb-4 grid h-10 w-10 place-items-center rounded-lg bg-accent-weak text-accent">
              <Icon className="h-5 w-5" />
            </span>
            <h3 className="font-semibold text-ink">{title}</h3>
            <p className="mt-1 text-sm text-ink-muted">{body}</p>
          </Card>
        ))}
      </section>

      <section className="mx-auto max-w-md">
        <Card className="p-5">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold text-ink">{t('backendStatus')}</h2>
            <span
              className={`inline-flex items-center gap-1.5 text-sm ${
                error ? 'text-danger' : health ? 'text-success' : 'text-ink-muted'
              }`}
            >
              <span
                className={`h-2 w-2 rounded-full ${
                  error ? 'bg-danger' : health ? 'bg-success' : 'bg-ink-muted'
                }`}
              />
              {error ? t('unreachable') : health ? t('healthy') : tc('connecting')}
            </span>
          </div>
          {health && (
            <dl className="mt-3 space-y-1 text-sm text-ink-muted">
              <div className="flex justify-between">
                <dt>{t('environment')}</dt>
                <dd className="text-ink">{health.environment}</dd>
              </div>
              <div className="flex justify-between">
                <dt>{t('checked')}</dt>
                <dd className="text-ink">{new Date(health.timestamp).toLocaleTimeString()}</dd>
              </div>
            </dl>
          )}
          {error && <p className="mt-3 text-sm text-danger">{error}</p>}
        </Card>
      </section>
    </div>
  );
}

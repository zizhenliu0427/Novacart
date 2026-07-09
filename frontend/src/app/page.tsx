'use client';

import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { GridIcon, PackageIcon, SparkIcon } from '@/components/icons';

interface HealthStatus {
  status: string;
  timestamp: string;
  environment: string;
}

const features = [
  { Icon: GridIcon, title: 'Any category', body: 'One catalogue for every product type, with details tailored per type.' },
  { Icon: PackageIcon, title: 'Secure checkout', body: 'Tokenised payments, order tracking, and a full purchase history.' },
  { Icon: SparkIcon, title: 'Fast & installable', body: 'A mobile-first PWA that loads fast and works like a native app.' },
];

export default function HomePage() {
  const [health, setHealth] = useState<HealthStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetch('/api/health')
      .then((res) => res.json())
      .then(setHealth)
      .catch((err) => setError(err.message));
  }, []);

  return (
    <div className="space-y-16">
      {/* Hero */}
      <section className="mx-auto max-w-2xl py-12 text-center sm:py-16">
        <span className="mb-5 inline-flex items-center gap-1.5 rounded-full border border-border bg-surface px-3 py-1 text-xs font-medium text-ink-muted">
          <SparkIcon className="h-3.5 w-3.5 text-accent" />
          Modern e-commerce, mobile-first
        </span>
        <h1 className="text-4xl font-semibold tracking-tight text-ink sm:text-5xl">
          Everything you need,
          <br />
          in one modern store.
        </h1>
        <p className="mx-auto mt-4 max-w-lg text-lg text-ink-muted">
          Browse a fast, secure catalogue across every category — from checkout to order history.
        </p>
        <div className="mt-8 flex justify-center gap-3">
          <a href="/products">
            <Button>Browse products</Button>
          </a>
          <a href="/orders">
            <Button variant="secondary">View orders</Button>
          </a>
        </div>
      </section>

      {/* Value props */}
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

      {/* Backend status */}
      <section className="mx-auto max-w-md">
        <Card className="p-5">
          <div className="flex items-center justify-between">
            <h2 className="text-sm font-semibold text-ink">Backend status</h2>
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
              {error ? 'Unreachable' : health ? 'Healthy' : 'Connecting…'}
            </span>
          </div>
          {health && (
            <dl className="mt-3 space-y-1 text-sm text-ink-muted">
              <div className="flex justify-between">
                <dt>Environment</dt>
                <dd className="text-ink">{health.environment}</dd>
              </div>
              <div className="flex justify-between">
                <dt>Checked</dt>
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

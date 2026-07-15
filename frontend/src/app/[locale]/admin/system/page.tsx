'use client';

import { useEffect, useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { useToast } from '@/contexts/ToastContext';
import { apiCall } from '@/lib/api';

interface HealthDetails {
  connected: boolean;
  error: string | null;
}

interface SystemHealthResponse {
  timestamp: string;
  database: HealthDetails;
  redisCache: HealthDetails;
}

interface RabbitMqQueueInfo {
  name: string;
  messages: number;
  messagesReady: number;
  isErrorQueue: boolean;
}

interface MessagingDiagnosticsResponse {
  timestamp: string;
  poisonMessageCount: number;
  errorQueues: RabbitMqQueueInfo[];
  queues: RabbitMqQueueInfo[];
}

interface CheckoutSagaSummary {
  correlationId: string;
  orderId: string;
  currentState: string;
  orderNumber: string;
  userId: string;
  userEmail: string | null;
  orderStatus: string | null;
  canRetry: boolean;
}

interface CheckoutSagaListResponse {
  timestamp: string;
  sagas: CheckoutSagaSummary[];
}

interface DlqRetryResponse {
  queueName: string;
  targetQueue: string;
  messagesRetried: number;
  message: string;
}

export default function AdminSystemPage() {
  const { toast } = useToast();
  const [health, setHealth] = useState<SystemHealthResponse | null>(null);
  const [messaging, setMessaging] = useState<MessagingDiagnosticsResponse | null>(null);
  const [sagas, setSagas] = useState<CheckoutSagaListResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [messagingLoading, setMessagingLoading] = useState(true);
  const [sagasLoading, setSagasLoading] = useState(true);
  const [clearing, setClearing] = useState(false);
  const [retryingQueue, setRetryingQueue] = useState<string | null>(null);
  const [retryingSaga, setRetryingSaga] = useState<string | null>(null);

  async function fetchHealth() {
    setLoading(true);
    try {
      const data = await apiCall<SystemHealthResponse>('/admin/system/health');
      setHealth(data);
    } catch {
      toast.error('Failed to load system diagnostics.');
    } finally {
      setLoading(false);
    }
  }

  async function fetchMessaging() {
    setMessagingLoading(true);
    try {
      const data = await apiCall<MessagingDiagnosticsResponse>('/admin/system/messaging');
      setMessaging(data);
    } catch {
      setMessaging(null);
    } finally {
      setMessagingLoading(false);
    }
  }

  async function fetchSagas() {
    setSagasLoading(true);
    try {
      const data = await apiCall<CheckoutSagaListResponse>('/admin/system/checkout-sagas');
      setSagas(data);
    } catch {
      setSagas(null);
    } finally {
      setSagasLoading(false);
    }
  }

  async function handleClearCache() {
    setClearing(true);
    try {
      await apiCall('/admin/system/clear-cache', { method: 'POST' });
      toast.success('Redis cache flushed successfully.');
      await fetchHealth();
    } catch {
      toast.error('Failed to flush Redis cache.');
    } finally {
      setClearing(false);
    }
  }

  async function handleRetryDlq(queueName: string) {
    setRetryingQueue(queueName);
    try {
      const result = await apiCall<DlqRetryResponse>('/admin/system/messaging/retry-dlq', {
        method: 'POST',
        body: JSON.stringify({ queueName, maxMessages: 10 }),
      });
      toast.success(result.message || `Retried ${result.messagesRetried} message(s).`);
      await fetchMessaging();
    } catch {
      toast.error(`Failed to retry DLQ ${queueName}.`);
    } finally {
      setRetryingQueue(null);
    }
  }

  async function handleRetrySaga(orderId: string) {
    setRetryingSaga(orderId);
    try {
      await apiCall(`/admin/system/checkout-sagas/${orderId}/retry`, { method: 'POST' });
      toast.success('Checkout saga retry published.');
      await fetchSagas();
      await fetchMessaging();
    } catch {
      toast.error('Failed to retry checkout saga.');
    } finally {
      setRetryingSaga(null);
    }
  }

  useEffect(() => {
    fetchHealth();
    fetchMessaging();
    fetchSagas();
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">System maintenance</h1>
        <p className="text-sm text-ink-muted">Diagnose health metrics and run maintenance operations.</p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        <Card className="p-6 space-y-6">
          <h2 className="text-lg font-semibold text-ink">Connection diagnostics</h2>

          {loading ? (
            <div className="space-y-3">
              <div className="h-10 w-full animate-pulse rounded bg-bg-subtle" />
              <div className="h-10 w-full animate-pulse rounded bg-bg-subtle" />
            </div>
          ) : health ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between rounded-lg border border-border p-3">
                <div>
                  <span className="block font-medium text-ink">PostgreSQL Database</span>
                  {health.database.error && (
                    <span className="block text-xs text-danger mt-1 font-mono break-all">{health.database.error}</span>
                  )}
                </div>
                <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  health.database.connected
                    ? 'bg-green-50 text-green-700 dark:bg-green-950/20 dark:text-green-400 border border-green-200 dark:border-green-800'
                    : 'bg-danger/10 text-danger border border-danger/20'
                }`}>
                  {health.database.connected ? 'Connected' : 'Disconnected'}
                </span>
              </div>

              <div className="flex items-center justify-between rounded-lg border border-border p-3">
                <div>
                  <span className="block font-medium text-ink">Redis Cache Server</span>
                  {health.redisCache.error && (
                    <span className="block text-xs text-danger mt-1 font-mono break-all">{health.redisCache.error}</span>
                  )}
                </div>
                <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                  health.redisCache.connected
                    ? 'bg-green-50 text-green-700 dark:bg-green-950/20 dark:text-green-400 border border-green-200 dark:border-green-800'
                    : 'bg-danger/10 text-danger border border-danger/20'
                }`}>
                  {health.redisCache.connected ? 'Connected' : 'Disconnected'}
                </span>
              </div>

              <div className="text-xs text-ink-muted">
                Last checked: {new Date(health.timestamp).toLocaleString()}
              </div>
            </div>
          ) : (
            <p className="text-sm text-danger">Could not retrieve connection details.</p>
          )}

          <Button variant="secondary" onClick={fetchHealth} disabled={loading} className="w-full">
            Refresh Status
          </Button>
        </Card>

        <Card className="p-6 space-y-6">
          <div>
            <h2 className="text-lg font-semibold text-ink">Maintenance operations</h2>
            <p className="text-sm text-ink-muted mt-1">Flush the Redis cache when syncing products or testing pricing rule overrides.</p>
          </div>

          <div className="rounded-lg border border-border p-4 bg-bg-subtle">
            <span className="block text-sm font-semibold text-ink">Flush Redis Cache</span>
            <p className="text-xs text-ink-muted mt-1">
              Flushes list caches, order histories and analytics snapshots. Active customer sessions won&apos;t be invalidated.
            </p>
            <Button
              onClick={handleClearCache}
              disabled={clearing}
              className="mt-4 w-full bg-accent hover:bg-accent/90"
            >
              {clearing ? 'Flushing cache…' : 'Flush Cache'}
            </Button>
          </div>
        </Card>
      </div>

      <Card className="p-6 space-y-4">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold text-ink">Checkout sagas</h2>
            <p className="text-sm text-ink-muted">Failed or stuck MassTransit checkout state machines (pending orders only).</p>
          </div>
          <Button variant="secondary" onClick={fetchSagas} disabled={sagasLoading}>
            Refresh
          </Button>
        </div>

        {sagasLoading ? (
          <div className="h-24 animate-pulse rounded bg-bg-subtle" />
        ) : sagas && sagas.sagas.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-left text-ink-muted">
                  <th className="py-2 pr-4 font-medium">Order</th>
                  <th className="py-2 pr-4 font-medium">Saga state</th>
                  <th className="py-2 pr-4 font-medium">Order status</th>
                  <th className="py-2 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {sagas.sagas.map((s) => (
                  <tr key={s.orderId} className="border-b border-border/60">
                    <td className="py-2 pr-4 font-mono text-xs">{s.orderNumber}</td>
                    <td className="py-2 pr-4">
                      <span className={s.currentState === 'Failed' ? 'text-danger font-medium' : 'text-ink'}>
                        {s.currentState}
                      </span>
                    </td>
                    <td className="py-2 pr-4 text-ink-muted">{s.orderStatus ?? '—'}</td>
                    <td className="py-2">
                      {s.canRetry ? (
                        <Button
                          variant="secondary"
                          className="text-xs"
                          disabled={retryingSaga === s.orderId}
                          onClick={() => handleRetrySaga(s.orderId)}
                        >
                          {retryingSaga === s.orderId ? 'Retrying…' : 'Retry checkout'}
                        </Button>
                      ) : (
                        <span className="text-xs text-ink-muted">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <p className="text-sm text-ink-muted">No failed or stuck checkout sagas.</p>
        )}
      </Card>

      <Card className="p-6 space-y-4">
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-lg font-semibold text-ink">RabbitMQ / DLQ</h2>
            <p className="text-sm text-ink-muted">MassTransit error queues with pending poison messages.</p>
          </div>
          <Button variant="secondary" onClick={fetchMessaging} disabled={messagingLoading}>
            Refresh
          </Button>
        </div>

        {messagingLoading ? (
          <div className="h-24 animate-pulse rounded bg-bg-subtle" />
        ) : messaging ? (
          <div className="space-y-4">
            <div className="flex items-center justify-between rounded-lg border border-border p-3">
              <span className="font-medium text-ink">Poison messages (error queues)</span>
              <span className={`text-sm font-semibold ${messaging.poisonMessageCount > 0 ? 'text-danger' : 'text-ink-muted'}`}>
                {messaging.poisonMessageCount}
              </span>
            </div>

            {messaging.errorQueues.length > 0 ? (
              <ul className="space-y-2 text-sm">
                {messaging.errorQueues.map((q) => (
                  <li key={q.name} className="flex flex-wrap items-center justify-between gap-2 rounded border border-border px-3 py-2">
                    <span className="font-mono text-xs">{q.name}</span>
                    <div className="flex items-center gap-2">
                      <span className="text-danger text-xs">{q.messages} msg</span>
                      {q.messages > 0 && (
                        <Button
                          variant="secondary"
                          className="text-xs"
                          disabled={retryingQueue === q.name}
                          onClick={() => handleRetryDlq(q.name)}
                        >
                          {retryingQueue === q.name ? 'Retrying…' : 'Retry DLQ'}
                        </Button>
                      )}
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="text-sm text-ink-muted">No error queues with pending messages.</p>
            )}

            <p className="text-xs text-ink-muted">
              Last checked: {new Date(messaging.timestamp).toLocaleString()} ·{' '}
              <a href="http://localhost:16686" target="_blank" rel="noreferrer" className="underline">
                Open Jaeger traces
              </a>
            </p>
          </div>
        ) : (
          <p className="text-sm text-ink-muted">Messaging diagnostics unavailable (sysadmin + order-api only).</p>
        )}
      </Card>
    </div>
  );
}

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

export default function AdminSystemPage() {
  const { toast } = useToast();
  const [health, setHealth] = useState<SystemHealthResponse | null>(null);
  const [messaging, setMessaging] = useState<MessagingDiagnosticsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [messagingLoading, setMessagingLoading] = useState(true);
  const [clearing, setClearing] = useState(false);

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

  useEffect(() => {
    fetchHealth();
    fetchMessaging();
  }, []);

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">System maintenance</h1>
        <p className="text-sm text-ink-muted">Diagnose health metrics and run maintenance operations.</p>
      </div>

      <div className="grid gap-6 md:grid-cols-2">
        {/* Connection Diagnostics Card */}
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

        {/* Maintenance Actions Card */}
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
                  <li key={q.name} className="flex justify-between rounded border border-border px-3 py-2 font-mono text-xs">
                    <span>{q.name}</span>
                    <span className="text-danger">{q.messages} msg</span>
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


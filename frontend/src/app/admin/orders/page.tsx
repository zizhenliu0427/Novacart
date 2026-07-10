'use client';

import { useCallback, useEffect, useState } from 'react';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { Input } from '@/components/ui/Input';
import { GridIcon } from '@/components/icons';
import { useAuth } from '@/contexts/AuthContext';
import { apiCall } from '@/lib/api';
import { formatPrice } from '@/types/product';
import type { PagedResult } from '@/types/product';
import type {
  AdminOrderDetail,
  AdminOrderSummary,
  UpdateOrderStatusBody,
} from '@/types/order';

type StatusFilter = 'all' | 'pending' | 'paid' | 'processing' | 'shipped' | 'completed' | 'cancelled';

const ORDER_STATUSES: StatusFilter[] = ['all', 'pending', 'paid', 'processing', 'shipped', 'completed', 'cancelled'];

/**
 * Next legal status for a given current status. Mirrors the backend state machine.
 * `null` means the status is terminal (no forward move available).
 */
const NEXT_STATUS: Record<string, string | null> = {
  pending: 'paid',
  paid: 'processing',
  processing: 'shipped',
  shipped: 'completed',
  completed: null,
  cancelled: null,
};

type BadgeTone = 'neutral' | 'accent' | 'success' | 'warning' | 'danger' | 'sale';

function statusTone(status: string): BadgeTone {
  switch (status) {
    case 'pending': return 'warning';
    case 'paid': return 'accent';
    case 'processing': return 'accent';
    case 'shipped': return 'sale';
    case 'completed': return 'success';
    case 'cancelled': return 'danger';
    default: return 'neutral';
  }
}

export default function AdminOrdersPage() {
  const { token } = useAuth();
  const [orders, setOrders] = useState<AdminOrderSummary[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [page, setPage] = useState(1);
  const [query, setQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [status, setStatus] = useState<StatusFilter>('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  // Detail modal state
  const [detail, setDetail] = useState<AdminOrderDetail | null>(null);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [advancing, setAdvancing] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQuery(query.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [query]);

  const loadOrders = useCallback(async () => {
    if (!token) return;
    setLoading(true);
    setError(null);
    const params = new URLSearchParams({ page: String(page), pageSize: '20' });
    if (debouncedQuery) params.set('q', debouncedQuery);
    if (status !== 'all') params.set('status', status);

    try {
      const data = await apiCall<PagedResult<AdminOrderSummary>>(`/admin/orders?${params}`, { token });
      setOrders(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load orders.');
    } finally {
      setLoading(false);
    }
  }, [token, page, debouncedQuery, status]);

  useEffect(() => {
    loadOrders();
  }, [loadOrders]);

  async function openDetail(orderId: string) {
    if (!token) return;
    setDetailLoading(true);
    setDetailError(null);
    setDetail(null);
    try {
      const data = await apiCall<AdminOrderDetail>(`/admin/orders/${orderId}`, { token });
      setDetail(data);
    } catch (err) {
      setDetailError(err instanceof Error ? err.message : 'Failed to load order detail.');
    } finally {
      setDetailLoading(false);
    }
  }

  function closeDetail() {
    if (advancing) return;
    setDetail(null);
    setDetailError(null);
  }

  async function advanceStatus() {
    if (!token || !detail) return;
    const next = NEXT_STATUS[detail.currentStatus];
    if (!next) return;
    setAdvancing(true);
    setDetailError(null);
    try {
      const updated = await apiCall<AdminOrderDetail>(`/admin/orders/${detail.id}/status`, {
        method: 'PATCH',
        token,
        body: { toStatus: next } satisfies UpdateOrderStatusBody,
      });
      setDetail(updated);
      setNotice(`Order ${updated.orderNumber} → ${updated.currentStatus}.`);
      await loadOrders();
    } catch (err) {
      setDetailError(err instanceof Error ? err.message : 'Failed to advance order status.');
    } finally {
      setAdvancing(false);
    }
  }

  async function cancelOrder() {
    if (!token || !detail) return;
    if (!window.confirm(`Cancel order ${detail.orderNumber}? This cannot be undone.`)) return;
    setAdvancing(true);
    setDetailError(null);
    try {
      const updated = await apiCall<AdminOrderDetail>(`/admin/orders/${detail.id}/status`, {
        method: 'PATCH',
        token,
        body: { toStatus: 'cancelled' } satisfies UpdateOrderStatusBody,
      });
      setDetail(updated);
      setNotice(`Order ${updated.orderNumber} cancelled.`);
      await loadOrders();
    } catch (err) {
      setDetailError(err instanceof Error ? err.message : 'Failed to cancel order.');
    } finally {
      setAdvancing(false);
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Orders</h1>
        <p className="mt-1 text-sm text-ink-muted">
          {loading ? 'Loading orders…' : `${totalCount} order${totalCount === 1 ? '' : 's'}`}
        </p>
      </div>

      {notice && (
        <div className="flex items-center justify-between rounded-lg border border-border bg-bg-subtle px-4 py-3 text-sm text-success">
          <span>{notice}</span>
          <button onClick={() => setNotice(null)} className="text-ink-muted hover:text-ink" aria-label="Dismiss">×</button>
        </div>
      )}

      <div className="flex flex-wrap gap-3">
        <div className="min-w-64 flex-1">
          <Input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search order number or customer email…"
            aria-label="Search orders"
          />
        </div>
        <select
          value={status}
          onChange={(event) => { setStatus(event.target.value as StatusFilter); setPage(1); }}
          className="h-10 rounded-lg border border-border bg-surface px-3 text-sm text-ink focus:border-accent focus:outline-none"
          aria-label="Filter by status"
        >
          {ORDER_STATUSES.map((s) => (
            <option key={s} value={s}>{s === 'all' ? 'All statuses' : s.charAt(0).toUpperCase() + s.slice(1)}</option>
          ))}
        </select>
      </div>

      {error && (
        <EmptyState icon={<GridIcon />} title="Couldn't load orders" description={error} />
      )}

      {!error && (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[720px] text-left text-sm">
              <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
                <tr>
                  <th className="px-4 py-3 font-semibold">Order</th>
                  <th className="px-4 py-3 font-semibold">Customer</th>
                  <th className="px-4 py-3 font-semibold">Total</th>
                  <th className="px-4 py-3 font-semibold">Items</th>
                  <th className="px-4 py-3 font-semibold">Status</th>
                  <th className="px-4 py-3 font-semibold">Date</th>
                  <th className="px-4 py-3 text-right font-semibold">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {loading && Array.from({ length: 5 }).map((_, index) => (
                  <tr key={index} className="animate-pulse">
                    <td colSpan={7} className="px-4 py-4"><div className="h-5 rounded bg-bg-subtle" /></td>
                  </tr>
                ))}
                {!loading && orders.map((order) => (
                  <tr key={order.id} className="hover:bg-bg-subtle/60">
                    <td className="px-4 py-3 font-medium text-ink">{order.orderNumber}</td>
                    <td className="px-4 py-3 text-ink-muted">{order.customerEmail}</td>
                    <td className="px-4 py-3 font-medium text-ink tnum">{formatPrice(order.total, order.currency)}</td>
                    <td className="px-4 py-3 text-ink-muted">{order.itemCount}</td>
                    <td className="px-4 py-3">
                      <Badge tone={statusTone(order.currentStatus)}>{order.currentStatus}</Badge>
                    </td>
                    <td className="px-4 py-3 text-ink-muted">
                      {new Date(order.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end">
                        <Button variant="secondary" size="sm" onClick={() => openDetail(order.id)}>View</Button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {!loading && orders.length === 0 && (
            <div className="p-8 text-center text-sm text-ink-muted">No orders match these filters.</div>
          )}
          {!loading && totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-border px-4 py-3">
              <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>
                Previous
              </Button>
              <span className="text-xs text-ink-muted">Page {page} of {totalPages}</span>
              <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>
                Next
              </Button>
            </div>
          )}
        </Card>
      )}

      {detail && (
        <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/50 p-4 sm:p-8" role="dialog" aria-modal="true" aria-labelledby="order-detail-title">
          <Card className="w-full max-w-2xl p-5 sm:p-6">
            <div className="mb-5 flex items-start justify-between gap-4">
              <div>
                <h2 id="order-detail-title" className="text-xl font-semibold text-ink">{detail.orderNumber}</h2>
                <p className="text-sm text-ink-muted">
                  {detail.customerName || detail.customerEmail} · {new Date(detail.createdAt).toLocaleString()}
                </p>
              </div>
              <button onClick={closeDetail} className="text-2xl leading-none text-ink-muted hover:text-ink" aria-label="Close">×</button>
            </div>

            {detailLoading && <p className="text-sm text-ink-muted">Loading…</p>}
            {detailError && <p className="rounded-lg bg-bg-subtle px-3 py-2 text-sm text-danger">{detailError}</p>}

            {!detailLoading && (
              <>
                <div className="mb-5 flex items-center gap-3">
                  <Badge tone={statusTone(detail.currentStatus)}>{detail.currentStatus}</Badge>
                  {detail.updatedAt && (
                    <span className="text-xs text-ink-muted">
                      Updated {new Date(detail.updatedAt).toLocaleString()}
                    </span>
                  )}
                </div>

                <div className="mb-5 overflow-hidden rounded-lg border border-border">
                  <table className="w-full text-left text-sm">
                    <thead className="bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
                      <tr>
                        <th className="px-3 py-2 font-semibold">Item</th>
                        <th className="px-3 py-2 text-right font-semibold">Price</th>
                        <th className="px-3 py-2 text-right font-semibold">Qty</th>
                        <th className="px-3 py-2 text-right font-semibold">Total</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-border">
                      {detail.items.map((item) => (
                        <tr key={item.id}>
                          <td className="px-3 py-2 text-ink">{item.productName}</td>
                          <td className="px-3 py-2 text-right text-ink-muted tnum">{formatPrice(item.price, detail.currency)}</td>
                          <td className="px-3 py-2 text-right text-ink-muted tnum">{item.quantity}</td>
                          <td className="px-3 py-2 text-right font-medium text-ink tnum">{formatPrice(item.lineTotal, detail.currency)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                <div className="mb-5 space-y-1 text-sm">
                  <div className="flex justify-between text-ink-muted">
                    <span>Subtotal</span><span className="tnum">{formatPrice(detail.subtotal, detail.currency)}</span>
                  </div>
                  <div className="flex justify-between text-ink-muted">
                    <span>Shipping</span><span className="tnum">{formatPrice(detail.shippingCost, detail.currency)}</span>
                  </div>
                  <div className="flex justify-between text-ink-muted">
                    <span>Tax</span><span className="tnum">{formatPrice(detail.tax, detail.currency)}</span>
                  </div>
                  <div className="flex justify-between border-t border-border pt-1 font-semibold text-ink">
                    <span>Total</span><span className="tnum">{formatPrice(detail.total, detail.currency)}</span>
                  </div>
                </div>

                <div className="flex flex-wrap justify-end gap-3 border-t border-border pt-5">
                  {(detail.currentStatus === 'pending' || detail.currentStatus === 'paid') && (
                    <Button variant="ghost" className="text-danger" onClick={cancelOrder} disabled={advancing}>
                      {advancing ? 'Cancelling…' : 'Cancel order'}
                    </Button>
                  )}
                  {NEXT_STATUS[detail.currentStatus] ? (
                    <Button onClick={advanceStatus} disabled={advancing}>
                      {advancing ? 'Updating…' : `Advance to ${NEXT_STATUS[detail.currentStatus]}`}
                    </Button>
                  ) : (
                    <span className="self-center text-sm text-ink-muted">Terminal status — no further moves.</span>
                  )}
                </div>
              </>
            )}
          </Card>
        </div>
      )}
    </div>
  );
}

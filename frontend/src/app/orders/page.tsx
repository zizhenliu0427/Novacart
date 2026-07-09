'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/EmptyState';
import { PackageIcon } from '@/components/icons';
import { formatPrice } from '@/types/product';
import { apiCall } from '@/lib/api';

interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  productSlug?: string;
  price: number;
  quantity: number;
  lineTotal: number;
}

interface Order {
  id: string;
  orderNumber: string;
  subtotal: number;
  shippingCost: number;
  tax: number;
  total: number;
  currency: string;
  currentStatus: string;
  createdAt: string;
  items?: OrderItem[];
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export default function OrdersPage() {
  const { user, token } = useAuth();
  const [orders, setOrders] = useState<Order[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [expandedOrderId, setExpandedOrderId] = useState<string | null>(null);
  const [detailedOrders, setDetailedOrders] = useState<Record<string, Order>>({});
  const [isLoadingDetails, setIsLoadingDetails] = useState<string | null>(null);

  useEffect(() => {
    if (!token) return;

    async function loadOrders() {
      try {
        const res = await apiCall<PagedResult<Order>>('/orders?page=1&pageSize=50', { token: token! });
        setOrders(res.items);
      } catch (err) {
        console.error('Failed loading orders:', err);
      } finally {
        setIsLoading(false);
      }
    }

    loadOrders();
  }, [token]);

  if (!user) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Order history</h1>
        <EmptyState
          icon={<PackageIcon />}
          title="Sign in to view orders"
          description="View your order receipts, details, and delivery updates."
          action={
            <Link href="/login">
              <Button>Sign in</Button>
            </Link>
          }
        />
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Order history</h1>
        <div className="space-y-4">
          {Array.from({ length: 2 }).map((_, i) => (
            <div key={i} className="h-32 animate-pulse rounded-xl bg-bg-subtle" />
          ))}
        </div>
      </div>
    );
  }

  if (orders.length === 0) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Order history</h1>
        <EmptyState
          icon={<PackageIcon />}
          title="No orders yet"
          description="Once you complete a purchase, your orders and their status appear here."
          action={
            <Link href="/products">
              <Button>Start shopping</Button>
            </Link>
          }
        />
      </div>
    );
  }

  async function handleToggleExpand(orderId: string) {
    if (expandedOrderId === orderId) {
      setExpandedOrderId(null);
      return;
    }

    setExpandedOrderId(orderId);

    // Load detailed order if not loaded already
    if (!detailedOrders[orderId]) {
      setIsLoadingDetails(orderId);
      try {
        const detail = await apiCall<Order>(`/orders/${orderId}`, { token: token! });
        setDetailedOrders((prev) => ({ ...prev, [orderId]: detail }));
      } catch (err) {
        console.error('Failed loading order details:', err);
      } finally {
        setIsLoadingDetails(null);
      }
    }
  }

  function getStatusTone(status: string): 'success' | 'warning' | 'danger' | 'accent' | 'neutral' {
    switch (status.toLowerCase()) {
      case 'paid':
      case 'completed':
        return 'success';
      case 'pending':
      case 'processing':
        return 'warning';
      case 'cancelled':
        return 'danger';
      case 'shipped':
        return 'accent';
      default:
        return 'neutral';
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-ink">Order history</h1>

      <div className="space-y-4">
        {orders.map((order) => {
          const isExpanded = expandedOrderId === order.id;
          const details = detailedOrders[order.id];
          const isBusy = isLoadingDetails === order.id;

          return (
            <Card key={order.id} className="overflow-hidden border border-border">
              {/* Header info */}
              <div
                onClick={() => handleToggleExpand(order.id)}
                className="p-5 flex flex-col md:flex-row md:items-center justify-between gap-4 cursor-pointer hover:bg-bg-subtle/50 transition-colors"
              >
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 flex-1">
                  <div>
                    <span className="text-[10px] font-semibold text-ink-muted uppercase tracking-wider block">Order Number</span>
                    <span className="font-semibold text-ink text-sm sm:text-base">{order.orderNumber}</span>
                  </div>
                  <div>
                    <span className="text-[10px] font-semibold text-ink-muted uppercase tracking-wider block">Date Placed</span>
                    <span className="text-sm text-ink font-medium">
                      {new Date(order.createdAt).toLocaleDateString('en-AU', { dateStyle: 'medium' })}
                    </span>
                  </div>
                  <div>
                    <span className="text-[10px] font-semibold text-ink-muted uppercase tracking-wider block">Total Amount</span>
                    <span className="tnum font-bold text-ink text-sm sm:text-base">
                      {formatPrice(order.total, order.currency)}
                    </span>
                  </div>
                  <div>
                    <span className="text-[10px] font-semibold text-ink-muted uppercase tracking-wider block mb-1">Status</span>
                    <Badge tone={getStatusTone(order.currentStatus)}>
                      {order.currentStatus.toUpperCase()}
                    </Badge>
                  </div>
                </div>

                <div className="flex items-center justify-end">
                  <span className="text-xs font-semibold text-accent flex items-center gap-1">
                    {isExpanded ? 'Hide Details' : 'View Details'}
                    <svg
                      className={`h-4 w-4 transform transition-transform ${isExpanded ? 'rotate-180' : ''}`}
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                    >
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                    </svg>
                  </span>
                </div>
              </div>

              {/* Expanded details */}
              {isExpanded && (
                <div className="border-t border-border bg-bg-subtle/20 p-5 space-y-4">
                  {isBusy && (
                    <div className="space-y-2 py-4">
                      <div className="h-4 animate-pulse rounded bg-border w-1/3" />
                      <div className="h-4 animate-pulse rounded bg-border w-1/2" />
                    </div>
                  )}

                  {!isBusy && details && (
                    <>
                      <div className="space-y-2">
                        <h3 className="text-sm font-semibold text-ink">Items Purchased</h3>
                        <div className="divide-y divide-border border border-border rounded-xl bg-surface">
                          {details.items?.map((item) => (
                            <div key={item.id} className="p-4 flex items-center justify-between gap-4 text-sm">
                              <div>
                                {item.productSlug ? (
                                  <Link
                                    href={`/products/${item.productId}`}
                                    className="font-medium text-ink hover:text-accent font-semibold"
                                  >
                                    {item.productName}
                                  </Link>
                                ) : (
                                  <span className="font-medium text-ink">{item.productName}</span>
                                )}
                                <span className="text-xs text-ink-muted block mt-1">
                                  Qty: {item.quantity} @ {formatPrice(item.price, order.currency)} each
                                </span>
                              </div>
                              <span className="tnum font-bold text-ink shrink-0">
                                {formatPrice(item.lineTotal, order.currency)}
                              </span>
                            </div>
                          ))}
                        </div>
                      </div>

                      {/* Financial details breakdown */}
                      <div className="flex justify-end pt-2">
                        <div className="w-full sm:w-64 space-y-1.5 text-xs text-ink-muted">
                          <div className="flex justify-between">
                            <span>Subtotal:</span>
                            <span className="tnum font-medium text-ink">{formatPrice(details.subtotal, order.currency)}</span>
                          </div>
                          <div className="flex justify-between">
                            <span>Shipping & Handling:</span>
                            <span className="tnum font-medium text-ink">{formatPrice(details.shippingCost, order.currency)}</span>
                          </div>
                          <div className="flex justify-between">
                            <span>GST (10%):</span>
                            <span className="tnum font-medium text-ink">{formatPrice(details.tax, order.currency)}</span>
                          </div>
                          <div className="border-t border-border pt-1.5 flex justify-between text-sm font-bold text-ink">
                            <span>Grand Total:</span>
                            <span className="tnum text-accent">{formatPrice(details.total, order.currency)}</span>
                          </div>
                        </div>
                      </div>
                    </>
                  )}
                </div>
              )}
            </Card>
          );
        })}
      </div>
    </div>
  );
}

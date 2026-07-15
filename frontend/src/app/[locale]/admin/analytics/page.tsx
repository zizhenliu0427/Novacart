'use client';

import { useEffect, useState } from 'react';
import dynamic from 'next/dynamic';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';
import { useFormatAudPrice } from '@/hooks/useFormatAudPrice';

// ECharts touches `window` on mount, so the chart must render client-side only.
const SalesChart = dynamic(() => import('@/components/SalesChart'), { ssr: false });

type Summary = {
  totalRevenue: number;
  totalOrders: number;
  totalUnitsSold: number;
  averageOrderValue: number;
};

type SalesPoint = {
  date: string;
  revenue: number;
  orders: number;
};

type BestSeller = {
  productId: string;
  name: string;
  unitsSold: number;
  revenue: number;
};

type LowStockProduct = {
  productId: string;
  name: string;
  stockQuantity: number;
};

export default function AdminAnalyticsPage() {
  const { formatAud } = useFormatAudPrice();
  const [summary, setSummary] = useState<Summary | null>(null);
  const [salesData, setSalesData] = useState<SalesPoint[]>([]);
  const [bestSellers, setBestSellers] = useState<BestSeller[]>([]);
  const [lowStock, setLowStock] = useState<LowStockProduct[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchAnalytics() {
      setLoading(true);
      setError(null);
      try {
        const [sumRes, salesRes, bestRes, lowRes] = await Promise.all([
          fetch('/api/admin/analytics/summary'),
          fetch('/api/admin/analytics/sales-over-time?days=30'),
          fetch('/api/admin/analytics/best-sellers?top=10'),
          fetch('/api/admin/analytics/low-stock?threshold=10'),
        ]);

        if (!sumRes.ok || !salesRes.ok || !bestRes.ok || !lowRes.ok) {
          throw new Error('Failed to load one or more analytics endpoints.');
        }

        const sumData = await sumRes.json();
        const salesData = await salesRes.json();
        const bestData = await bestRes.json();
        const lowData = await lowRes.json();

        setSummary(sumData);
        setSalesData(salesData);
        setBestSellers(bestData);
        setLowStock(lowData);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'An error occurred fetching analytics.');
      } finally {
        setLoading(false);
      }
    }

    fetchAnalytics();
  }, []);

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold text-ink">Analytics Dashboard</h1>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Card key={i} className="h-24 animate-pulse bg-bg-subtle" />
          ))}
        </div>
        <Card className="h-64 animate-pulse bg-bg-subtle" />
      </div>
    );
  }

  if (error) {
    return (
      <EmptyState
        icon={<GridIcon />}
        title="Failed to load dashboard"
        description={error}
      />
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Analytics Dashboard</h1>
        <p className="text-sm text-ink-muted">Overview of paid store metrics and aggregate sales.</p>
      </div>

      {/* KPI Cards */}
      {summary && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <Card className="p-4 space-y-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-ink-muted">Total Revenue</span>
            <p className="text-2xl font-bold text-ink">{formatAud(summary.totalRevenue)}</p>
          </Card>
          <Card className="p-4 space-y-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-ink-muted">Total Orders</span>
            <p className="text-2xl font-bold text-ink">{summary.totalOrders}</p>
          </Card>
          <Card className="p-4 space-y-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-ink-muted">Units Sold</span>
            <p className="text-2xl font-bold text-ink">{summary.totalUnitsSold}</p>
          </Card>
          <Card className="p-4 space-y-2">
            <span className="text-xs font-semibold uppercase tracking-wider text-ink-muted">Average Order Value</span>
            <p className="text-2xl font-bold text-ink">{formatAud(summary.averageOrderValue)}</p>
          </Card>
        </div>
      )}

      {/* Sales Chart */}
      <Card className="p-5 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-base font-semibold text-ink">Sales Over Time (Last 30 Days)</h2>
        </div>

        {salesData.length === 0 ? (
          <div className="flex h-44 items-center justify-center text-sm text-ink-muted">No sales data recorded.</div>
        ) : (
          <SalesChart data={salesData} />
        )}
      </Card>

      {/* Best Sellers */}
      <Card className="p-5 space-y-4">
        <h2 className="text-base font-semibold text-ink">Top 10 Best Sellers</h2>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th className="px-4 py-3 font-semibold">Product Name</th>
                <th className="px-4 py-3 font-semibold text-right">Units Sold</th>
                <th className="px-4 py-3 text-right font-semibold">Total Revenue</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {bestSellers.length === 0 ? (
                <tr>
                  <td colSpan={3} className="px-4 py-8 text-center text-ink-muted">No sales yet.</td>
                </tr>
              ) : (
                bestSellers.map((b) => (
                  <tr key={b.productId} className="hover:bg-bg-subtle/40">
                    <td className="px-4 py-3 font-medium text-ink">{b.name}</td>
                    <td className="px-4 py-3 text-right text-ink-muted">{b.unitsSold}</td>
                    <td className="px-4 py-3 text-right font-medium text-ink">{formatAud(b.revenue)}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Low Stock Alerts */}
      <Card className="p-5 space-y-4">
        <h2 className="text-base font-semibold text-ink text-danger">Low Stock Alerts (Stock &le; 10)</h2>
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-border bg-bg-subtle text-xs uppercase tracking-wide text-ink-muted">
              <tr>
                <th className="px-4 py-3 font-semibold">Product Name</th>
                <th className="px-4 py-3 text-right font-semibold">Stock Quantity</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {lowStock.length === 0 ? (
                <tr>
                  <td colSpan={2} className="px-4 py-8 text-center text-ink-muted">All active products have healthy stock levels.</td>
                </tr>
              ) : (
                lowStock.map((l) => (
                  <tr key={l.productId} className="hover:bg-bg-subtle/40">
                    <td className="px-4 py-3 font-medium text-ink">{l.name}</td>
                    <td className="px-4 py-3 text-right">
                      <span className="font-semibold text-danger">{l.stockQuantity}</span>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </Card>
    </div>
  );
}

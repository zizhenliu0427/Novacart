'use client';

import { useMemo } from 'react';
import ReactECharts from 'echarts-for-react';
import { useFormatAudPrice } from '@/hooks/useFormatAudPrice';

type SalesPoint = {
  date: string;
  revenue: number;
  orders: number;
};

type SalesChartProps = {
  data: SalesPoint[];
  /**
   * Height of the chart in pixels. Defaults to 320 for a comfortable admin viewport.
   * Passed straight through to echarts-for-react's `style` prop.
   */
  height?: number;
};

/**
 * ECharts line + area chart of daily revenue over time, with an orders series
 * surfaced in the tooltip. Colour follows the design-system accent token so the
 * chart blends with the rest of the admin UI regardless of light/dark theme.
 *
 * Rendered client-side only (see the dynamic import in analytics/page.tsx) because
 * echarts-for-react touches `window` during mount.
 */
export default function SalesChart({ data, height = 320 }: SalesChartProps) {
  const { formatAud } = useFormatAudPrice();

  const option = useMemo(() => {
    const dates = data.map((d) => d.date);
    const revenues = data.map((d) => d.revenue);
    const orders = data.map((d) => d.orders);

    return {
      // Keep the chart quiet: no title (the card already has one), no heavy grid.
      grid: { left: 8, right: 16, top: 16, bottom: 8, containLabel: true },
      tooltip: {
        trigger: 'axis',
        backgroundColor: 'rgba(17, 24, 39, 0.92)',
        borderWidth: 0,
        textStyle: { color: '#f9fafb', fontSize: 12 },
        valueFormatter: (value: number, index: number) =>
          index === 0 ? formatAud(value) : `${value} order${value === 1 ? '' : 's'}`,
      },
      xAxis: {
        type: 'category',
        boundaryGap: false,
        data: dates,
        axisLine: { lineStyle: { color: 'var(--border)' } },
        axisLabel: { color: 'var(--ink-muted)', fontSize: 11 },
      },
      yAxis: [
        {
          type: 'value',
          axisLabel: {
            color: 'var(--ink-muted)',
            fontSize: 11,
            formatter: (value: number) => formatAud(value),
          },
          splitLine: { lineStyle: { color: 'var(--border)' } },
        },
        {
          type: 'value',
          axisLabel: {
            color: 'var(--ink-muted)',
            fontSize: 11,
          },
          splitLine: { show: false },
        },
      ],
      series: [
        {
          name: 'Revenue',
          type: 'line',
          smooth: true,
          showSymbol: false,
          data: revenues,
          yAxisIndex: 0,
          lineStyle: { color: 'var(--accent)', width: 2 },
          itemStyle: { color: 'var(--accent)' },
          areaStyle: {
            // Gradient fading from accent to transparent, mirroring the old SVG look.
            color: {
              type: 'linear',
              x: 0, y: 0, x2: 0, y2: 1,
              colorStops: [
                { offset: 0, color: 'var(--accent)' },
                { offset: 1, color: 'transparent' },
              ],
            },
            opacity: 0.25,
          },
        },
        {
          name: 'Orders',
          type: 'bar',
          data: orders,
          yAxisIndex: 1,
          barWidth: '40%',
          itemStyle: { color: 'var(--border)', borderRadius: [3, 3, 0, 0] },
        },
      ],
    };
  }, [data, formatAud]);

  return (
    <ReactECharts
      option={option}
      style={{ width: '100%', height }}
      opts={{ renderer: 'svg' }}
      notMerge
      lazyUpdate
    />
  );
}

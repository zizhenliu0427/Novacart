import { ComingSoon } from '@/components/ui/ComingSoon';

export default function AdminAnalyticsPage() {
  return (
    <ComingSoon title="Analytics" item="P2-9">
      Sales dashboard (ECharts): total revenue, orders/day, revenue summary, best-sellers.
      Calls <code>/api/admin/analytics/*</code> (RBAC-guarded).
    </ComingSoon>
  );
}

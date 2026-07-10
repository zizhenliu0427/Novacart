import { ComingSoon } from '@/components/ui/ComingSoon';

export default function AdminOrdersPage() {
  return (
    <ComingSoon title="Orders" item="P2-7 / P2-8">
      All orders with status management — advance <code>pending → paid → processing → shipped → completed</code>.
      Calls <code>/api/admin/orders</code> (RBAC-guarded).
    </ComingSoon>
  );
}

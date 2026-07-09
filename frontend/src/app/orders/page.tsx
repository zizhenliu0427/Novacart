import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import { PackageIcon } from '@/components/icons';

export default function OrdersPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-ink">Order history</h1>
      <EmptyState
        icon={<PackageIcon />}
        title="No orders yet"
        description="Once you complete a purchase, your orders and their status appear here."
        action={
          <a href="/products">
            <Button>Start shopping</Button>
          </a>
        }
      />
    </div>
  );
}

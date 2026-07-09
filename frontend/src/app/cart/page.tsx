import { EmptyState } from '@/components/ui/EmptyState';
import { Button } from '@/components/ui/Button';
import { CartIcon } from '@/components/icons';

export default function CartPage() {
  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold tracking-tight text-ink">Your cart</h1>
      <EmptyState
        icon={<CartIcon />}
        title="Your cart is empty"
        description="Browse the catalogue and add items — they'll show up here."
        action={
          <a href="/products">
            <Button>Browse products</Button>
          </a>
        }
      />
    </div>
  );
}

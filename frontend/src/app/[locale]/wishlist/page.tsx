'use client';

import { Link } from '@/i18n/navigation';
import { useAuth } from '@/contexts/AuthContext';
import { useWishlist } from '@/contexts/WishlistContext';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { EmptyState } from '@/components/ui/EmptyState';
import { GridIcon } from '@/components/icons';
import { useFormatAudPrice } from '@/hooks/useFormatAudPrice';

/** P2-3 (Wishlist) — backed by the wishlist API via WishlistContext. */
export default function WishlistPage() {
  const { user, isLoading: authLoading } = useAuth();
  const { items, toggle, isLoading } = useWishlist();
  const { formatAud } = useFormatAudPrice();

  if (authLoading) return <p className="text-sm text-ink-muted">Loading…</p>;

  if (!user) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Wishlist</h1>
        <EmptyState
          icon={<GridIcon />}
          title="Sign in to view your wishlist"
          action={
            <Link href="/login">
              <Button>Sign in</Button>
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-ink">Wishlist</h1>
        <p className="mt-1 text-sm text-ink-muted">
          {isLoading ? 'Loading…' : `${items.length} saved item${items.length === 1 ? '' : 's'}`}
        </p>
      </div>

      {!isLoading && items.length === 0 && (
        <EmptyState
          icon={<GridIcon />}
          title="Your wishlist is empty"
          description="Tap the heart on a product to save it for later."
          action={
            <Link href="/products">
              <Button>Browse products</Button>
            </Link>
          }
        />
      )}

      {items.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {items.map((item) => (
            <Card key={item.productId} className="flex flex-col gap-3 p-4">
              <Link href={`/products/${item.productId}`} className="flex-1">
                <h3 className="font-medium text-ink hover:text-accent">{item.name}</h3>
                <p className="mt-1 text-sm text-ink-muted tnum">{formatAud(item.price)}</p>
              </Link>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" onClick={() => toggle(item.productId)}>
                  Remove
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}

'use client';

import Link from 'next/link';
import { useAuth } from '@/contexts/AuthContext';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { ComingSoon } from '@/components/ui/ComingSoon';
import { GridIcon } from '@/components/icons';

/** P2-3 (Wishlist) — SCAFFOLD. Uses WishlistContext (local-only today). */
export default function WishlistPage() {
  const { user, isLoading } = useAuth();

  if (isLoading) return <p className="text-sm text-ink-muted">Loading…</p>;

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
    <ComingSoon title="Wishlist" item="P2-3">
      Save products for later. Hydrate from <code>GET /api/wishlist</code> and toggle via a heart on
      product cards / detail (WishlistContext is scaffolded).
    </ComingSoon>
  );
}

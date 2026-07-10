'use client';

import { createContext, useCallback, useContext, useMemo, useState } from 'react';

/**
 * P2-3 (Wishlist) — SCAFFOLD context. Holds local state only for now.
 * TODO P2-3: hydrate from `GET /api/wishlist` on auth, and call
 * `POST /api/wishlist/items` / `DELETE /api/wishlist/items/{productId}` in `toggle`.
 */
interface WishlistContextValue {
  productIds: Set<string>;
  isWishlisted: (productId: string) => boolean;
  toggle: (productId: string) => Promise<void>;
  isLoading: boolean;
}

const WishlistContext = createContext<WishlistContextValue | undefined>(undefined);

export function WishlistProvider({ children }: { children: React.ReactNode }) {
  const [productIds, setProductIds] = useState<Set<string>>(new Set());
  const [isLoading] = useState(false);

  const isWishlisted = useCallback((id: string) => productIds.has(id), [productIds]);

  const toggle = useCallback(async (id: string) => {
    // TODO P2-3: persist via the wishlist API; optimistic local update for now.
    setProductIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ productIds, isWishlisted, toggle, isLoading }),
    [productIds, isWishlisted, toggle, isLoading],
  );

  return <WishlistContext.Provider value={value}>{children}</WishlistContext.Provider>;
}

export function useWishlist(): WishlistContextValue {
  const ctx = useContext(WishlistContext);
  if (!ctx) throw new Error('useWishlist must be used within a WishlistProvider');
  return ctx;
}

'use client';

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { apiCall } from '@/lib/api';
import { useAuth } from './AuthContext';

interface WishlistItem {
  productId: string;
  name: string;
  slug: string;
  price: number;
  addedAt: string;
}

interface WishlistContextValue {
  productIds: Set<string>;
  items: WishlistItem[];
  isWishlisted: (productId: string) => boolean;
  toggle: (productId: string) => Promise<void>;
  isLoading: boolean;
}

const WishlistContext = createContext<WishlistContextValue | undefined>(undefined);

export function WishlistProvider({ children }: { children: React.ReactNode }) {
  const { token, user } = useAuth();
  const [items, setItems] = useState<WishlistItem[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Hydrate the wishlist when the user is authenticated.
  useEffect(() => {
    if (!token || !user) {
      setItems([]);
      return;
    }
    setIsLoading(true);
    apiCall<WishlistItem[]>('/wishlist', { token })
      .then(setItems)
      .catch(() => setItems([]))
      .finally(() => setIsLoading(false));
  }, [token, user]);

  const productIds = useMemo(() => new Set(items.map((i) => i.productId)), [items]);

  const isWishlisted = useCallback((id: string) => productIds.has(id), [productIds]);

  const toggle = useCallback(async (id: string) => {
    if (!token) return;
    const isAdded = productIds.has(id);
    // Optimistic update
    setItems((prev) =>
      isAdded
        ? prev.filter((i) => i.productId !== id)
        : prev,
    );

    try {
      if (isAdded) {
        await apiCall<void>(`/wishlist/items/${id}`, { method: 'DELETE', token });
      } else {
        await apiCall<void>('/wishlist/items', { method: 'POST', token, body: { productId: id } });
        // Re-fetch to get the full item data (name/price/etc.)
        const refreshed = await apiCall<WishlistItem[]>('/wishlist', { token });
        setItems(refreshed);
      }
    } catch {
      // Revert optimistic update on failure by re-fetching.
      const refreshed = await apiCall<WishlistItem[]>('/wishlist', { token }).catch(() => []);
      setItems(refreshed);
    }
  }, [token, productIds]);

  const value = useMemo(
    () => ({ productIds, items, isWishlisted, toggle, isLoading }),
    [productIds, items, isWishlisted, toggle, isLoading],
  );

  return <WishlistContext.Provider value={value}>{children}</WishlistContext.Provider>;
}

export function useWishlist(): WishlistContextValue {
  const ctx = useContext(WishlistContext);
  if (!ctx) throw new Error('useWishlist must be used within a WishlistProvider');
  return ctx;
}

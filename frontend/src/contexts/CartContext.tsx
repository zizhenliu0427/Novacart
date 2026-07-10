'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import type { Cart, CartItem } from '@/types/cart';
import { apiCall } from '@/lib/api';
import { useAuth } from './AuthContext';

/* ─── Types ─────────────────────────────────────────────────── */

interface CartContextValue {
  cart: Cart | null;
  isLoading: boolean;
  totalItems: number;
  addItem: (productId: string, quantity?: number) => Promise<void>;
  updateItem: (cartItemId: string, quantity: number) => Promise<void>;
  removeItem: (cartItemId: string) => Promise<void>;
}

/* ─── Context ────────────────────────────────────────────────── */

const CartContext = createContext<CartContextValue | null>(null);

/* ─── Provider ───────────────────────────────────────────────── */

export function CartProvider({ children }: { children: React.ReactNode }) {
  const { user } = useAuth();
  const [cart, setCart] = useState<Cart | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  /** Load cart whenever the user logs in. Clear when logged out. */
  useEffect(() => {
    if (!user) {
      setCart(null);
      return;
    }
    setIsLoading(true);
    apiCall<Cart>('/cart')
      .then(setCart)
      .catch(() => setCart(null))
      .finally(() => setIsLoading(false));
  }, [user]);

  const addItem = useCallback(
    async (productId: string, quantity = 1) => {
      if (!user) throw new Error('Must be logged in to add to cart');
      const updated = await apiCall<Cart>('/cart/items', {
        method: 'POST',
        body: { productId, quantity },
      });
      setCart(updated);
    },
    [user],
  );

  const updateItem = useCallback(
    async (cartItemId: string, quantity: number) => {
      if (!user) return;
      const updated = await apiCall<Cart>(`/cart/items/${cartItemId}`, {
        method: 'PUT',
        body: { quantity },
      });
      setCart(updated);
    },
    [user],
  );

  const removeItem = useCallback(
    async (cartItemId: string) => {
      if (!user) return;
      const updated = await apiCall<Cart>(`/cart/items/${cartItemId}`, {
        method: 'DELETE',
      });
      setCart(updated);
    },
    [user],
  );

  const totalItems = useMemo(() => cart?.totalItems ?? 0, [cart]);

  const value = useMemo<CartContextValue>(
    () => ({ cart, isLoading, totalItems, addItem, updateItem, removeItem }),
    [cart, isLoading, totalItems, addItem, updateItem, removeItem],
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

/* ─── Hook ───────────────────────────────────────────────────── */

export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (!ctx) throw new Error('useCart must be used inside <CartProvider>');
  return ctx;
}

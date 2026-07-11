'use client';

import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import type { Cart } from '@/types/cart';
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

  /** Load/merge cart on login, load anonymous cart on mount/logout. */
  useEffect(() => {
    async function initCart() {
      setIsLoading(true);
      try {
        if (user) {
          // Trigger merge of guest cart items
          await apiCall('/cart/merge', { method: 'POST' }).catch(() => {});
        }
        const currentCart = await apiCall<Cart>('/cart');
        setCart(currentCart);
      } catch {
        setCart(null);
      } finally {
        setIsLoading(false);
      }
    }

    initCart();
  }, [user]);

  const addItem = useCallback(
    async (productId: string, quantity = 1) => {
      const updated = await apiCall<Cart>('/cart/items', {
        method: 'POST',
        body: { productId, quantity },
      });
      setCart(updated);
    },
    [],
  );

  const updateItem = useCallback(
    async (cartItemId: string, quantity: number) => {
      const updated = await apiCall<Cart>(`/cart/items/${cartItemId}`, {
        method: 'PUT',
        body: { quantity },
      });
      setCart(updated);
    },
    [],
  );

  const removeItem = useCallback(
    async (cartItemId: string) => {
      const updated = await apiCall<Cart>(`/cart/items/${cartItemId}`, {
        method: 'DELETE',
      });
      setCart(updated);
    },
    [],
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

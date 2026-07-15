'use client';

import { useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { CartIcon, HeartIcon, HeartFilledIcon } from '@/components/icons';
import { formatPrice, type Product } from '@/types/product';
import { useAuth } from '@/contexts/AuthContext';
import { useCart } from '@/contexts/CartContext';
import { useWishlist } from '@/contexts/WishlistContext';

/**
 * Category-agnostic product card — no vertical-specific ornament, works for any
 * product type. Image is a neutral placeholder until real imagery lands.
 */
export function ProductCard({ product }: { product: Product }) {
  const { user } = useAuth();
  const { addItem } = useCart();
  const { isWishlisted, toggle } = useWishlist();
  const router = useRouter();
  const [adding, setAdding] = useState(false);

  const onSale =
    typeof product.compareAtPrice === 'number' && product.compareAtPrice > product.price;

  const outOfStock = product.stockQuantity === 0;
  const wishlisted = isWishlisted(product.id);

  async function handleAddToCart(e: React.MouseEvent) {
    e.preventDefault(); // don't follow Link
    if (!user) {
      router.push('/login');
      return;
    }
    setAdding(true);
    try {
      await addItem(product.id, 1);
    } catch {
      // Silently fail here — errors are surfaced on the cart page.
    } finally {
      setAdding(false);
    }
  }

  async function handleToggleWishlist(e: React.MouseEvent) {
    e.preventDefault(); // don't follow Link
    if (!user) {
      router.push('/login');
      return;
    }
    await toggle(product.id);
  }

  return (
    <Card interactive className="flex flex-col overflow-hidden">
      <Link href={`/products/${product.id}`} className="flex flex-1 flex-col">
        <div className="relative flex aspect-square items-center justify-center bg-bg-subtle overflow-hidden">
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              className="h-full w-full object-cover transition-transform duration-300 hover:scale-105"
            />
          ) : (
            <span className="select-none text-3xl font-semibold text-ink-muted/50">
              {product.name.charAt(0)}
            </span>
          )}
          {onSale && (
            <span className="absolute left-3 top-3">
              <Badge tone="sale">Sale</Badge>
            </span>
          )}
          {outOfStock && (
            <span className="absolute right-3 top-3">
              <Badge tone="danger">Out of stock</Badge>
            </span>
          )}
          {/* Wishlist heart toggle */}
          <button
            onClick={handleToggleWishlist}
            aria-label={wishlisted ? `Remove ${product.name} from wishlist` : `Add ${product.name} to wishlist`}
            className={`absolute bottom-3 right-3 rounded-full p-1.5 transition ${
              wishlisted
                ? 'bg-danger/10 text-danger hover:bg-danger/20'
                : 'bg-surface/80 text-ink-muted hover:bg-surface hover:text-danger'
            }`}
          >
            {wishlisted ? (
              <HeartFilledIcon className="h-[18px] w-[18px]" />
            ) : (
              <HeartIcon className="h-[18px] w-[18px]" />
            )}
          </button>
        </div>

        <div className="flex flex-1 flex-col gap-2 p-4">
          <span className="text-xs font-medium uppercase tracking-wide text-ink-muted">
            {product.categoryName ?? product.tags?.[0] ?? ''}
          </span>
          <h3 className="line-clamp-2 font-semibold leading-snug text-ink">{product.name}</h3>
          <p className="line-clamp-2 text-sm text-ink-muted">{product.description}</p>
        </div>
      </Link>

      <div className="px-4 pb-4">
        <div className="flex items-center justify-between">
          <div className="flex items-baseline gap-2">
            <span className="tnum text-lg font-semibold text-ink">
              {formatPrice(product.price)}
            </span>
            {onSale && (
              <span className="tnum text-sm text-ink-muted line-through">
                {formatPrice(product.compareAtPrice!)}
              </span>
            )}
          </div>
          <Button
            size="sm"
            aria-label={`Add ${product.name} to cart`}
            onClick={handleAddToCart}
            disabled={outOfStock || adding}
          >
            <CartIcon className="h-4 w-4" />
            {adding ? '…' : 'Add'}
          </Button>
        </div>
      </div>
    </Card>
  );
}

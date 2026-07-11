'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { CartIcon, HeartIcon, HeartFilledIcon } from '@/components/icons';
import {
  formatPrice,
  parseMetadata,
  formatMetadataValue,
  humanizeKey,
  type Product,
} from '@/types/product';
import { useAuth } from '@/contexts/AuthContext';
import { useWishlist } from '@/contexts/WishlistContext';
import { useCart } from '@/contexts/CartContext';

function WishlistButton({ productId, productName }: { productId: string; productName: string }) {
  const { user } = useAuth();
  const { isWishlisted, toggle } = useWishlist();
  const router = useRouter();
  const wishlisted = isWishlisted(productId);

  async function handleClick() {
    if (!user) { router.push('/login'); return; }
    await toggle(productId);
  }

  return (
    <Button variant="secondary" onClick={handleClick} aria-label={wishlisted ? `Remove ${productName} from wishlist` : `Save ${productName} for later`}>
      {wishlisted ? <HeartFilledIcon className="h-4 w-4 text-danger" /> : <HeartIcon className="h-4 w-4" />}
      {wishlisted ? 'Saved' : 'Save for later'}
    </Button>
  );
}

export default function ProductDetailPage({ params }: { params: { id: string } }) {
  const { user } = useAuth();
  const { addItem } = useCart();
  const router = useRouter();

  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [adding, setAdding] = useState(false);
  const [addedFeedback, setAddedFeedback] = useState(false);

  useEffect(() => {
    fetch(`/api/products/${params.id}`)
      .then((res) => {
        if (!res.ok) throw new Error(`Request failed (${res.status})`);
        return res.json();
      })
      .then(setProduct)
      .catch((err) => setError(err.message))
      .finally(() => setLoading(false));
  }, [params.id]);

  async function handleAddToCart() {
    if (!user) { router.push('/login'); return; }
    if (!product) return;
    setAdding(true);
    try {
      await addItem(product.id, 1);
      setAddedFeedback(true);
      setTimeout(() => setAddedFeedback(false), 1800);
    } catch {
      // silent — product detail doesn't have inline error for cart
    } finally {
      setAdding(false);
    }
  }

  const metadata = parseMetadata(product?.metadata);
  const metaEntries = Object.entries(metadata);
  const outOfStock = product ? product.stockQuantity === 0 : false;
  const lowStock = product ? product.stockQuantity > 0 && product.stockQuantity <= 5 : false;

  return (
    <div className="space-y-6">
      <a href="/products" className="inline-flex text-sm text-ink-muted transition hover:text-ink">
        ← Back to products
      </a>

      {/* Loading skeleton */}
      {loading && (
        <div className="grid gap-8 lg:grid-cols-2">
          <div className="aspect-square animate-pulse rounded-xl bg-bg-subtle" />
          <div className="space-y-4">
            <div className="h-4 w-24 animate-pulse rounded bg-bg-subtle" />
            <div className="h-8 w-2/3 animate-pulse rounded bg-bg-subtle" />
            <div className="h-6 w-24 animate-pulse rounded bg-bg-subtle" />
            <div className="h-20 w-full animate-pulse rounded bg-bg-subtle" />
            <div className="h-32 w-full animate-pulse rounded bg-bg-subtle" />
          </div>
        </div>
      )}

      {error && <p className="text-sm text-danger">Couldn&apos;t load this product: {error}</p>}

      {product && !loading && (
        <div className="grid gap-8 lg:grid-cols-2">
          {/* Image placeholder */}
          <div className="flex aspect-square items-center justify-center rounded-xl border border-border bg-bg-subtle">
            <span className="select-none text-8xl font-semibold text-ink-muted/30">
              {product.name.charAt(0)}
            </span>
          </div>

          {/* Info */}
          <div className="flex flex-col gap-5">
            {/* Category + title */}
            <div>
              {product.categoryName && (
                <Badge tone="accent">{product.categoryName}</Badge>
              )}
              <h1 className="mt-3 text-3xl font-semibold tracking-tight text-ink">
                {product.name}
              </h1>
            </div>

            {/* Price */}
            <div className="flex items-baseline gap-3">
              <p className="tnum text-2xl font-semibold text-ink">{formatPrice(product.price)}</p>
              {product.compareAtPrice && product.compareAtPrice > product.price && (
                <p className="tnum text-lg text-ink-muted line-through">
                  {formatPrice(product.compareAtPrice)}
                </p>
              )}
            </div>

            {/* Stock */}
            {outOfStock && (
              <Badge tone="danger" className="w-fit">Out of stock</Badge>
            )}
            {lowStock && !outOfStock && (
              <Badge tone="warning" className="w-fit">
                Only {product.stockQuantity} left
              </Badge>
            )}

            {/* Description */}
            {product.description && (
              <p className="text-ink-muted">{product.description}</p>
            )}

            {/* Tags */}
            {product.tags?.length > 0 && (
              <div className="flex flex-wrap gap-1.5">
                {product.tags.map((tag) => (
                  <Badge key={tag} tone="neutral">{tag}</Badge>
                ))}
              </div>
            )}

            {/* Dynamic attribute table from metadata */}
            {metaEntries.length > 0 && (
              <div className="rounded-xl border border-border bg-bg-subtle p-4">
                <h2 className="mb-3 text-sm font-semibold text-ink">Specifications</h2>
                <dl className="grid grid-cols-[auto_1fr] gap-x-6 gap-y-2 text-sm">
                  {metaEntries.map(([key, value]) => (
                    <div key={key} className="contents">
                      <dt className="text-ink-muted">{humanizeKey(key)}</dt>
                      <dd className="font-medium text-ink">{formatMetadataValue(value)}</dd>
                    </div>
                  ))}
                </dl>
              </div>
            )}

            {/* Actions */}
            <div className="mt-2 flex gap-3">
              <Button
                onClick={handleAddToCart}
                disabled={outOfStock || adding}
                aria-label={`Add ${product.name} to cart`}
              >
                <CartIcon className="h-4 w-4" />
                {addedFeedback ? 'Added ✓' : adding ? 'Adding…' : 'Add to cart'}
              </Button>
              <WishlistButton productId={product.id} productName={product.name} />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

'use client';

import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { CartIcon } from '@/components/icons';
import { formatPrice, type Product } from '@/types/product';

export default function ProductDetailPage({ params }: { params: { id: string } }) {
  const [product, setProduct] = useState<Product | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

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

  return (
    <div className="space-y-6">
      <a href="/products" className="inline-flex text-sm text-ink-muted transition hover:text-ink">
        ← Back to products
      </a>

      {loading && (
        <div className="grid gap-8 lg:grid-cols-2">
          <div className="aspect-square animate-pulse rounded-xl bg-bg-subtle" />
          <div className="space-y-4">
            <div className="h-4 w-24 animate-pulse rounded bg-bg-subtle" />
            <div className="h-8 w-2/3 animate-pulse rounded bg-bg-subtle" />
            <div className="h-6 w-24 animate-pulse rounded bg-bg-subtle" />
            <div className="h-20 w-full animate-pulse rounded bg-bg-subtle" />
          </div>
        </div>
      )}

      {error && <p className="text-sm text-danger">Couldn&apos;t load this product: {error}</p>}

      {product && !loading && (
        <div className="grid gap-8 lg:grid-cols-2">
          <div className="flex aspect-square items-center justify-center rounded-xl border border-border bg-bg-subtle">
            <span className="select-none text-6xl font-semibold text-ink-muted/50">
              {product.name.charAt(0)}
            </span>
          </div>

          <div className="flex flex-col gap-4">
            <div>
              <Badge tone="accent">{product.category}</Badge>
              <h1 className="mt-3 text-3xl font-semibold tracking-tight text-ink">{product.name}</h1>
            </div>

            <p className="tnum text-2xl font-semibold text-ink">{formatPrice(product.price)}</p>

            <p className="text-ink-muted">{product.description}</p>

            {/* Type-specific attributes render here once products carry `metadata` (see HANDOFF §5, Feature 2). */}

            <div className="mt-2 flex gap-3">
              <Button aria-label={`Add ${product.name} to cart`}>
                <CartIcon className="h-4 w-4" />
                Add to cart
              </Button>
              <Button variant="secondary">Save for later</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

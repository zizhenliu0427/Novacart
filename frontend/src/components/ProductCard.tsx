'use client';

import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { CartIcon } from '@/components/icons';
import { formatPrice, type Product } from '@/types/product';

/**
 * Category-agnostic product card — no vertical-specific ornament, works for any
 * product type. Image is a neutral placeholder until real imagery/S3 lands.
 */
export function ProductCard({ product }: { product: Product }) {
  const onSale =
    typeof product.compareAtPrice === 'number' && product.compareAtPrice > product.price;

  return (
    <Card interactive className="flex flex-col overflow-hidden">
      <div className="relative flex aspect-square items-center justify-center bg-bg-subtle">
        <span className="select-none text-3xl font-semibold text-ink-muted/50">
          {product.name.charAt(0)}
        </span>
        {onSale && (
          <span className="absolute left-3 top-3">
            <Badge tone="sale">Sale</Badge>
          </span>
        )}
      </div>

      <div className="flex flex-1 flex-col gap-2 p-4">
        <span className="text-xs font-medium uppercase tracking-wide text-ink-muted">
          {product.category}
        </span>
        <h3 className="line-clamp-2 font-semibold leading-snug text-ink">{product.name}</h3>
        <p className="line-clamp-2 text-sm text-ink-muted">{product.description}</p>

        <div className="mt-auto flex items-center justify-between pt-2">
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
          <Button size="sm" aria-label={`Add ${product.name} to cart`}>
            <CartIcon className="h-4 w-4" />
            Add
          </Button>
        </div>
      </div>
    </Card>
  );
}

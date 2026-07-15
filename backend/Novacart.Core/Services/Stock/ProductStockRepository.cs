using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services.Stock;

public class ProductStockRepository(AppDbContext db) : IProductStockRepository
{
    public async Task<int?> TryDecrementStockAsync(
        Guid productId,
        int quantity,
        CancellationToken cancellationToken = default)
    {
        if (!db.Database.IsRelational())
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product is null || product.StockQuantity < quantity)
                return null;

            product.StockQuantity -= quantity;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return product.StockQuantity;
        }

        var rows = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE products
            SET stock_quantity = stock_quantity - {quantity},
                updated_at = NOW()
            WHERE id = {productId} AND stock_quantity >= {quantity}
            """, cancellationToken);

        if (rows == 0)
            return null;

        return await db.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => p.StockQuantity)
            .FirstAsync(cancellationToken);
    }

    public async Task<int> GetActiveHoldQuantityAsync(
        Guid productId,
        Guid? excludeOrderId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = db.Set<StockHold>().AsNoTracking()
            .Where(h => h.ProductId == productId &&
                        h.Status == StockHoldStatuses.Active &&
                        h.ExpiresAt > now);

        if (excludeOrderId.HasValue)
            query = query.Where(h => h.OrderId != excludeOrderId.Value);

        var sum = await query.SumAsync(h => (int?)h.Quantity, cancellationToken);
        return sum ?? 0;
    }
}

namespace Novacart.Api.Services.Stock;

public interface IProductStockRepository
{
    /// <summary>
    /// Atomically decrement stock when sufficient quantity remains.
    /// Returns new stock quantity, or null when the row was not updated.
    /// </summary>
    Task<int?> TryDecrementStockAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);

    Task<int> GetActiveHoldQuantityAsync(Guid productId, Guid? excludeOrderId, CancellationToken cancellationToken = default);
}

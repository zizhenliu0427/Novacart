using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services.CartRedis;
using Novacart.Api.Services.Catalog;
using CartEntity = Novacart.Api.Models.Entities.Cart;

namespace Novacart.Api.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId);
    Task ClearCartAsync(Guid userId);

    Task<CartDto> GetCartAsync(string sessionId);
    Task<CartDto> AddItemAsync(string sessionId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(string sessionId, Guid cartItemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(string sessionId, Guid cartItemId);
    Task ClearCartAsync(string sessionId);

    Task MergeGuestCartAsync(string sessionId, Guid userId);
}

public class CartService(
    AppDbContext db,
    IPricingService pricing,
    IProductCatalogStore catalog,
    ICartRedisStore redis) : ICartService
{
    private async Task CacheCartAsync(CartEntity cart, CancellationToken ct = default)
    {
        if (!redis.Enabled)
            return;

        var snapshot = CartRedisStore.ToSnapshot(cart);
        if (cart.UserId.HasValue)
            await redis.SetUserCartAsync(snapshot, ct);
        else if (!string.IsNullOrEmpty(cart.SessionId))
            await redis.SetGuestCartAsync(snapshot, ct);
    }

    private async Task InvalidateCartCacheAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        if (!redis.Enabled)
            return;

        if (userId.HasValue)
            await redis.RemoveUserCartAsync(userId.Value, ct);
        if (!string.IsNullOrEmpty(sessionId))
            await redis.RemoveGuestCartAsync(sessionId, ct);
    }

    private async Task<CartDto?> TryGetCachedCartDtoAsync(Guid? userId, string? sessionId, CancellationToken ct = default)
    {
        if (!redis.Enabled)
            return null;

        CartRedisSnapshot? snapshot = userId.HasValue
            ? await redis.GetUserCartAsync(userId.Value, ct)
            : !string.IsNullOrEmpty(sessionId)
                ? await redis.GetGuestCartAsync(sessionId, ct)
                : null;

        if (snapshot is null)
            return null;

        var cart = CartRedisStore.ToEntity(snapshot);
        if (cart.Items.Count == 0)
            return MapToDto(cart, new Dictionary<Guid, Product>(), Array.Empty<PriceRule>());

        var products = await LoadProductsAsync(cart, ct);
        var rules = await LoadActiveRulesAsync(cart, products);
        return MapToDto(cart, products, rules);
    }

    private async Task<CartEntity> GetOrCreateCartAsync(Guid? userId, string? sessionId)
    {
        if (userId == null && string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Either userId or sessionId must be provided.");

        CartEntity? cart = null;
        if (userId.HasValue)
        {
            cart = await db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);
        }
        else
        {
            cart = await db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        if (cart is not null) return cart;

        var newCart = new CartEntity { UserId = userId, SessionId = sessionId };
        db.Carts.Add(newCart);
        await db.SaveChangesAsync();

        return await db.Carts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == newCart.Id);
    }

    private Task<CartEntity> ReloadCartAsync(Guid cartId) =>
        db.Carts.Include(c => c.Items).AsNoTracking().FirstAsync(c => c.Id == cartId);

    private async Task<Dictionary<Guid, Product>> LoadProductsAsync(CartEntity cart, CancellationToken ct = default)
    {
        var map = new Dictionary<Guid, Product>();
        foreach (var productId in cart.Items.Select(i => i.ProductId).Distinct())
        {
            var product = await catalog.FindProductAsync(productId, ct)
                ?? throw AppException.NotFound("Product");
            map[productId] = product;
        }
        return map;
    }

    private CartDto MapToDto(CartEntity cart, IReadOnlyDictionary<Guid, Product> products, IReadOnlyCollection<PriceRule> rules)
    {
        var lineDtos = cart.Items.Select(ci =>
        {
            var product = products[ci.ProductId];
            var effectivePrice = pricing.ResolveEffectivePrice(product, rules);
            return new CartItemDto
            {
                Id = ci.Id,
                ProductId = ci.ProductId,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                UnitPrice = effectivePrice,
                Currency = product.Currency,
                Quantity = ci.Quantity,
                LineTotal = effectivePrice * ci.Quantity,
                StockQuantity = product.StockQuantity,
            };
        }).ToList();

        return new CartDto
        {
            Id = cart.Id,
            Items = lineDtos,
            Subtotal = lineDtos.Sum(dto => dto.LineTotal),
            TotalItems = cart.Items.Sum(ci => ci.Quantity),
        };
    }

    private async Task<IReadOnlyCollection<PriceRule>> LoadActiveRulesAsync(
        CartEntity cart,
        IReadOnlyDictionary<Guid, Product> products)
    {
        var productIds = cart.Items.Select(ci => ci.ProductId).Distinct().ToList();
        var categoryIds = cart.Items
            .Select(ci => products[ci.ProductId].CategoryId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        return await catalog.LoadActiveRulesForProductsAsync(productIds, categoryIds);
    }

    private async Task<CartDto> CoreAddItemAsync(Guid? userId, string? sessionId, AddCartItemRequest request)
    {
        var product = await catalog.FindProductAsync(request.ProductId)
            ?? throw AppException.NotFound("Product");

        if (!product.IsActive)
            throw new AppException("This product is no longer available.", StatusCodes.Status410Gone);

        if (product.StockQuantity < request.Quantity)
            throw new AppException($"Only {product.StockQuantity} unit(s) available.", StatusCodes.Status422UnprocessableEntity);

        var cart = await GetOrCreateCartAsync(userId, sessionId);

        var existing = cart.Items.FirstOrDefault(ci => ci.ProductId == request.ProductId);
        if (existing is not null)
        {
            var newQty = existing.Quantity + request.Quantity;
            if (newQty > product.StockQuantity)
                throw new AppException($"Cannot add {request.Quantity} more — only {product.StockQuantity} in stock.", StatusCodes.Status422UnprocessableEntity);
            existing.Quantity = newQty;
        }
        else
        {
            db.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                Quantity = request.Quantity,
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var reloaded = await ReloadCartAsync(cart.Id);
        await CacheCartAsync(reloaded);
        var products = await LoadProductsAsync(reloaded);
        var rules = await LoadActiveRulesAsync(reloaded, products);
        return MapToDto(reloaded, products, rules);
    }

    private async Task<CartDto> CoreUpdateItemAsync(Guid? userId, string? sessionId, Guid cartItemId, UpdateCartItemRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId)
            ?? throw AppException.NotFound("Cart item");

        if (request.Quantity == 0)
        {
            cart.Items.Remove(item);
            db.CartItems.Remove(item);
        }
        else
        {
            var product = await catalog.FindProductAsync(item.ProductId)
                ?? throw AppException.NotFound("Product");
            if (request.Quantity > product.StockQuantity)
                throw new AppException($"Only {product.StockQuantity} unit(s) available.", StatusCodes.Status422UnprocessableEntity);
            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await CacheCartAsync(cart);
        var products = await LoadProductsAsync(cart);
        var rules = await LoadActiveRulesAsync(cart, products);
        return MapToDto(cart, products, rules);
    }

    private async Task<CartDto> CoreRemoveItemAsync(Guid? userId, string? sessionId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId)
            ?? throw AppException.NotFound("Cart item");

        cart.Items.Remove(item);
        db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await CacheCartAsync(cart);
        var products = await LoadProductsAsync(cart);
        var rules = await LoadActiveRulesAsync(cart, products);
        return MapToDto(cart, products, rules);
    }

    private async Task CoreClearCartAsync(Guid? userId, string? sessionId)
    {
        CartEntity? cart = null;
        if (userId.HasValue)
            cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId.Value);
        else
            cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (cart is null)
        {
            await InvalidateCartCacheAsync(userId, sessionId);
            return;
        }

        db.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        await InvalidateCartCacheAsync(userId, sessionId);
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cached = await TryGetCachedCartDtoAsync(userId, null);
        if (cached is not null)
            return cached;

        var cart = await GetOrCreateCartAsync(userId, null);
        await CacheCartAsync(cart);
        var products = await LoadProductsAsync(cart);
        var rules = await LoadActiveRulesAsync(cart, products);
        return MapToDto(cart, products, rules);
    }

    public Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request) =>
        CoreAddItemAsync(userId, null, request);

    public Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request) =>
        CoreUpdateItemAsync(userId, null, cartItemId, request);

    public Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId) =>
        CoreRemoveItemAsync(userId, null, cartItemId);

    public Task ClearCartAsync(Guid userId) => CoreClearCartAsync(userId, null);

    public async Task<CartDto> GetCartAsync(string sessionId)
    {
        var cached = await TryGetCachedCartDtoAsync(null, sessionId);
        if (cached is not null)
            return cached;

        var cart = await GetOrCreateCartAsync(null, sessionId);
        await CacheCartAsync(cart);
        var products = await LoadProductsAsync(cart);
        var rules = await LoadActiveRulesAsync(cart, products);
        return MapToDto(cart, products, rules);
    }

    public Task<CartDto> AddItemAsync(string sessionId, AddCartItemRequest request) =>
        CoreAddItemAsync(null, sessionId, request);

    public Task<CartDto> UpdateItemAsync(string sessionId, Guid cartItemId, UpdateCartItemRequest request) =>
        CoreUpdateItemAsync(null, sessionId, cartItemId, request);

    public Task<CartDto> RemoveItemAsync(string sessionId, Guid cartItemId) =>
        CoreRemoveItemAsync(null, sessionId, cartItemId);

    public Task ClearCartAsync(string sessionId) => CoreClearCartAsync(null, sessionId);

    public async Task MergeGuestCartAsync(string sessionId, Guid userId)
    {
        if (string.IsNullOrEmpty(sessionId)) return;

        db.ChangeTracker.Clear();

        var guestCart = await db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (guestCart is null || !guestCart.Items.Any()) return;

        var userCart = await GetOrCreateCartAsync(userId, null);
        var products = await LoadProductsAsync(guestCart);

        foreach (var guestItem in guestCart.Items)
        {
            var product = products[guestItem.ProductId];
            var existing = userCart.Items.FirstOrDefault(ci => ci.ProductId == guestItem.ProductId);
            if (existing is not null)
            {
                var totalQty = existing.Quantity + guestItem.Quantity;
                existing.Quantity = Math.Min(totalQty, product.StockQuantity);
            }
            else
            {
                db.CartItems.Add(new CartItem
                {
                    CartId = userCart.Id,
                    ProductId = guestItem.ProductId,
                    Quantity = Math.Min(guestItem.Quantity, product.StockQuantity),
                });
            }
        }

        db.Carts.Remove(guestCart);
        userCart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        await redis.RemoveGuestCartAsync(sessionId);
        await CacheCartAsync(userCart);
    }
}

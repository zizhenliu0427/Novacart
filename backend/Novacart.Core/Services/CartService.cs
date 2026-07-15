using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services.Catalog;

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
    IProductCatalogStore catalog) : ICartService
{
    private async Task<Cart> GetOrCreateCartAsync(Guid? userId, string? sessionId)
    {
        if (userId == null && string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Either userId or sessionId must be provided.");

        Cart? cart = null;
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

        var newCart = new Cart { UserId = userId, SessionId = sessionId };
        db.Carts.Add(newCart);
        await db.SaveChangesAsync();

        return await db.Carts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == newCart.Id);
    }

    private Task<Cart> ReloadCartAsync(Guid cartId) =>
        db.Carts.Include(c => c.Items).AsNoTracking().FirstAsync(c => c.Id == cartId);

    private async Task<Dictionary<Guid, Product>> LoadProductsAsync(Cart cart, CancellationToken ct = default)
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

    private CartDto MapToDto(Cart cart, IReadOnlyDictionary<Guid, Product> products, IReadOnlyCollection<PriceRule> rules)
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
        Cart cart,
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

        var products = await LoadProductsAsync(cart);
        var rules = await LoadActiveRulesAsync(cart, products);
        return MapToDto(cart, products, rules);
    }

    private async Task CoreClearCartAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = null;
        if (userId.HasValue)
            cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId.Value);
        else
            cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (cart is null) return;

        db.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId, null);
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
        var cart = await GetOrCreateCartAsync(null, sessionId);
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
    }
}

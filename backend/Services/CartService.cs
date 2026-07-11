using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface ICartService
{
    // Authenticated path
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId);
    Task ClearCartAsync(Guid userId);

    // Guest path
    Task<CartDto> GetCartAsync(string sessionId);
    Task<CartDto> AddItemAsync(string sessionId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(string sessionId, Guid cartItemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(string sessionId, Guid cartItemId);
    Task ClearCartAsync(string sessionId);

    // Merge logic
    Task MergeGuestCartAsync(string sessionId, Guid userId);
}

public class CartService : ICartService
{
    private readonly AppDbContext _db;
    private readonly IPricingService _pricing;

    public CartService(AppDbContext db, IPricingService pricing)
    {
        _db = db;
        _pricing = pricing;
    }

    // ── Internal helpers ──────────────────────────────────────

    private async Task<Cart> GetOrCreateCartAsync(Guid? userId, string? sessionId)
    {
        if (userId == null && string.IsNullOrEmpty(sessionId))
            throw new ArgumentException("Either userId or sessionId must be provided.");

        Cart? cart = null;
        if (userId.HasValue)
        {
            cart = await _db.Carts
                .Include(c => c.Items).ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);
        }
        else
        {
            cart = await _db.Carts
                .Include(c => c.Items).ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        if (cart is not null) return cart;

        // No cart exists — create and persist, then re-fetch
        var newCart = new Cart
        {
            UserId = userId,
            SessionId = sessionId
        };
        _db.Carts.Add(newCart);
        await _db.SaveChangesAsync();

        return await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .FirstAsync(c => c.Id == newCart.Id);
    }

    private async Task<Cart> ReloadCartAsync(Guid cartId)
    {
        return await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .AsNoTracking()
            .FirstAsync(c => c.Id == cartId);
    }

    private CartDto MapToDto(Cart cart, IReadOnlyCollection<PriceRule> rules)
    {
        var lineDtos = cart.Items.Select(ci =>
        {
            var effectivePrice = _pricing.ResolveEffectivePrice(ci.Product, rules);
            return new CartItemDto
            {
                Id            = ci.Id,
                ProductId     = ci.ProductId,
                ProductName   = ci.Product.Name,
                ProductSlug   = ci.Product.Slug,
                UnitPrice     = effectivePrice,
                Currency      = ci.Product.Currency,
                Quantity      = ci.Quantity,
                LineTotal     = effectivePrice * ci.Quantity,
                StockQuantity = ci.Product.StockQuantity,
            };
        }).ToList();

        return new CartDto
        {
            Id         = cart.Id,
            Items      = lineDtos,
            Subtotal   = lineDtos.Sum(dto => dto.LineTotal),
            TotalItems = cart.Items.Sum(ci => ci.Quantity),
        };
    }

    private async Task<IReadOnlyCollection<PriceRule>> LoadActiveRulesAsync(Cart cart)
    {
        var productIds = cart.Items.Select(ci => ci.ProductId).Distinct().ToList();
        var categoryIds = cart.Items
            .Where(ci => ci.Product.CategoryId.HasValue)
            .Select(ci => ci.Product.CategoryId!.Value)
            .Distinct()
            .ToList();

        if (productIds.Count == 0) return Array.Empty<PriceRule>();

        return await _db.PriceRules
            .Where(r => r.IsActive &&
                        ((r.StartsAt == null || r.StartsAt <= DateTime.UtcNow) &&
                         (r.EndsAt == null || r.EndsAt >= DateTime.UtcNow)) &&
                        ((r.ProductId != null && productIds.Contains(r.ProductId.Value)) ||
                         (r.CategoryId != null && categoryIds.Contains(r.CategoryId.Value)) ||
                         (r.ProductId == null && r.CategoryId == null)))
            .ToListAsync();
    }

    // ── Unified internal core CRUD ─────────────────────────────

    private async Task<CartDto> CoreAddItemAsync(Guid? userId, string? sessionId, AddCartItemRequest request)
    {
        var product = await _db.Products.FindAsync(request.ProductId)
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
            _db.CartItems.Add(new CartItem
            {
                CartId    = cart.Id,
                ProductId = product.Id,
                Quantity  = request.Quantity,
            });
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var reloaded = await ReloadCartAsync(cart.Id);
        var rules = await LoadActiveRulesAsync(reloaded);
        return MapToDto(reloaded, rules);
    }

    private async Task<CartDto> CoreUpdateItemAsync(Guid? userId, string? sessionId, Guid cartItemId, UpdateCartItemRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId)
            ?? throw AppException.NotFound("Cart item");

        if (request.Quantity == 0)
        {
            cart.Items.Remove(item);
            _db.CartItems.Remove(item);
        }
        else
        {
            if (request.Quantity > item.Product.StockQuantity)
                throw new AppException($"Only {item.Product.StockQuantity} unit(s) available.", StatusCodes.Status422UnprocessableEntity);
            item.Quantity = request.Quantity;
        }

        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var rules = await LoadActiveRulesAsync(cart);
        return MapToDto(cart, rules);
    }

    private async Task<CartDto> CoreRemoveItemAsync(Guid? userId, string? sessionId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId, sessionId);

        var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId)
            ?? throw AppException.NotFound("Cart item");

        cart.Items.Remove(item);
        _db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var rules = await LoadActiveRulesAsync(cart);
        return MapToDto(cart, rules);
    }

    private async Task CoreClearCartAsync(Guid? userId, string? sessionId)
    {
        Cart? cart = null;
        if (userId.HasValue)
        {
            cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);
        }
        else
        {
            cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        if (cart is null) return;

        _db.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ── Public Auth Methods ─────────────────────────────────────

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId, null);
        var rules = await LoadActiveRulesAsync(cart);
        return MapToDto(cart, rules);
    }

    public Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request) =>
        CoreAddItemAsync(userId, null, request);

    public Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request) =>
        CoreUpdateItemAsync(userId, null, cartItemId, request);

    public Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId) =>
        CoreRemoveItemAsync(userId, null, cartItemId);

    public Task ClearCartAsync(Guid userId) =>
        CoreClearCartAsync(userId, null);

    // ── Public Guest Methods ─────────────────────────────────────

    public async Task<CartDto> GetCartAsync(string sessionId)
    {
        var cart = await GetOrCreateCartAsync(null, sessionId);
        var rules = await LoadActiveRulesAsync(cart);
        return MapToDto(cart, rules);
    }

    public Task<CartDto> AddItemAsync(string sessionId, AddCartItemRequest request) =>
        CoreAddItemAsync(null, sessionId, request);

    public Task<CartDto> UpdateItemAsync(string sessionId, Guid cartItemId, UpdateCartItemRequest request) =>
        CoreUpdateItemAsync(null, sessionId, cartItemId, request);

    public Task<CartDto> RemoveItemAsync(string sessionId, Guid cartItemId) =>
        CoreRemoveItemAsync(null, sessionId, cartItemId);

    public Task ClearCartAsync(string sessionId) =>
        CoreClearCartAsync(null, sessionId);

    // ── Merge guest cart ──────────────────────────────────────────

    public async Task MergeGuestCartAsync(string sessionId, Guid userId)
    {
        if (string.IsNullOrEmpty(sessionId)) return;

        _db.ChangeTracker.Clear();

        var guestCart = await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.SessionId == sessionId);

        if (guestCart is null || !guestCart.Items.Any()) return;

        var userCart = await GetOrCreateCartAsync(userId, null);

        foreach (var guestItem in guestCart.Items)
        {
            var existing = userCart.Items.FirstOrDefault(ci => ci.ProductId == guestItem.ProductId);
            if (existing is not null)
            {
                // Merge quantity and clamp to stock
                var totalQty = existing.Quantity + guestItem.Quantity;
                existing.Quantity = Math.Min(totalQty, guestItem.Product.StockQuantity);
            }
            else
            {
                // Transfer item to user's cart
                var newItem = new CartItem
                {
                    CartId = userCart.Id,
                    ProductId = guestItem.ProductId,
                    Quantity = Math.Min(guestItem.Quantity, guestItem.Product.StockQuantity)
                };
                _db.CartItems.Add(newItem);
            }
        }

        // Delete guest cart (cascade deletes items)
        _db.Carts.Remove(guestCart);

        userCart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

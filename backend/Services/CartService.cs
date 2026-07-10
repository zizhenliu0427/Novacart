using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request);
    Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request);
    Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId);
    Task ClearCartAsync(Guid userId);
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

    /// <summary>
    /// Load the user's cart with items + products, or create one if it doesn't exist.
    /// </summary>
    private async Task<Cart> GetOrCreateCartAsync(Guid userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is not null) return cart;

        // No cart exists — create and persist, then re-fetch with includes.
        var newCart = new Cart { UserId = userId };
        _db.Carts.Add(newCart);
        await _db.SaveChangesAsync();

        // Re-fetch so the entity is clean and has the correct tracking state.
        return (await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .FirstAsync(c => c.Id == newCart.Id));
    }

    /// <summary>
    /// Re-fetch the cart with full includes so MapToDto can read Product navigations.
    /// </summary>
    private async Task<Cart> ReloadCartAsync(Guid cartId)
    {
        return await _db.Carts
            .Include(c => c.Items).ThenInclude(ci => ci.Product)
            .AsNoTracking()
            .FirstAsync(c => c.Id == cartId);
    }

    /// <summary>
    /// Load active pricing rules applicable to the cart's products, then map to DTO.
    /// Called after items are materialised (Product navigation loaded).
    /// </summary>
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

    /// <summary>Map using base prices only (no rules) — legacy/static fallback.</summary>
    private CartDto MapToDtoBase(Cart cart) =>
        MapToDto(cart, Array.Empty<PriceRule>());

    /// <summary>Load the pricing rules applicable to a cart's product set.</summary>
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

    // ── Public methods ────────────────────────────────────────

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        var rules = await LoadActiveRulesAsync(cart);
        return MapToDto(cart, rules);
    }

    public async Task<CartDto> AddItemAsync(Guid userId, AddCartItemRequest request)
    {
        var product = await _db.Products.FindAsync(request.ProductId)
            ?? throw AppException.NotFound("Product");

        if (!product.IsActive)
            throw new AppException("This product is no longer available.", StatusCodes.Status410Gone);

        if (product.StockQuantity < request.Quantity)
            throw new AppException($"Only {product.StockQuantity} unit(s) available.", StatusCodes.Status422UnprocessableEntity);

        var cart = await GetOrCreateCartAsync(userId);

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

        // Re-fetch cart with products loaded for DTO mapping.
        var reloaded = await ReloadCartAsync(cart.Id);
        var rules = await LoadActiveRulesAsync(reloaded);
        return MapToDto(reloaded, rules);
    }

    public async Task<CartDto> UpdateItemAsync(Guid userId, Guid cartItemId, UpdateCartItemRequest request)
    {
        var cart = await GetOrCreateCartAsync(userId);

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

    public async Task<CartDto> RemoveItemAsync(Guid userId, Guid cartItemId)
    {
        var cart = await GetOrCreateCartAsync(userId);

        var item = cart.Items.FirstOrDefault(ci => ci.Id == cartItemId)
            ?? throw AppException.NotFound("Cart item");

        cart.Items.Remove(item);
        _db.CartItems.Remove(item);
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        var rules = await LoadActiveRulesAsync(cart);
        return MapToDto(cart, rules);
    }

    public async Task ClearCartAsync(Guid userId)
    {
        var cart = await _db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null) return;

        _db.CartItems.RemoveRange(cart.Items);
        cart.Items.Clear();
        cart.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

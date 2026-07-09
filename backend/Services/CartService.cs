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

    public CartService(AppDbContext db) => _db = db;

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
    /// Map cart to DTO. Must be called after items are materialised (Product navigation loaded).
    /// Uses ProductService.ResolvePrice (static) so it doesn't capture an instance in LINQ.
    /// </summary>
    private static CartDto MapToDto(Cart cart) =>
        new()
        {
            Id = cart.Id,
            Items = cart.Items.Select(ci => new CartItemDto
            {
                Id            = ci.Id,
                ProductId     = ci.ProductId,
                ProductName   = ci.Product.Name,
                ProductSlug   = ci.Product.Slug,
                UnitPrice     = ProductService.ResolvePrice(ci.Product),
                Currency      = ci.Product.Currency,
                Quantity      = ci.Quantity,
                LineTotal     = ProductService.ResolvePrice(ci.Product) * ci.Quantity,
                StockQuantity = ci.Product.StockQuantity,
            }).ToList(),
            Subtotal   = cart.Items.Sum(ci => ProductService.ResolvePrice(ci.Product) * ci.Quantity),
            TotalItems = cart.Items.Sum(ci => ci.Quantity),
        };

    // ── Public methods ────────────────────────────────────────

    public async Task<CartDto> GetCartAsync(Guid userId)
    {
        var cart = await GetOrCreateCartAsync(userId);
        return MapToDto(cart);
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
        return MapToDto(reloaded);
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
        return MapToDto(cart);
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
        return MapToDto(cart);
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

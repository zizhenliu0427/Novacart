using System.ComponentModel.DataAnnotations;

namespace Novacart.Api.Models.Dtos.Cart;

public class CartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSlug { get; set; }
    public decimal UnitPrice { get; set; }
    public string Currency { get; set; } = "AUD";
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
    public int StockQuantity { get; set; }
}

public class CartDto
{
    public Guid Id { get; set; }
    public IReadOnlyList<CartItemDto> Items { get; set; } = Array.Empty<CartItemDto>();
    public decimal Subtotal { get; set; }
    public int TotalItems { get; set; }
}

public class AddCartItemRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(0, 100)]
    public int Quantity { get; set; }
}

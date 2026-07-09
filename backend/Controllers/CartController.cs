using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>Get the current user's cart (creates one if it doesn't exist).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
        => Ok(await _cart.GetCartAsync(GetUserId()));

    /// <summary>Add a product to the cart (merges if already present).</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
        => Ok(await _cart.AddItemAsync(GetUserId(), request));

    /// <summary>Update the quantity of a cart item (set to 0 to remove).</summary>
    [HttpPut("items/{id:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request)
        => Ok(await _cart.UpdateItemAsync(GetUserId(), id, request));

    /// <summary>Remove a specific cart item.</summary>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(Guid id)
        => Ok(await _cart.RemoveItemAsync(GetUserId(), id));
}

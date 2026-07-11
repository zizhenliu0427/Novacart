using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Cart;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    private Guid? TryGetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private string GetOrCreateSessionId()
    {
        // Try header first
        if (Request.Headers.TryGetValue("X-Cart-Session", out var headerVal) && !string.IsNullOrEmpty(headerVal))
            return headerVal.ToString();

        // Try cookie
        if (Request.Cookies.TryGetValue("novacart_cart_session", out var cookieVal) && !string.IsNullOrEmpty(cookieVal))
            return cookieVal;

        // Generate a new session ID
        var newSession = Guid.NewGuid().ToString("N");
        
        // Write to cookie (30 days TTL)
        Response.Cookies.Append("novacart_cart_session", newSession, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Allow HTTP in development compose env
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

        return newSession;
    }

    /// <summary>Get the current cart (authenticated or guest).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        var userId = TryGetUserId();
        if (userId.HasValue)
        {
            return Ok(await _cart.GetCartAsync(userId.Value));
        }
        return Ok(await _cart.GetCartAsync(GetOrCreateSessionId()));
    }

    /// <summary>Add a product to the cart (authenticated or guest).</summary>
    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request)
    {
        var userId = TryGetUserId();
        if (userId.HasValue)
        {
            return Ok(await _cart.AddItemAsync(userId.Value, request));
        }
        return Ok(await _cart.AddItemAsync(GetOrCreateSessionId(), request));
    }

    /// <summary>Update the quantity of a cart item.</summary>
    [HttpPut("items/{id:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request)
    {
        var userId = TryGetUserId();
        if (userId.HasValue)
        {
            return Ok(await _cart.UpdateItemAsync(userId.Value, id, request));
        }
        return Ok(await _cart.UpdateItemAsync(GetOrCreateSessionId(), id, request));
    }

    /// <summary>Remove a specific cart item.</summary>
    [HttpDelete("items/{id:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveItem(Guid id)
    {
        var userId = TryGetUserId();
        if (userId.HasValue)
        {
            return Ok(await _cart.RemoveItemAsync(userId.Value, id));
        }
        return Ok(await _cart.RemoveItemAsync(GetOrCreateSessionId(), id));
    }

    /// <summary>Merge guest cart into authenticated user cart.</summary>
    [HttpPost("merge")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MergeCart()
    {
        var userId = TryGetUserId();
        if (!userId.HasValue) return Unauthorized();

        var sessionId = Request.Cookies["novacart_cart_session"] 
            ?? Request.Headers["X-Cart-Session"].ToString();

        if (!string.IsNullOrEmpty(sessionId))
        {
            await _cart.MergeGuestCartAsync(sessionId, userId.Value);
            Response.Cookies.Delete("novacart_cart_session");
        }
        return NoContent();
    }
}

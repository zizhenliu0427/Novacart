using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

/// <summary>P2-3 (Wishlist) — SCAFFOLD (actions return 501 via the stub service).</summary>
[ApiController]
[Route("api/wishlist")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlist;
    public WishlistController(IWishlistService wishlist) => _wishlist = wishlist;

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>List the current user's wishlist.</summary>
    [HttpGet]
    public async Task<IActionResult> Get() => Ok(await _wishlist.GetAsync(GetUserId()));

    /// <summary>Add a product to the wishlist.</summary>
    [HttpPost("items")]
    public async Task<IActionResult> Add([FromBody] AddWishlistItemRequest request)
    {
        await _wishlist.AddAsync(GetUserId(), request.ProductId);
        return NoContent();
    }

    /// <summary>Remove a product from the wishlist.</summary>
    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> Remove(Guid productId)
    {
        await _wishlist.RemoveAsync(GetUserId(), productId);
        return NoContent();
    }
}

public record AddWishlistItemRequest(Guid ProductId);

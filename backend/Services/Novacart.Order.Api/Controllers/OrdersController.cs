using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Dtos.Products; // for PagedResult
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>Get user order history (paginated).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);
        return Ok(await _orders.GetOrdersAsync(GetUserId(), page, pageSize));
    }

    /// <summary>Get detailed line items for a specific order.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id)
        => Ok(await _orders.GetOrderByIdAsync(GetUserId(), id));
}

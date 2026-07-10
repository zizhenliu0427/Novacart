using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Orders;
using Novacart.Api.Models.Dtos.Products;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// P2-7 / P2-8: admin order management + status workflow.
/// RBAC-guarded (admin/sysadmin). Status transitions are validated by
/// <see cref="AdminOrderService"/>.
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminOrdersController : ControllerBase
{
    private readonly IAdminOrderService _orders;

    public AdminOrdersController(IAdminOrderService orders) => _orders = orders;

    private Guid? GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    /// <summary>List all orders (paginated, filterable by status + free-text search).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminOrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return Ok(await _orders.GetAllAsync(q, status, page, pageSize));
    }

    /// <summary>Get a single order with line items.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id) => Ok(await _orders.GetByIdAsync(id));

    /// <summary>Advance an order's status (validates allowed transitions).</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AdminOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var updated = await _orders.UpdateStatusAsync(id, request, GetUserId());
        return Ok(updated);
    }
}

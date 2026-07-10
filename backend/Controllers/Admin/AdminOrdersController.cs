using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// P2-7 / P2-8 (Admin order management + status workflow) — SCAFFOLD. RBAC-guarded.
/// See HANDOFF §7 P2-7 (status transitions) and P2-8.
/// </summary>
[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminOrdersController : ControllerBase
{
    /// <summary>List all orders (admin view, paginated + filterable by status).</summary>
    [HttpGet]
    public IActionResult List() => throw AppException.NotImplemented("P2-8: admin order list not implemented yet.");

    /// <summary>Advance an order's status (validates allowed transitions — see OrderStatuses).</summary>
    [HttpPatch("{id:guid}/status")]
    public IActionResult UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        => throw AppException.NotImplemented("P2-7: order status transition not implemented yet.");
}

public record UpdateOrderStatusRequest(string ToStatus, string? Notes);

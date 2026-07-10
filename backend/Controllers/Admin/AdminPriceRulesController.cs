using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// P2-5 (Dynamic pricing admin) — SCAFFOLD. CRUD for <see cref="PriceRule"/> rows that
/// feed <see cref="IPricingService"/>. RBAC-guarded. See HANDOFF §7 P2-5.
/// </summary>
[ApiController]
[Route("api/admin/price-rules")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminPriceRulesController : ControllerBase
{
    [HttpGet]
    public IActionResult List() => throw AppException.NotImplemented("P2-5: list price rules not implemented yet.");

    [HttpPost]
    public IActionResult Create() => throw AppException.NotImplemented("P2-5: create price rule not implemented yet.");

    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) => throw AppException.NotImplemented("P2-5: delete price rule not implemented yet.");
}

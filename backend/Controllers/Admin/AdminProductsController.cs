using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// P2-8 (Admin product/inventory CRUD) — SCAFFOLD. RBAC is REAL: only admin/sysadmin
/// reach these (customers get 403); the bodies return 501 until implemented. See HANDOFF §7 P2-8.
/// </summary>
[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminProductsController : ControllerBase
{
    /// <summary>Create a product.</summary>
    [HttpPost]
    public IActionResult Create() => throw AppException.NotImplemented("P2-8: create product not implemented yet.");

    /// <summary>Update a product (price / stock / description / active).</summary>
    [HttpPut("{id:guid}")]
    public IActionResult Update(Guid id) => throw AppException.NotImplemented("P2-8: update product not implemented yet.");

    /// <summary>Deactivate / delete a product.</summary>
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id) => throw AppException.NotImplemented("P2-8: delete product not implemented yet.");
}

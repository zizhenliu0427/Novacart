using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Pricing;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// P2-5 (Dynamic pricing admin). CRUD for <see cref="PriceRule"/> rows that feed
/// <see cref="IPricingService"/>. RBAC-guarded.
/// </summary>
[ApiController]
[Route("api/admin/price-rules")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminPriceRulesController : ControllerBase
{
    private readonly IPriceRuleService _priceRules;

    public AdminPriceRulesController(IPriceRuleService priceRules) => _priceRules = priceRules;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PriceRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List() => Ok(await _priceRules.GetAllAsync());

    [HttpPost]
    [ProducesResponseType(typeof(PriceRuleDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreatePriceRuleRequest request)
    {
        var created = await _priceRules.CreateAsync(request);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _priceRules.DeleteAsync(id);
        return NoContent();
    }
}

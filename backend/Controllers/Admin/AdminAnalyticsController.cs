using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// P2-9 (Analytics dashboard) — SCAFFOLD. RBAC-guarded; delegates to the stub
/// <see cref="IAnalyticsService"/> (returns 501). See HANDOFF §7 P2-9.
/// </summary>
[ApiController]
[Route("api/admin/analytics")]
[Authorize(Roles = RoleNames.AdminRoles)]
public class AdminAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;
    public AdminAnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    /// <summary>Total revenue, order count, units sold, average order value.</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary() => Ok(await _analytics.GetSummaryAsync());

    /// <summary>Revenue + order count per day over the last N days.</summary>
    [HttpGet("sales-over-time")]
    public async Task<IActionResult> SalesOverTime([FromQuery] int days = 30)
        => Ok(await _analytics.GetSalesOverTimeAsync(days));

    /// <summary>Best-selling products by units sold.</summary>
    [HttpGet("best-sellers")]
    public async Task<IActionResult> BestSellers([FromQuery] int top = 10)
        => Ok(await _analytics.GetBestSellersAsync(top));
}

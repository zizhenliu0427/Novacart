using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers.Admin;

/// <summary>
/// Dedicated system administrator controller for cache clearing and detailed system diagnostics.
/// Restricted specifically to the "sysadmin" role claim (P14 role-differentiation).
/// </summary>
[ApiController]
[Route("api/admin/system")]
[Authorize(Roles = RoleNames.SysAdmin)]
public class AdminSystemController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<AdminSystemController> _logger;

    public AdminSystemController(
        AppDbContext db,
        IRedisCacheService cache,
        ILogger<AdminSystemController> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Checks database and cache connection status and returns details.
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        bool dbOk;
        string? dbError = null;
        try
        {
            dbOk = await _db.Database.CanConnectAsync();
        }
        catch (Exception ex)
        {
            dbOk = false;
            dbError = ex.Message;
        }

        bool redisOk;
        string? redisError = null;
        try
        {
            // Mini check
            await _cache.SetAsync("__sys_health__", "ok", TimeSpan.FromSeconds(5));
            var val = await _cache.GetAsync<string>("__sys_health__");
            redisOk = val == "ok";
        }
        catch (Exception ex)
        {
            redisOk = false;
            redisError = ex.Message;
        }

        return Ok(new
        {
            Timestamp = DateTime.UtcNow,
            Database = new { Connected = dbOk, Error = dbError },
            RedisCache = new { Connected = redisOk, Error = redisError }
        });
    }

    /// <summary>
    /// Flushes all entries currently stored in the Redis cache prefix lists.
    /// </summary>
    [HttpPost("clear-cache")]
    public async Task<IActionResult> ClearCache()
    {
        _logger.LogWarning("System administrator initiated Redis cache flush.");
        
        // Remove all prefixes in the application cache
        await _cache.RemoveByPrefixAsync("products:list:");
        await _cache.RemoveByPrefixAsync("orders:");
        await _cache.RemoveByPrefixAsync("analytics:");
        
        // Clean the temporary health keys
        await _cache.RemoveAsync("__sys_health__");
        await _cache.RemoveAsync("__health__");

        return Ok(new { success = true, message = "Redis cache flushed successfully." });
    }
}

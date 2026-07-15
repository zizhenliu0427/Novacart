using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

/// <summary>P2-2 (Customer profile management) — SCAFFOLD (actions return 501 via the stub service).</summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    public UsersController(IUserService users) => _users = users;

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    /// <summary>Get the current user's profile.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe() => Ok(await _users.GetProfileAsync(GetUserId()));

    /// <summary>Update the current user's profile.</summary>
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
        => Ok(await _users.UpdateProfileAsync(GetUserId(), request));
}

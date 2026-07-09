using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Auth;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>Register a new user and return a JWT.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        => Ok(await _auth.RegisterAsync(request));

    /// <summary>Login with email + password and return a JWT.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
        => Ok(await _auth.LoginAsync(request));

    /// <summary>Return the currently authenticated user (proves the JWT works).</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Ok(new UserDto
        {
            Id = Guid.TryParse(id, out var g) ? g : Guid.Empty,
            Email = User.FindFirstValue(JwtRegisteredClaimNames.Email)
                    ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            FullName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray(),
        });
    }

    /// <summary>
    /// Logout — with Bearer tokens the client simply discards the token.
    /// This endpoint exists for symmetry and future HttpOnly-cookie migration.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout() => Ok(new { message = "Logged out successfully." });
}

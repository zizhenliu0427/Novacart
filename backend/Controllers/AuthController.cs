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
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, IConfiguration config)
    {
        _auth = auth;
        _config = config;
    }

    /// <summary>Register a new user and return a JWT (also set as HttpOnly cookie).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _auth.RegisterAsync(request);
        SetJwtCookie(response.Token);
        return Ok(response);
    }

    /// <summary>Login with email + password and return a JWT (also set as HttpOnly cookie).</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _auth.LoginAsync(request);
        SetJwtCookie(response.Token);
        return Ok(response);
    }

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
    /// Logout — clears the HttpOnly JWT cookie.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        ClearJwtCookie();
        return Ok(new { message = "Logged out successfully." });
    }

    // ── Cookie helpers ──────────────────────────────────────

    private void SetJwtCookie(string token)
    {
        var expiryHours = int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h : 24;

        Response.Cookies.Append("novacart_jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices
                        .GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            MaxAge = TimeSpan.FromHours(expiryHours),
        });
    }

    private void ClearJwtCookie()
    {
        Response.Cookies.Delete("novacart_jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices
                        .GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Path = "/api",
        });
    }
}

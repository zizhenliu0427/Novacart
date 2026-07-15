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
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, IRefreshTokenService refreshTokens, IConfiguration config)
    {
        _auth = auth;
        _refreshTokens = refreshTokens;
        _config = config;
    }

    /// <summary>Register a new user and return a JWT + refresh token (both set as HttpOnly cookies).</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var response = await _auth.RegisterAsync(request);
        SetAuthCookies(response);
        return Ok(response);
    }

    /// <summary>Login with email + password and return a JWT + refresh token (both set as HttpOnly cookies).</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _auth.LoginAsync(request);
        SetAuthCookies(response);
        return Ok(response);
    }

    /// <summary>
    /// Exchange a valid refresh token (read from the <c>novacart_refresh</c> cookie) for a new
    /// access + refresh token pair. Rotation invalidates the old refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["novacart_refresh"];
        if (string.IsNullOrEmpty(refreshToken))
            throw AppException.Unauthorized("No refresh token provided.");

        var response = await _auth.RefreshAsync(refreshToken);
        SetAuthCookies(response);
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
    /// Logout — revokes all refresh tokens for the user and clears both HttpOnly cookies.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(id, out var userId))
            await _refreshTokens.RevokeAllAsync(userId);

        ClearAuthCookies();
        return Ok(new { message = "Logged out successfully." });
    }

    // ── Cookie helpers ──────────────────────────────────────

    private void SetAuthCookies(AuthResponse response)
    {
        // Short-lived access token — sent with every /api request.
        var accessMinutes = int.TryParse(_config["Jwt:AccessTokenMinutes"], out var m)
            ? m
            : (int.TryParse(_config["Jwt:ExpiryHours"], out var h) ? h * 60 : 24 * 60);

        Response.Cookies.Append("novacart_jwt", response.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsDevelopment,
            SameSite = SameSiteMode.Strict,
            Path = "/api",
            MaxAge = TimeSpan.FromMinutes(accessMinutes),
        });

        // Long-lived refresh token — scoped to /api/auth so it's not sent on other requests.
        if (!string.IsNullOrEmpty(response.RefreshToken))
        {
            var refreshDays = int.TryParse(_config["Jwt:RefreshTokenDays"], out var d) ? d : 7;
            Response.Cookies.Append("novacart_refresh", response.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsDevelopment,
                SameSite = SameSiteMode.Strict,
                Path = "/api/auth",
                MaxAge = TimeSpan.FromDays(refreshDays),
            });
        }
    }

    private void ClearAuthCookies()
    {
        Response.Cookies.Delete("novacart_jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsDevelopment,
            SameSite = SameSiteMode.Strict,
            Path = "/api",
        });
        Response.Cookies.Delete("novacart_refresh", new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsDevelopment,
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
        });
    }

    private bool IsDevelopment => HttpContext.RequestServices
        .GetRequiredService<IWebHostEnvironment>().IsDevelopment();
}

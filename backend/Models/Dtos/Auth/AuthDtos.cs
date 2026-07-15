using System.ComponentModel.DataAnnotations;

namespace Novacart.Api.Models.Dtos.Auth;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8), MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    /// <summary>The refresh token (opaque). The client stores it in an HttpOnly cookie; this field is informational.</summary>
    public string? RefreshToken { get; set; }
    public DateTime? RefreshExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}

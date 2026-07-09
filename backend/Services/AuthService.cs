using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Auth;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

/// <summary>Thrown for expected auth failures (mapped to 4xx by the controller).</summary>
public class AuthException : Exception
{
    public int StatusCode { get; }
    public AuthException(string message, int statusCode) : base(message) => StatusCode = statusCode;
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IJwtTokenService _jwt;

    public AuthService(AppDbContext db, IJwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email))
            throw new AuthException("An account with this email already exists.", StatusCodes.Status409Conflict);

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        // Every new signup is a customer by default.
        user.UserRoles.Add(new UserRole { RoleId = RoleNames.CustomerId });

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return BuildResponse(user, new[] { RoleNames.Customer });
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Same generic message whether the email or password is wrong (avoid user enumeration).
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new AuthException("Invalid email or password.", StatusCodes.Status401Unauthorized);

        if (!user.IsActive)
            throw new AuthException("This account has been deactivated.", StatusCodes.Status403Forbidden);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return BuildResponse(user, roles);
    }

    private AuthResponse BuildResponse(User user, IReadOnlyCollection<string> roles)
    {
        var (token, expiresAt) = _jwt.CreateToken(user, roles);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles,
            },
        };
    }
}

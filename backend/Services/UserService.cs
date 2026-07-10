using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

// ── DTOs (P2-2) ────────────────────────────────────────────
public record UserProfileDto(Guid Id, string Email, string FullName, IEnumerable<string> Roles);
public record UpdateProfileRequest(string FullName);

/// <summary>P2-2 (Customer profile management). See HANDOFF §7 P2-2.</summary>
public interface IUserService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
}

/// <summary>
/// P2-2: customer profile read/update. Email changes require a verification flow
/// (deferred), so only <c>FullName</c> is editable for now.
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public async Task<UserProfileDto> GetProfileAsync(Guid userId)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw AppException.NotFound("User");

        return Map(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var fullName = request.FullName?.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            throw new AppException("Full name is required.");

        var user = await _db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw AppException.NotFound("User");

        user.FullName = fullName;
        await _db.SaveChangesAsync();

        return Map(user);
    }

    private static UserProfileDto Map(User user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.UserRoles.Select(ur => ur.Role.Name).ToList());
}

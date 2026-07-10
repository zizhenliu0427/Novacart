using Novacart.Api.Data;

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
/// SCAFFOLD STUB — throws 501 until implemented. Fill in the bodies (load/update the
/// <c>User</c> via <see cref="AppDbContext"/>, map to <see cref="UserProfileDto"/>).
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _db;
    public UserService(AppDbContext db) => _db = db;

    public Task<UserProfileDto> GetProfileAsync(Guid userId) =>
        throw AppException.NotImplemented("P2-2: GET /api/users/me not implemented yet.");

    public Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request) =>
        throw AppException.NotImplemented("P2-2: PUT /api/users/me not implemented yet.");
}

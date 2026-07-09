namespace Novacart.Api.Models.Entities;

/// <summary>
/// Join entity for the many-to-many between <see cref="User"/> and <see cref="Role"/>.
/// Composite key (UserId, RoleId) is configured in AppDbContext.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
}

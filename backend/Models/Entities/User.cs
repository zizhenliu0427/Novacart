namespace Novacart.Api.Models.Entities;

/// <summary>
/// Application user. Passwords are stored only as bcrypt hashes.
/// Roles are attached via the <see cref="UserRole"/> join table (many-to-many).
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

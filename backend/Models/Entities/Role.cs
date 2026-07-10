namespace Novacart.Api.Models.Entities;

/// <summary>
/// A role a user can hold (customer, admin, sysadmin, …).
/// Adding a new role later is a single data insert — no code change.
/// </summary>
public class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

/// <summary>Well-known role names / ids, kept in sync with the seed data.</summary>
public static class RoleNames
{
    public const string Customer = "customer";
    public const string Admin = "admin";
    public const string SysAdmin = "sysadmin";

    public const int CustomerId = 1;
    public const int AdminId = 2;
    public const int SysAdminId = 3;

    /// <summary>Comma-separated roles for `[Authorize(Roles = RoleNames.AdminRoles)]` on admin endpoints (P2-1).</summary>
    public const string AdminRoles = Admin + "," + SysAdmin;
}

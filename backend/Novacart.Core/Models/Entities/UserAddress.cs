namespace Novacart.Api.Models.Entities;

/// <summary>
/// P2-7 (Shipping): a user's saved shipping/billing address. Also the source that
/// gets snapshotted onto an order at checkout. See HANDOFF §7 P2-7.
/// </summary>
public class UserAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Label { get; set; } = "home";
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string Country { get; set; } = "Australia";

    public bool IsDefaultShipping { get; set; }
    public bool IsDefaultBilling { get; set; }
}

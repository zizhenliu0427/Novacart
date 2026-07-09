namespace Novacart.Api.Models.Entities;

/// <summary>
/// Registry of supported payment gateways/methods (e.g. stripe, paypal).
/// </summary>
public class PaymentMethod
{
    public int Id { get; set; }

    /// <summary>Unique registry code: "stripe", "paypal", etc.</summary>
    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    /// <summary>Gateway-specific configuration as raw JSON (Postgres jsonb).</summary>
    public string? Config { get; set; }
}

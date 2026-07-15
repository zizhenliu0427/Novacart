using System.ComponentModel.DataAnnotations;

namespace Novacart.Api.Models.Dtos.Payments;

public class CheckoutRequest
{
    [Required]
    public string SuccessUrl { get; set; } = string.Empty;

    [Required]
    public string CancelUrl { get; set; } = string.Empty;

    [Required]
    public Guid AddressId { get; set; }
}

public class CheckoutResponseDto
{
    public string RedirectUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class PaymentSessionResult
{
    public string ProviderTransactionId { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty;
}

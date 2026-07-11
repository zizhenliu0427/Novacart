using System.ComponentModel.DataAnnotations;

namespace Novacart.Api.Models.Dtos.Address;

public record UserAddressDto(
    Guid Id,
    string Label,
    string Line1,
    string? Line2,
    string City,
    string State,
    string Postcode,
    string Country,
    bool IsDefaultShipping,
    bool IsDefaultBilling
);

public record AddressCreateUpdateDto(
    [Required, MaxLength(50)] string Label,
    [Required, MaxLength(200)] string Line1,
    [MaxLength(200)] string? Line2,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(100)] string State,
    [Required, MaxLength(20)] string Postcode,
    [Required, MaxLength(100)] string Country,
    bool IsDefaultShipping,
    bool IsDefaultBilling
);

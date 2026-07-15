using Microsoft.EntityFrameworkCore;
using Novacart.Api.Data;
using Novacart.Api.Models.Dtos.Address;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

public interface IAddressService
{
    Task<IReadOnlyList<UserAddressDto>> GetAddressesAsync(Guid userId);
    Task<UserAddressDto> CreateAddressAsync(Guid userId, AddressCreateUpdateDto dto);
    Task<UserAddressDto> UpdateAddressAsync(Guid userId, Guid addressId, AddressCreateUpdateDto dto);
    Task DeleteAddressAsync(Guid userId, Guid addressId);
}

public class AddressService : IAddressService
{
    private readonly AppDbContext _db;

    public AddressService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<UserAddressDto>> GetAddressesAsync(Guid userId)
    {
        var addresses = await _db.UserAddresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefaultShipping)
            .ThenBy(a => a.Label)
            .ToListAsync();

        return addresses.Select(MapToDto).ToList();
    }

    public async Task<UserAddressDto> CreateAddressAsync(Guid userId, AddressCreateUpdateDto dto)
    {
        var address = new UserAddress
        {
            UserId = userId,
            Label = dto.Label,
            Line1 = dto.Line1,
            Line2 = dto.Line2,
            City = dto.City,
            State = dto.State,
            Postcode = dto.Postcode,
            Country = dto.Country,
            IsDefaultShipping = dto.IsDefaultShipping,
            IsDefaultBilling = dto.IsDefaultBilling
        };

        if (address.IsDefaultShipping)
        {
            await ResetDefaultShippingAsync(userId);
        }
        if (address.IsDefaultBilling)
        {
            await ResetDefaultBillingAsync(userId);
        }

        _db.UserAddresses.Add(address);
        await _db.SaveChangesAsync();

        return MapToDto(address);
    }

    public async Task<UserAddressDto> UpdateAddressAsync(Guid userId, Guid addressId, AddressCreateUpdateDto dto)
    {
        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId)
            ?? throw AppException.NotFound("Address");

        address.Label = dto.Label;
        address.Line1 = dto.Line1;
        address.Line2 = dto.Line2;
        address.City = dto.City;
        address.State = dto.State;
        address.Postcode = dto.Postcode;
        address.Country = dto.Country;

        if (dto.IsDefaultShipping && !address.IsDefaultShipping)
        {
            await ResetDefaultShippingAsync(userId);
        }
        address.IsDefaultShipping = dto.IsDefaultShipping;

        if (dto.IsDefaultBilling && !address.IsDefaultBilling)
        {
            await ResetDefaultBillingAsync(userId);
        }
        address.IsDefaultBilling = dto.IsDefaultBilling;

        await _db.SaveChangesAsync();
        return MapToDto(address);
    }

    public async Task DeleteAddressAsync(Guid userId, Guid addressId)
    {
        var address = await _db.UserAddresses
            .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId)
            ?? throw AppException.NotFound("Address");

        _db.UserAddresses.Remove(address);
        await _db.SaveChangesAsync();
    }

    private async Task ResetDefaultShippingAsync(Guid userId)
    {
        var defaults = await _db.UserAddresses
            .Where(a => a.UserId == userId && a.IsDefaultShipping)
            .ToListAsync();

        foreach (var addr in defaults)
        {
            addr.IsDefaultShipping = false;
        }
    }

    private async Task ResetDefaultBillingAsync(Guid userId)
    {
        var defaults = await _db.UserAddresses
            .Where(a => a.UserId == userId && a.IsDefaultBilling)
            .ToListAsync();

        foreach (var addr in defaults)
        {
            addr.IsDefaultBilling = false;
        }
    }

    private static UserAddressDto MapToDto(UserAddress a) =>
        new(a.Id, a.Label, a.Line1, a.Line2, a.City, a.State, a.Postcode, a.Country, a.IsDefaultShipping, a.IsDefaultBilling);
}

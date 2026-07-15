using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Novacart.Api.Services;
using Novacart.Api.Models.Dtos.Address;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Tests;

/// <summary>
/// Unit tests for AddressService — P2-7 (shipping address management).
/// Covers the state-change logic that no other test exercises: default-address
/// uniqueness enforcement, ownership checks, and update transitions.
/// </summary>
public class AddressServiceTests
{
    private static AddressCreateUpdateDto ValidDto(
        string label = "Home",
        bool shipping = false,
        bool billing = false) =>
        new(label, "1 Main St", null, "Sydney", "NSW", "2000", "Australia", shipping, billing);

    [Fact]
    public async Task GetAddressesAsync_ReturnsOnlyUsersOwnAddresses_OrderedDefaultFirst()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var userA = await TestDbFactory.SeedTestUserAsync(db, "a@example.com");
        var userB = await TestDbFactory.SeedTestUserAsync(db, "b@example.com");

        await svc.CreateAddressAsync(userB, ValidDto("Other's"));
        await svc.CreateAddressAsync(userA, ValidDto("Plain", shipping: false));
        await svc.CreateAddressAsync(userA, ValidDto("Default", shipping: true));

        var result = await svc.GetAddressesAsync(userA);

        result.Should().HaveCount(2);
        result.Should().NotContain(a => a.Label == "Other's");
        // Default shipping sorts first.
        result[0].Label.Should().Be("Default");
        result[0].IsDefaultShipping.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAddressAsync_ResetsExistingDefaultShipping_WhenNewIsDefault()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var first = await svc.CreateAddressAsync(userId, ValidDto("Old Default", shipping: true));
        await svc.CreateAddressAsync(userId, ValidDto("New Default", shipping: true));

        var addresses = await svc.GetAddressesAsync(userId);
        addresses.First(a => a.Label == "Old Default").IsDefaultShipping.Should().BeFalse();
        addresses.First(a => a.Label == "New Default").IsDefaultShipping.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAddressAsync_ResetsExistingDefaultBilling_WhenNewIsDefault()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        await svc.CreateAddressAsync(userId, ValidDto("Old Billing", billing: true));
        await svc.CreateAddressAsync(userId, ValidDto("New Billing", billing: true));

        var addresses = await svc.GetAddressesAsync(userId);
        addresses.First(a => a.Label == "Old Billing").IsDefaultBilling.Should().BeFalse();
        addresses.First(a => a.Label == "New Billing").IsDefaultBilling.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAddressAsync_ThrowsNotFound_WhenAddressBelongsToAnotherUser()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var owner = await TestDbFactory.SeedTestUserAsync(db, "owner@example.com");
        var intruder = await TestDbFactory.SeedTestUserAsync(db, "intruder@example.com");

        var address = await svc.CreateAddressAsync(owner, ValidDto());

        var act = () => svc.UpdateAddressAsync(intruder, address.Id, ValidDto("Hijacked"));

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task DeleteAddressAsync_ThrowsNotFound_WhenAddressBelongsToAnotherUser()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var owner = await TestDbFactory.SeedTestUserAsync(db, "owner@example.com");
        var intruder = await TestDbFactory.SeedTestUserAsync(db, "intruder@example.com");

        var address = await svc.CreateAddressAsync(owner, ValidDto());

        var act = () => svc.DeleteAddressAsync(intruder, address.Id);

        await act.Should().ThrowAsync<AppException>().Where(e => e.StatusCode == 404);
    }

    [Fact]
    public async Task UpdateAddressAsync_PromotesToDefault_AndResetsPreviousDefault()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var current = await svc.CreateAddressAsync(userId, ValidDto("Current Default", shipping: true));
        var other = await svc.CreateAddressAsync(userId, ValidDto("Becomes Default"));

        // Promote 'other' to default shipping via update.
        await svc.UpdateAddressAsync(userId, other.Id, ValidDto("Becomes Default", shipping: true));

        var addresses = await svc.GetAddressesAsync(userId);
        addresses.First(a => a.Id == current.Id).IsDefaultShipping.Should().BeFalse();
        addresses.First(a => a.Id == other.Id).IsDefaultShipping.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAddressAsync_RemovesAddress()
    {
        using var db = TestDbFactory.Create();
        var svc = new AddressService(db);
        var userId = await TestDbFactory.SeedTestUserAsync(db);

        var address = await svc.CreateAddressAsync(userId, ValidDto());
        await svc.DeleteAddressAsync(userId, address.Id);

        var remaining = await svc.GetAddressesAsync(userId);
        remaining.Should().BeEmpty();
    }
}

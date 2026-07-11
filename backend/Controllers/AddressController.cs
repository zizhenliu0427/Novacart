using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Models.Dtos.Address;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addresses;

    public AddressController(IAddressService addresses) => _addresses = addresses;

    private Guid GetUserId()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
               ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : throw new UnauthorizedAccessException();
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserAddressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddresses()
    {
        return Ok(await _addresses.GetAddressesAsync(GetUserId()));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserAddressDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAddress([FromBody] AddressCreateUpdateDto dto)
    {
        var address = await _addresses.CreateAddressAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetAddresses), new { id = address.Id }, address);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserAddressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] AddressCreateUpdateDto dto)
    {
        return Ok(await _addresses.UpdateAddressAsync(GetUserId(), id, dto));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id)
    {
        await _addresses.DeleteAddressAsync(GetUserId(), id);
        return NoContent();
    }
}

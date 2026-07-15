using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Services;

namespace Novacart.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currency;

    public CurrencyController(ICurrencyService currency) => _currency = currency;

    /// <summary>
    /// Latest exchange rates with AUD as base. Rates show target currency units per 1 AUD.
    /// Data from Frankfurter (ECB reference), cached server-side.
    /// </summary>
    [HttpGet("rates")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetRates(CancellationToken cancellationToken)
    {
        var rates = await _currency.GetRatesAsync(cancellationToken);
        return Ok(rates);
    }
}

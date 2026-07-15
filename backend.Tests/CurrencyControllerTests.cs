using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Novacart.Api.Controllers;
using Novacart.Api.Models.Dtos.Currency;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class CurrencyControllerTests
{
    private sealed class StubCurrencyService : ICurrencyService
    {
        public Task<ExchangeRatesDto> GetRatesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new ExchangeRatesDto
            {
                Base = "AUD",
                Date = "2026-07-14",
                Source = "test",
                Rates = new Dictionary<string, decimal>
                {
                    ["USD"] = 0.69m,
                    ["GBP"] = 0.51m,
                    ["NZD"] = 1.09m,
                },
            });
    }

    [Fact]
    public async Task GetRates_ReturnsOkWithAudBaseAndRates()
    {
        var controller = new CurrencyController(new StubCurrencyService());

        var result = await controller.GetRates(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<ExchangeRatesDto>().Subject;
        dto.Base.Should().Be("AUD");
        dto.Date.Should().Be("2026-07-14");
        dto.Rates.Should().ContainKeys("USD", "GBP", "NZD");
        dto.Rates["USD"].Should().Be(0.69m);
    }
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Novacart.Api.Models.Dtos.Currency;

namespace Novacart.Api.Services;

public interface ICurrencyService
{
    /// <summary>Latest AUD-based rates for supported display currencies.</summary>
    Task<ExchangeRatesDto> GetRatesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fetches exchange rates from the free Frankfurter API (ECB reference data).
/// Cached in Redis to avoid hammering the public API (rates update ~daily).
/// </summary>
public class CurrencyService : ICurrencyService
{
    public const string BaseCurrency = "AUD";
    public static readonly string[] SupportedTargets = ["USD", "CNY", "JPY", "SGD", "GBP", "NZD"];

    private const string CacheKey = "currency:rates:AUD";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly HttpClient _http;
    private readonly IRedisCacheService _cache;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(HttpClient http, IRedisCacheService cache, ILogger<CurrencyService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExchangeRatesDto> GetRatesAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync<ExchangeRatesDto>(CacheKey);
        if (cached is not null)
        {
            cached.Source = "cache";
            return cached;
        }

        try
        {
            var symbols = string.Join(',', SupportedTargets);
            var url = $"https://api.frankfurter.dev/v1/latest?base={BaseCurrency}&symbols={symbols}";
            var response = await _http.GetFromJsonAsync<FrankfurterResponse>(url, cancellationToken);

            if (response?.Rates is null || response.Rates.Count == 0)
            {
                _logger.LogWarning("Frankfurter returned empty rates.");
                return FallbackRates();
            }

            var dto = new ExchangeRatesDto
            {
                Base = BaseCurrency,
                Date = response.Date,
                Rates = response.Rates.ToDictionary(kv => kv.Key, kv => kv.Value),
                Source = "live",
            };

            await _cache.SetAsync(CacheKey, dto, CacheTtl);
            return dto;
        }
        catch (Exception ex) when (ex is not AppException)
        {
            _logger.LogWarning(ex, "Failed to fetch exchange rates from Frankfurter.");

            var stale = await _cache.GetAsync<ExchangeRatesDto>(CacheKey);
            if (stale is not null)
            {
                stale.Source = "cache-stale";
                return stale;
            }

            // Last-resort static fallbacks so the UI still works offline/demo.
            return FallbackRates();
        }
    }

    private static ExchangeRatesDto FallbackRates() => new()
    {
        Base = BaseCurrency,
        Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
        Source = "fallback",
        Rates = new Dictionary<string, decimal>
        {
            ["USD"] = 0.69m,
            ["CNY"] = 4.71m,
            ["JPY"] = 112.6m,
            ["SGD"] = 0.90m,
            ["GBP"] = 0.51m,
            ["NZD"] = 1.09m,
        },
    };

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("base")]
        public string Base { get; set; } = "AUD";

        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }
}

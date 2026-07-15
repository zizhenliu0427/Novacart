namespace Novacart.Api.Models.Dtos.Currency;

/// <summary>
/// Exchange rates with base AUD. Each rate is how many units of the target currency equal 1 AUD.
/// Source: Frankfurter API (ECB reference rates, updated on working days).
/// </summary>
public class ExchangeRatesDto
{
    public string Base { get; set; } = "AUD";
    /// <summary>ISO date of the rate snapshot (YYYY-MM-DD).</summary>
    public string Date { get; set; } = string.Empty;
    public Dictionary<string, decimal> Rates { get; set; } = new();
    /// <summary>Where the data came from (live API or cache).</summary>
    public string Source { get; set; } = "live";
}

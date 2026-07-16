using System.Text.Json;

namespace Novacart.Api.Infrastructure.Chatbot;

public sealed record SupportFaqEntry(string Question, string Answer, IReadOnlyList<string> Keywords);

public interface ISupportFaqStore
{
    IReadOnlyList<SupportFaqEntry> GetFaq(string locale);
    string? MatchFaq(string locale, string message);
}

public sealed class SupportFaqStore : ISupportFaqStore
{
    private readonly Dictionary<string, IReadOnlyList<SupportFaqEntry>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<SupportFaqEntry> GetFaq(string locale)
    {
        var key = NormalizeLocale(locale);
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        var path = Path.Combine(AppContext.BaseDirectory, "Data", $"support-faq.{key}.json");
        if (!File.Exists(path))
            path = Path.Combine(AppContext.BaseDirectory, "Data", "support-faq.en.json");

        if (!File.Exists(path))
        {
            _cache[key] = Array.Empty<SupportFaqEntry>();
            return _cache[key];
        }

        var json = File.ReadAllText(path);
        var entries = JsonSerializer.Deserialize<List<SupportFaqEntry>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? [];

        _cache[key] = entries;
        return entries;
    }

    public string? MatchFaq(string locale, string message)
    {
        var normalized = message.Trim().ToLowerInvariant();
        if (normalized.Length == 0) return null;

        foreach (var entry in GetFaq(locale))
        {
            if (entry.Keywords.Any(k => normalized.Contains(k.ToLowerInvariant())))
                return entry.Answer;
        }

        return null;
    }

    private static string NormalizeLocale(string locale)
        => locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
}

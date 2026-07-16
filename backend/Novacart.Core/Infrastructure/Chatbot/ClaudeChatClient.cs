using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Novacart.Api.Infrastructure.Chatbot;

public sealed class ClaudeChatClient : IChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly ChatbotOptions _options;

    public ClaudeChatClient(HttpClient http, IOptions<ChatbotOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

    public string ProviderName => "claude";

    public async Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Claude.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Chatbot:Claude:ApiKey is not configured.");

        var model = request.ModelOverride ?? _options.Claude.Model;
        var system = request.Messages.FirstOrDefault(m => m.Role == "system")?.Content;
        var conversation = request.Messages
            .Where(m => m.Role is "user" or "assistant")
            .Select(m => new { role = m.Role, content = m.Content })
            .ToArray();

        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["max_tokens"] = _options.MaxTokens,
            ["messages"] = conversation,
        };
        if (!string.IsNullOrWhiteSpace(system))
            payload["system"] = system;

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.Claude.BaseUrl.TrimEnd('/')}/messages");
        req.Headers.Add("x-api-key", apiKey);
        req.Headers.Add("anthropic-version", "2023-06-01");
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Claude API error ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? string.Empty;
        return new ChatCompletionResult(content.Trim(), ProviderName);
    }
}

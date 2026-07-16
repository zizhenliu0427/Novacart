using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Novacart.Api.Infrastructure.Chatbot;

public sealed class OpenAiChatClient : IChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly ChatbotOptions _options;

    public OpenAiChatClient(HttpClient http, IOptions<ChatbotOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

    public string ProviderName => "openai";

    public async Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var apiKey = _options.OpenAI.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Chatbot:OpenAI:ApiKey is not configured.");

        var model = request.ModelOverride ?? _options.Model;
        var payload = new
        {
            model,
            max_tokens = _options.MaxTokens,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.OpenAI.BaseUrl.TrimEnd('/')}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"OpenAI API error ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? string.Empty;
        return new ChatCompletionResult(content.Trim(), ProviderName);
    }
}

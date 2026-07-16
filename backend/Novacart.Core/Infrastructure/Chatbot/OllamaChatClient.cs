using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Novacart.Api.Infrastructure.Chatbot;

public sealed class OllamaChatClient : IChatCompletionClient
{
    private readonly HttpClient _http;
    private readonly ChatbotOptions _options;

    public OllamaChatClient(HttpClient http, IOptions<ChatbotOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);
    }

    public string ProviderName => "ollama";

    public async Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        var model = request.ModelOverride ?? _options.Ollama.Model;
        var payload = new
        {
            model,
            stream = false,
            messages = request.Messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_options.Ollama.BaseUrl.TrimEnd('/')}/api/chat");
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await _http.SendAsync(req, cancellationToken);
        var body = await res.Content.ReadAsStringAsync(cancellationToken);
        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ollama API error ({(int)res.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        return new ChatCompletionResult(content.Trim(), ProviderName);
    }
}

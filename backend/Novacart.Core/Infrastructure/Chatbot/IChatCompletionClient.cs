namespace Novacart.Api.Infrastructure.Chatbot;

public sealed record ChatMessageDto(string Role, string Content);

public sealed record ChatCompletionRequest(
    IReadOnlyList<ChatMessageDto> Messages,
    string? ModelOverride = null);

public sealed record ChatCompletionResult(string Content, string Provider);

public interface IChatCompletionClient
{
    string ProviderName { get; }
    Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default);
}

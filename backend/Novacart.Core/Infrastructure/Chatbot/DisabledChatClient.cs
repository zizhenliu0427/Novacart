namespace Novacart.Api.Infrastructure.Chatbot;

public sealed class DisabledChatClient : IChatCompletionClient
{
    public static readonly DisabledChatClient Instance = new();
    public string ProviderName => "disabled";

    public Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Chatbot LLM provider is disabled.");
}

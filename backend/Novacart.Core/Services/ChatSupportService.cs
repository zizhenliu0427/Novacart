using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure.Chatbot;
using Novacart.Api.Models.Dtos.Support;

namespace Novacart.Api.Services;

public interface IChatSupportService
{
    Task<SendChatMessageResponse> SendMessageAsync(Guid? userId, SendChatMessageRequest request, CancellationToken cancellationToken = default);
    IReadOnlyList<SupportFaqItemDto> GetFaq(string locale);
}

public sealed class ChatSupportService : IChatSupportService
{
    private readonly ChatbotOptions _options;
    private readonly IChatCompletionClient _client;
    private readonly ISupportContextBuilder _context;
    private readonly ISupportFaqStore _faq;
    private readonly ILogger<ChatSupportService> _logger;

    public ChatSupportService(
        IOptions<ChatbotOptions> options,
        IChatCompletionClient client,
        ISupportContextBuilder context,
        ISupportFaqStore faq,
        ILogger<ChatSupportService> logger)
    {
        _options = options.Value;
        _client = client;
        _context = context;
        _faq = faq;
        _logger = logger;
    }

    public async Task<SendChatMessageResponse> SendMessageAsync(
        Guid? userId,
        SendChatMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var locale = request.Locale.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "zh" : "en";
        var userMessage = request.Message.Trim();

        var faqMatch = _faq.MatchFaq(locale, userMessage);
        if (!_options.Enabled || _options.Provider == ChatbotProvider.Disabled)
        {
            return new SendChatMessageResponse
            {
                Reply = faqMatch ?? DefaultFallback(locale),
                Source = "faq",
                Provider = "disabled",
            };
        }

        try
        {
            var systemPrompt = await _context.BuildSystemPromptAsync(locale, userId, cancellationToken);
            var messages = BuildMessages(systemPrompt, request.History, userMessage);
            var result = await _client.CompleteAsync(new ChatCompletionRequest(messages), cancellationToken);

            _logger.LogInformation(
                "Chat support reply provider={Provider} userId={UserIdHash} locale={Locale} latencyMs=n/a",
                result.Provider,
                userId?.GetHashCode().ToString("X8") ?? "guest",
                locale);

            return new SendChatMessageResponse
            {
                Reply = result.Content,
                Source = "ai",
                Provider = result.Provider,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chat LLM failed; falling back to FAQ");
            return new SendChatMessageResponse
            {
                Reply = faqMatch ?? DefaultFallback(locale),
                Source = "faq",
                Provider = _client.ProviderName,
            };
        }
    }

    public IReadOnlyList<SupportFaqItemDto> GetFaq(string locale)
        => _faq.GetFaq(locale)
            .Select(e => new SupportFaqItemDto { Question = e.Question, Answer = e.Answer })
            .ToList();

    private List<ChatMessageDto> BuildMessages(string systemPrompt, List<ChatHistoryMessageDto> history, string userMessage)
    {
        var messages = new List<ChatMessageDto> { new("system", systemPrompt) };
        var maxHistory = Math.Clamp(_options.MaxHistoryMessages, 0, 20);

        foreach (var item in history.TakeLast(maxHistory))
        {
            var role = item.Role is "assistant" or "user" ? item.Role : "user";
            messages.Add(new ChatMessageDto(role, PiiRedactor.Redact(item.Content)));
        }

        messages.Add(new ChatMessageDto("user", PiiRedactor.Redact(userMessage)));
        return messages;
    }

    private static string DefaultFallback(string locale)
        => locale == "zh"
            ? "抱歉，我暂时无法回答。请查看下方常见问题，或联系人工客服。"
            : "Sorry, I couldn't answer that right now. Please check the FAQ below or contact our support team.";
}

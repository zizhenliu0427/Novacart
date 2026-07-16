using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Novacart.Api.Infrastructure.Chatbot;
using Novacart.Api.Models.Dtos.Support;
using Novacart.Api.Services;
using Xunit;

namespace Novacart.Api.Tests;

public class ChatSupportServiceTests
{
    private sealed class StubClient(string reply) : IChatCompletionClient
    {
        public string ProviderName => "stub";
        public Task<ChatCompletionResult> CompleteAsync(ChatCompletionRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new ChatCompletionResult(reply, ProviderName));
    }

    private static ChatSupportService CreateService(bool enabled, IChatCompletionClient? client = null)
    {
        var options = Options.Create(new ChatbotOptions
        {
            Enabled = enabled,
            Provider = enabled ? ChatbotProvider.OpenAI : ChatbotProvider.Disabled,
        });
        var faq = new SupportFaqStore();
        var context = new StubContextBuilder();
        return new ChatSupportService(
            options,
            client ?? new StubClient("Hello from AI"),
            context,
            faq,
            NullLogger<ChatSupportService>.Instance);
    }

    [Fact]
    public async Task SendMessage_WhenDisabled_ReturnsFaqOrFallback()
    {
        var svc = CreateService(enabled: false);
        var response = await svc.SendMessageAsync(null, new SendChatMessageRequest
        {
            Message = "What is your returns policy?",
            Locale = "en",
        });

        response.Source.Should().Be("faq");
        response.Provider.Should().Be("disabled");
        response.Reply.Should().Contain("30 days");
    }

    [Fact]
    public async Task SendMessage_WhenEnabled_UsesLlm()
    {
        var svc = CreateService(enabled: true, new StubClient("AI reply"));
        var response = await svc.SendMessageAsync(null, new SendChatMessageRequest
        {
            Message = "Hello",
            Locale = "en",
        });

        response.Source.Should().Be("ai");
        response.Reply.Should().Be("AI reply");
    }

    private sealed class StubContextBuilder : ISupportContextBuilder
    {
        public Task<string> BuildSystemPromptAsync(string locale, Guid? userId, CancellationToken cancellationToken = default)
            => Task.FromResult("system");
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Novacart.Api.Infrastructure.Chatbot;
using Novacart.Api.Services;

namespace Novacart.Api.Infrastructure;

public static class ChatSupportExtensions
{
    public static IServiceCollection AddNovacartChatSupport(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ChatbotOptions>(configuration.GetSection(ChatbotOptions.SectionName));

        var chatPermit = configuration.GetValue("Chatbot:RateLimitPerMinute", 10);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("chat", limiter =>
            {
                limiter.PermitLimit = chatPermit;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
        });

        services.AddHttpClient<OpenAiChatClient>();
        services.AddHttpClient<OllamaChatClient>();
        services.AddHttpClient<ClaudeChatClient>();

        services.AddSingleton<ISupportFaqStore, SupportFaqStore>();
        services.AddScoped<ISupportContextBuilder, SupportContextBuilder>();
        services.AddScoped<IChatSupportService, ChatSupportService>();

        services.AddSingleton<IChatCompletionClient>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ChatbotOptions>>().Value;
            if (!opts.Enabled || opts.Provider == ChatbotProvider.Disabled)
                return DisabledChatClient.Instance;

            return opts.Provider switch
            {
                ChatbotProvider.OpenAI => sp.GetRequiredService<OpenAiChatClient>(),
                ChatbotProvider.Ollama => sp.GetRequiredService<OllamaChatClient>(),
                ChatbotProvider.Claude => sp.GetRequiredService<ClaudeChatClient>(),
                _ => DisabledChatClient.Instance,
            };
        });

        return services;
    }
}

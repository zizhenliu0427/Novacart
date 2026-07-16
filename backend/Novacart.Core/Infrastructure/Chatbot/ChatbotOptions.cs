namespace Novacart.Api.Infrastructure.Chatbot;

public enum ChatbotProvider
{
    Disabled = 0,
    OpenAI = 1,
    Ollama = 2,
    Claude = 3,
}

public class ChatbotOptions
{
    public const string SectionName = "Chatbot";

    public bool Enabled { get; set; }
    public ChatbotProvider Provider { get; set; } = ChatbotProvider.Disabled;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 512;
    public int MaxHistoryMessages { get; set; } = 10;
    public int RequestTimeoutSeconds { get; set; } = 30;

    public OpenAiOptions OpenAI { get; set; } = new();
    public OllamaOptions Ollama { get; set; } = new();
    public ClaudeOptions Claude { get; set; } = new();
}

public class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "llama3.2";
}

public class ClaudeOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";
    public string Model { get; set; } = "claude-3-5-haiku-20241022";
}

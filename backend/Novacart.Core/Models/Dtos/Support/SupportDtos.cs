using System.ComponentModel.DataAnnotations;

namespace Novacart.Api.Models.Dtos.Support;

public class ChatHistoryMessageDto
{
    [Required]
    [MaxLength(2000)]
    public string Role { get; set; } = "user";

    [Required]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;
}

public class SendChatMessageRequest
{
    [Required]
    [MinLength(1)]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(10)]
    public List<ChatHistoryMessageDto> History { get; set; } = [];

    [MaxLength(10)]
    public string Locale { get; set; } = "en";
}

public class SendChatMessageResponse
{
    public string Reply { get; set; } = string.Empty;
    public string Source { get; set; } = "ai";
    public string Provider { get; set; } = "disabled";
}

public class SupportFaqItemDto
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
}

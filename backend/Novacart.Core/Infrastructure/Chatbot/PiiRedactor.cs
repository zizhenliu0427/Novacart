using System.Text.RegularExpressions;

namespace Novacart.Api.Infrastructure.Chatbot;

public static partial class PiiRedactor
{
    [GeneratedRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:\+?\d{1,3}[\s-]?)?(?:\(\d{2,4}\)|\d{2,4})[\s-]?\d{3,4}[\s-]?\d{3,4}\b", RegexOptions.Compiled)]
    private static partial Regex PhoneRegex();

    public static string Redact(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var redacted = EmailRegex().Replace(text, "[email redacted]");
        redacted = PhoneRegex().Replace(redacted, "[phone redacted]");
        return redacted;
    }
}

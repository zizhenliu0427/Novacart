using FluentAssertions;
using Novacart.Api.Infrastructure.Chatbot;
using Xunit;

namespace Novacart.Api.Tests;

public class PiiRedactorTests
{
    [Fact]
    public void Redact_MasksEmailAndPhone()
    {
        var input = "Contact me at user@example.com or 0412 345 678.";
        var result = PiiRedactor.Redact(input);
        result.Should().NotContain("user@example.com");
        result.Should().NotContain("0412 345 678");
        result.Should().Contain("[email redacted]");
    }
}

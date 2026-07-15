using Novacart.Api.Services;

namespace Novacart.Api.Tests;

/// <summary>
/// Test double for <see cref="IEmailQueue"/>. Records enqueued messages so tests
/// can assert that an email was queued (without actually sending anything).
/// </summary>
public class FakeEmailQueue : IEmailQueue
{
    public List<EmailMessage> Enqueued { get; } = new();

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Enqueued.Add(message);
        return ValueTask.CompletedTask;
    }
}

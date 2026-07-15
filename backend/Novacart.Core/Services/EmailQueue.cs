using System.Threading.Channels;
using Novacart.Api.Models.Entities;

namespace Novacart.Api.Services;

/// <summary>
/// A queued email command. Captures the minimum data needed to render the email
/// so the background worker does not depend on an EF entity that may be disposed
/// by the time the message is processed (the worker runs in its own scope).
/// </summary>
public record EmailMessage
{
    /// <summary>Discriminator telling the worker which template to send.</summary>
    public EmailKind Kind { get; init; }

    public string Recipient { get; init; } = string.Empty;

    // Snapshot fields (avoid passing a tracked Order entity across scopes).
    public string? OrderNumber { get; init; }
    public decimal? OrderTotal { get; init; }
    public string? NewStatus { get; init; }
}

public enum EmailKind { OrderConfirmation, OrderStatusUpdate }

/// <summary>
/// Bounded in-process queue decoupling email sending from request/webhook handling.
/// Producers enqueue and return immediately; the <see cref="EmailBackgroundWorker"/>
/// consumes on a background thread and calls <see cref="IEmailService"/>.
/// </summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public class EmailQueue : IEmailQueue
{
    // Bounded channel: applies back-pressure under load rather than unbounded growth.
    private readonly Channel<EmailMessage> _channel =
        Channel.CreateBounded<EmailMessage>(new BoundedChannelOptions(256)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(message, cancellationToken);

    /// <summary>Used by the background worker to read the next message.</summary>
    public IAsyncEnumerable<EmailMessage> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}

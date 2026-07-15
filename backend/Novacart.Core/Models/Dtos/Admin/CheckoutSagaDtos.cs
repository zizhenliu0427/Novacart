namespace Novacart.Api.Models.Dtos.Admin;

public record CheckoutSagaSummaryDto(
    Guid CorrelationId,
    Guid OrderId,
    string CurrentState,
    string OrderNumber,
    Guid UserId,
    string? UserEmail,
    string? OrderStatus,
    bool CanRetry);

public record CheckoutSagaListResponse(
    DateTime Timestamp,
    IReadOnlyList<CheckoutSagaSummaryDto> Sagas);

public record DlqRetryRequest(string QueueName, int MaxMessages = 10);

public record DlqRetryResponse(
    string QueueName,
    string TargetQueue,
    int MessagesRetried,
    string Message);

namespace Common.Events;

public record PaymentResultEvent
{
    public Guid OrderId { get; init; }
    public Guid UserId { get; init; }
    public bool IsSuccess { get; init; }
    public string FailureReason { get; init; } = string.Empty;
}
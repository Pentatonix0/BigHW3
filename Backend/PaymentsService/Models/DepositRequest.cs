namespace PaymentsService.Models;

public record DepositRequest(Guid UserId, decimal Amount);
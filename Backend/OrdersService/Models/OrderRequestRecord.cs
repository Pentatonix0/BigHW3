namespace OrdersService.Models;

public record CreateOrderRequest(Guid UserId, decimal Amount, string Description);
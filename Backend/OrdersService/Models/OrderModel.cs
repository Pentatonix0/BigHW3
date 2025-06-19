namespace OrdersService.Models;

public class OrderModel
{
	public Guid Id { get; set; }
	public Guid UserId { get; set; }
	public decimal Amount { get; set; }
	public string Description { get; set; } = string.Empty;
	public OrderStatusModel Status { get; set; }
	public DateTime CreatedOn { get; set; }
}
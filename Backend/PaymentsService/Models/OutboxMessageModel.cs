namespace PaymentsService.Models;

public class OutboxMessageModel
{
	public Guid Id { get; set; }
	public DateTime OccurredAt { get; set; }
	public string Type { get; set; } = string.Empty;
	public string Data { get; set; } = string.Empty;
	public DateTime? ProcessedAt { get; set; }
}
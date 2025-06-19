using System.ComponentModel.DataAnnotations;

namespace PaymentsService.Models;

public class InboxMessageModel
{
	[Key]
	public Guid MessageId { get; set; }
	public DateTime ProcessedAt { get; set; }
}
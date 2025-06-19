using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Database;
using Common.Events;
using System.Text.Json;

namespace PaymentsService.Messaging
{
	/// <summary>
	/// Фоновый процессор для обработки сообщений Outbox в сервисе Payments
	/// </summary>
	public class OutboxMessageProcessor : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider; // Провайдер сервисов
		private readonly ILogger<OutboxMessageProcessor> _logger; // Логгер для записи событий

		/// <summary>
		/// Инициализация процессора с зависимостями
		/// </summary>
		public OutboxMessageProcessor(IServiceProvider serviceProvider, ILogger<OutboxMessageProcessor> logger)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider), "Провайдер сервисов не может быть null");
			_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Логгер не может быть null");
		}

		/// <summary>
		/// Основной цикл выполнения обработки сообщений Outbox
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await HandlePendingMessagesAsync(cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			}
		}

		/// <summary>
		/// Обработка набора не обработанных сообщений из Outbox
		/// </summary>
		private async Task HandlePendingMessagesAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsServiceDbContext>();
			var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

			var pendingMessages = await dbContext.OutboxMessages
				.Where(m => m.ProcessedAt == null)
				.OrderBy(m => m.OccurredAt)
				.Take(20)
				.ToListAsync(cancellationToken);

			if (!pendingMessages.Any())
			{
				return;
			}

			_logger.LogInformation("Обнаружено {MessageCount} записей в Outbox сервиса Payments для обработки", pendingMessages.Count);

			foreach (var message in pendingMessages)
			{
				try
				{
					if (message.Type == nameof(PaymentResultEvent))
					{
						var paymentEvent = JsonSerializer.Deserialize<PaymentResultEvent>(message.Data);
						if (paymentEvent != null)
						{
							await publishEndpoint.Publish(paymentEvent, cancellationToken);
							_logger.LogInformation("Успешно отправлено событие платежа с ID {MessageId}", message.Id);
						}
					}
					else
					{
						_logger.LogWarning("Обнаружено сообщение с неизвестным типом {MessageType} в Outbox", message.Type);
					}

					message.ProcessedAt = DateTime.UtcNow;
					await dbContext.SaveChangesAsync(cancellationToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Произошла ошибка при обработке записи Outbox с ID {MessageId}", message.Id);
				}
			}
		}
	}
}
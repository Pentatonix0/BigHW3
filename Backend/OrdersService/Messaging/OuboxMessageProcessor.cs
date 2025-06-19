using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Database;
using Common.Events;
using System.Text.Json;

namespace OrdersService.Messaging
{
	/// <summary>
	/// Фоновый процессор для обработки сообщений из Outbox
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
		/// Основной цикл выполнения обработки сообщений
		/// </summary>
		protected override async Task ExecuteAsync(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				await HandleOutboxMessagesAsync(cancellationToken);
				await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			}
		}

		/// <summary>
		/// Обработка пакета сообщений из Outbox
		/// </summary>
		private async Task HandleOutboxMessagesAsync(CancellationToken cancellationToken)
		{
			using var scope = _serviceProvider.CreateAsyncScope();
			var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
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

			_logger.LogInformation("Найдено {MessageCount} не обработанных записей в Outbox", pendingMessages.Count);

			foreach (var message in pendingMessages)
			{
				try
				{
					if (message.Type == nameof(OrderCreatedEvent))
					{
						var eventData = JsonSerializer.Deserialize<OrderCreatedEvent>(message.Data);
						if (eventData != null)
						{
							await publishEndpoint.Publish(eventData, cancellationToken);
						}
					}

					message.ProcessedAt = DateTime.UtcNow;
					await dbContext.SaveChangesAsync(cancellationToken);

					_logger.LogInformation("Успешно обработана запись Outbox с ID {MessageId}", message.Id);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Произошла ошибка при обработке записи Outbox с ID {MessageId}", message.Id);
				}
			}
		}
	}
}
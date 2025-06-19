using MassTransit;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Database;
using PaymentsService.Models;
using Common.Events;
using System.Text.Json;

namespace PaymentsService.Messaging
{
	/// <summary>
	/// Потребитель событий создания заказа для обработки оплаты
	/// </summary>
	public class OrderPaymentConsumer : IConsumer<OrderCreatedEvent>
	{
		private readonly PaymentsServiceDbContext _dbContext; // Контекст базы данных
		private readonly ILogger<OrderPaymentConsumer> _logger; // Логгер для записи событий

		/// <summary>
		/// Инициализация потребителя с зависимостями
		/// </summary>
		public OrderPaymentConsumer(PaymentsServiceDbContext dbContext, ILogger<OrderPaymentConsumer> logger)
		{
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "Контекст базы данных не может быть null");
			_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Логгер не может быть null");
		}

		/// <summary>
		/// Обработка входящего события создания заказа
		/// </summary>
		public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
		{
			var eventData = context.Message;
			_logger.LogInformation("Обработка события OrderCreatedEvent для заказа с ID: {OrderId}", eventData.OrderId);

			if (await _dbContext.InboxMessages.AnyAsync(im => im.MessageId == context.MessageId))
			{
				_logger.LogWarning("Сообщение с ID {MessageId} уже было обработано ранее", context.MessageId);
				return;
			}

			await using var transactionScope = await _dbContext.Database.BeginTransactionAsync();

			try
			{
				_dbContext.InboxMessages.Add(new InboxMessageModel
				{
					MessageId = context.MessageId.Value,
					ProcessedAt = DateTime.UtcNow
				});

				var userAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == eventData.UserId);

				PaymentResultEvent paymentOutcome;

				if (userAccount == null)
				{
					_logger.LogWarning("Аккаунт отсутствует для UserId: {UserId}", eventData.UserId);
					paymentOutcome = GenerateFailedPayment(eventData, "Аккаунт не найден.");
				}
				else if (userAccount.Balance < eventData.Amount)
				{
					_logger.LogWarning("Недостаточный баланс для UserId: {UserId}. Текущий: {Balance}, Требуется: {Amount}",
						eventData.UserId, userAccount.Balance, eventData.Amount);
					paymentOutcome = GenerateFailedPayment(eventData, "Недостаточно средств.");
				}
				else
				{
					userAccount.Balance -= eventData.Amount;
					_logger.LogInformation("Успешное списание средств для UserId: {UserId}. Обновленный баланс: {Balance}",
						eventData.UserId, userAccount.Balance);
					paymentOutcome = new PaymentResultEvent
					{
						OrderId = eventData.OrderId,
						UserId = eventData.UserId,
						IsSuccess = true
					};
				}

				var outboxRecord = new OutboxMessageModel
				{
					Id = Guid.NewGuid(),
					OccurredAt = DateTime.UtcNow,
					Type = nameof(PaymentResultEvent),
					Data = JsonSerializer.Serialize(paymentOutcome)
				};
				_dbContext.OutboxMessages.Add(outboxRecord);

				await _dbContext.SaveChangesAsync();
				await transactionScope.CommitAsync();

				_logger.LogInformation("Оплата для заказа с ID {OrderId} завершена. Статус: {Result}",
					eventData.OrderId, paymentOutcome.IsSuccess ? "Успешно" : "Неудачно");
			}
			catch (DbUpdateConcurrencyException ex)
			{
				await transactionScope.RollbackAsync();
				_logger.LogError(ex, "Ошибка конкорренции при оплате заказа с ID: {OrderId}. Повторная попытка", eventData.OrderId);
				throw;
			}
			catch (Exception ex)
			{
				await transactionScope.RollbackAsync();
				_logger.LogError(ex, "Неожиданная ошибка при обработке заказа с ID: {OrderId}", eventData.OrderId);
				throw;
			}
		}

		/// <summary>
		/// Генерация события неудачной оплаты
		/// </summary>
		private PaymentResultEvent GenerateFailedPayment(OrderCreatedEvent eventData, string reason)
		{
			return new PaymentResultEvent
			{
				OrderId = eventData.OrderId,
				UserId = eventData.UserId,
				IsSuccess = false,
				FailureReason = reason
			};
		}
	}
}
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrdersService.Database;
using OrdersService.Models;
using Common.Events;

namespace OrdersService.Messaging
{
	/// <summary>
	/// Потребитель событий результата оплаты заказа
	/// </summary>
	public class PaymentResultConsumer : IConsumer<PaymentResultEvent>
	{
		private readonly OrdersDbContext _dbContext; // Контекст базы данных
		private readonly ILogger<PaymentResultConsumer> _logger; // Логгер для записи событий

		/// <summary>
		/// Инициализация потребителя с зависимостями
		/// </summary>
		public PaymentResultConsumer(OrdersDbContext dbContext, ILogger<PaymentResultConsumer> logger)
		{
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "Контекст базы данных не может быть null");
			_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Логгер не может быть null");
		}

		/// <summary>
		/// Обработка входящего события результата оплаты
		/// </summary>
		public async Task Consume(ConsumeContext<PaymentResultEvent> context)
		{
			var eventData = context.Message;
			_logger.LogInformation("Обработка события PaymentResultEvent для заказа с ID: {OrderId}", eventData.OrderId);

			var existingOrder = await _dbContext.Orders.FirstOrDefaultAsync(order => order.Id == eventData.OrderId);

			if (existingOrder == null)
			{
				_logger.LogError("Заказ с идентификатором {OrderId} отсутствует в системе", eventData.OrderId);
				return;
			}

			var previousStatus = existingOrder.Status;
			existingOrder.Status = eventData.IsSuccess ? OrderStatusModel.Finished : OrderStatusModel.Cancelled;

			await _dbContext.SaveChangesAsync();

			_logger.LogInformation(
				"Статус заказа {OrderId} изменен с '{PreviousStatus}' на '{CurrentStatus}'. Детали: {Details}",
				existingOrder.Id,
				previousStatus,
				existingOrder.Status,
				eventData.IsSuccess ? "Успешная оплата" : $"Ошибка: {eventData.FailureReason}"
			);
		}
	}
}
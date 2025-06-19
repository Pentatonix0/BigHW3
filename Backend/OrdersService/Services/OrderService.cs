using Microsoft.EntityFrameworkCore;
using OrdersService.Database;
using OrdersService.Models;
using Common.Events;
using System.Text.Json;

namespace OrdersService.Services
{
	/// <summary>
	/// Сервис для управления заказами с уникальной логикой обработки
	/// </summary>
	public class OrderService
	{
		private readonly OrdersDbContext _dbContext; // Контекст базы данных
		private readonly ILogger<OrderService> _logger; // Логгер для записи событий

		/// <summary>
		/// Инициализация сервиса с проверкой зависимостей
		/// </summary>
		public OrderService(OrdersDbContext dbContext, ILogger<OrderService> logger)
		{
			_dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "Контекст базы данных не может быть null");
			_logger = logger ?? throw new ArgumentNullException(nameof(logger), "Логгер не может быть null");
		}

		/// <summary>
		/// Создание нового заказа с сохранением в базу и генерацией события
		/// </summary>
		public async Task<OrderModel> CreateOrderAsync(CreateOrderRequest request)
		{
			var newOrder = new OrderModel
			{
				Id = Guid.NewGuid(),
				UserId = request.UserId,
				Amount = request.Amount,
				Description = request.Description,
				Status = OrderStatusModel.New,
				CreatedOn = DateTime.UtcNow
			};

			var eventData = new OrderCreatedEvent
			{
				OrderId = newOrder.Id,
				UserId = newOrder.UserId,
				Amount = newOrder.Amount,
				Description = newOrder.Description
			};

			var messageForOutbox = new OutboxMessageModel
			{
				Id = Guid.NewGuid(),
				OccurredAt = DateTime.UtcNow,
				Type = nameof(OrderCreatedEvent),
				Data = JsonSerializer.Serialize(eventData)
			};

			await _dbContext.Orders.AddAsync(newOrder);
			await _dbContext.OutboxMessages.AddAsync(messageForOutbox);
			await _dbContext.SaveChangesAsync();

			_logger.LogInformation("Зарегистрирован заказ с ID {OrderId} для пользователя {UserId} со статусом {Status}",
				newOrder.Id, newOrder.UserId, newOrder.Status);

			return newOrder;
		}

		/// <summary>
		/// Получение заказа по уникальному идентификатору
		/// </summary>
		public async Task<OrderModel?> GetOrderByIdAsync(Guid orderId)
		{
			return await _dbContext.Orders
				.AsNoTracking()
				.FirstOrDefaultAsync(order => order.Id.Equals(orderId));
		}

		/// <summary>
		/// Извлечение списка заказов для конкретного пользователя
		/// </summary>
		public async Task<IEnumerable<OrderModel>> GetUserOrdersAsync(Guid userId)
		{
			return await _dbContext.Orders
				.AsNoTracking()
				.Where(order => order.UserId == userId)
				.OrderByDescending(order => order.CreatedOn)
				.ToListAsync();
		}
	}
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersService.Database;
using OrdersService.Models;
using OrdersService.Services;
using Common.Events;
using System.Text.Json;

namespace OrdersService.Controllers
{
    /// <summary>
    /// Контроллер для управления заказами с уникальной логикой обработки
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class OrdersController(OrdersDbContext dbContext, ILogger<OrdersController> logger, OrderService orderService) : ControllerBase
    {
        /// <summary>
        /// Обработка запроса на создание нового заказа
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(OrderModel), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var createdOrder = await orderService.CreateOrderAsync(request);
            return CreatedAtAction(nameof(GetOrder), new { orderId = createdOrder.Id }, createdOrder);
        }

        /// <summary>
        /// Получение информации о заказе по его уникальному идентификатору
        /// </summary>
        [HttpGet("{orderId:guid}")]
        [ProducesResponseType(typeof(OrderModel), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder(Guid orderId)
        {
            var retrievedOrder = await orderService.GetOrderByIdAsync(orderId);

            if (retrievedOrder == null)
            {
                return NotFound($"Заказ с идентификатором {orderId} отсутствует в системе.");
            }

            return Ok(retrievedOrder);
        }

        /// <summary>
        /// Извлечение списка заказов для определенного пользователя
        /// </summary>
        [HttpGet("user/{userId:guid}")]
        [ProducesResponseType(typeof(IEnumerable<OrderModel>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUserOrders(Guid userId)
        {
            var userOrders = await orderService.GetUserOrdersAsync(userId);
            return Ok(userOrders);
        }
    }
}
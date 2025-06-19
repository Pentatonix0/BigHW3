using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OrdersService.Database;
using OrdersService.Messaging;
using OrdersService.Services;
using System.Text.Json.Serialization;
using Common.Events;

namespace OrdersService
{
	/// <summary>
	/// Основной класс настройки приложения OrdersService
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Точка входа в приложение с уникальной конфигурацией
		/// </summary>
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Настройка сериализации JSON с поддержкой перечислений
			builder.Services.AddControllers().AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

			// Добавление Swagger для документации API
			builder.Services.AddScoped<OrderService>();
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen(c =>
				{
					c.SwaggerDoc("v1", new OpenApiInfo { Title = "Orders API", Version = "v1" });
				});

			// Подключение к базе данных PostgreSQL
			var dbConnection = builder.Configuration.GetConnectionString("Postgres");
			builder.Services.AddDbContext<OrdersDbContext>(config =>
				config.UseNpgsql(dbConnection));

			// Настройка MassTransit для работы с RabbitMQ
			builder.Services.AddMassTransit(busConfig =>
			{
				busConfig.SetKebabCaseEndpointNameFormatter();
				busConfig.AddConsumer<PaymentResultConsumer>();
				busConfig.UsingRabbitMq((context, mqConfig) =>
				{
					mqConfig.Host(builder.Configuration.GetConnectionString("RabbitMQ"), "/", host =>
					{
						host.Username("user");
						host.Password("password");
					});
					mqConfig.ReceiveEndpoint("payment-result-event", endpoint =>
					{
						endpoint.Durable = true;
						endpoint.ConfigureConsumer<PaymentResultConsumer>(context);
					});
				});
			});

			// Регистрация фонового сервиса для обработки Outbox
			builder.Services.AddHostedService<OutboxMessageProcessor>();

			var app = builder.Build();

			// Активация middleware для Swagger
			app.UseSwagger();
			app.UseSwaggerUI(config =>
			{
				config.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders API V1");
			});

			app.UseAuthorization();
			app.MapControllers();

			// Выполнение миграций базы данных при старте
			using (var scope = app.Services.CreateScope())
			{
				var database = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
				database.Database.Migrate();
			}

			app.Run();
		}
	}
}
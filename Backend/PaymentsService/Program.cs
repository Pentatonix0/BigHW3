using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PaymentsService.Database;
using PaymentsService.Messaging;
using PaymentsService.Services;

namespace PaymentsService
{
	/// <summary>
	/// Основной класс настройки приложения PaymentsService
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Точка входа в приложение с уникальной конфигурацией
		/// </summary>
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Регистрация сервиса управления аккаунтами
			builder.Services.AddScoped<AccountPaymentsService>();

			// Настройка контроллеров и API-исследования
			builder.Services.AddControllers();
			builder.Services.AddEndpointsApiExplorer();

			// Настройка Swagger для документации
			builder.Services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "Payments API", Version = "v1" });
			});

			// Подключение к базе данных PostgreSQL
			var dbConnection = builder.Configuration.GetConnectionString("Postgres");
			builder.Services.AddDbContext<PaymentsServiceDbContext>(config =>
				config.UseNpgsql(dbConnection));

			// Настройка MassTransit для работы с RabbitMQ
			builder.Services.AddMassTransit(busConfig =>
			{
				busConfig.SetKebabCaseEndpointNameFormatter();
				busConfig.AddConsumer<OrderPaymentConsumer>();
				busConfig.UsingRabbitMq((context, mqConfig) =>
				{
					mqConfig.Host(builder.Configuration.GetConnectionString("RabbitMQ"), "/", host =>
					{
						host.Username("user");
						host.Password("password");
					});
					mqConfig.ReceiveEndpoint("order-created-event", endpoint =>
					{
						endpoint.ConfigureConsumer<OrderPaymentConsumer>(context);
					});
				});
			});

			// Регистрация фонового процессора Outbox
			builder.Services.AddHostedService<OutboxMessageProcessor>();

			var app = builder.Build();

			// Активация middleware для Swagger
			app.UseSwagger();
			app.UseSwaggerUI(config =>
			{
				config.SwaggerEndpoint("/swagger/v1/swagger.json", "Payments API V1");
			});

			app.UseAuthorization();
			app.MapControllers();

			// Выполнение миграций базы данных при старте
			using (var scope = app.Services.CreateScope())
			{
				var database = scope.ServiceProvider.GetRequiredService<PaymentsServiceDbContext>();
				database.Database.Migrate();
			}

			app.Run();
		}
	}
}
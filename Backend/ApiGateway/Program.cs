using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace GatewayService
{
	/// <summary>
	/// Основной класс настройки шлюза приложения
	/// </summary>
	public class Program
	{
		/// <summary>
		/// Точка входа с уникальной конфигурацией шлюза
		/// </summary>
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Определение уникального имени политики CORS
			const string CustomCorsPolicy = "_customCorsPolicy";

			// Настройка CORS для локального фронтенда
			builder.Services.AddCors(options =>
			{
				options.AddPolicy(name: CustomCorsPolicy, policy =>
				{
					policy.WithOrigins("http://localhost:5173")
						  .AllowAnyHeader()
						  .AllowAnyMethod();
				});
			});

			// Подключение конфигурации Ocelot из JSON
			builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

			// Регистрация сервисов Ocelot
			builder.Services.AddOcelot(builder.Configuration);

			// Настройка Swagger для Ocelot
			builder.Services.AddSwaggerForOcelot(builder.Configuration);

			// Добавление инструментов для API и генерации документации
			builder.Services.AddEndpointsApiExplorer();
			builder.Services.AddSwaggerGen();

			var app = builder.Build();

			// Включение статических файлов
			app.UseStaticFiles();

			// Применение политики CORS
			app.UseCors(CustomCorsPolicy);

			// Настройка и запуск Ocelot с UI Swagger
			app.UseSwaggerForOcelotUI(opt =>
			{
				opt.PathToSwaggerGenerator = "/swagger/docs";
			}).UseOcelot().Wait();

			app.Run();
		}
	}
}
using Microsoft.EntityFrameworkCore;
using PaymentsService.Database;
using PaymentsService.Models;

namespace PaymentsService.Services
{
    /// <summary>
    /// Сервис для управления платежными аккаунтами с уникальной логикой
    /// </summary>
    public class AccountPaymentsService
    {
        private readonly PaymentsServiceDbContext _dbContext; // Контекст базы данных
        private readonly ILogger<AccountPaymentsService> _logger; // Логгер для записи событий

        /// <summary>
        /// Инициализация сервиса с проверкой зависимостей
        /// </summary>
        public AccountPaymentsService(PaymentsServiceDbContext dbContext, ILogger<AccountPaymentsService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext), "Контекст базы данных не может быть null");
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Логгер не может быть null");
        }

        /// <summary>
        /// Создание нового платежного аккаунта
        /// </summary>
        public async Task<AccountModel> CreateAccountAsync(CreateAccountRequest request)
        {
            var isAccountPresent = await _dbContext.Accounts.AnyAsync(a => a.UserId == request.UserId);
            if (isAccountPresent)
            {
                throw new InvalidOperationException($"Аккаунт с идентификатором {request.UserId} уже зарегистрирован");
            }

            var newAccount = new AccountModel
            {
                Id = Guid.NewGuid(),
                UserId = request.UserId,
                Balance = 0
            };

            _dbContext.Accounts.Add(newAccount);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Зарегистрирован аккаунт с ID {UserId}", request.UserId);

            return newAccount;
        }

        /// <summary>
        /// Выполнение операции пополнения баланса
        /// </summary>
        public async Task<AccountModel> DepositAsync(DepositRequest request)
        {
            var existingAccount = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.UserId == request.UserId);
            if (existingAccount == null)
            {
                throw new KeyNotFoundException($"Аккаунт с ID {request.UserId} отсутствует в системе");
            }

            existingAccount.Balance += request.Amount;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Пополнение на {Amount} выполнено для аккаунта с ID {UserId}. Баланс: {Balance}",
                request.Amount, request.UserId, existingAccount.Balance);

            return existingAccount;
        }

        /// <summary>
        /// Получение баланса аккаунта по идентификатору пользователя
        /// </summary>
        public async Task<AccountModel?> GetAccountBalanceAsync(Guid userId)
        {
            return await _dbContext.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }
    }
}
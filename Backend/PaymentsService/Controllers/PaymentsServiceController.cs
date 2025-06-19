using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Database;
using PaymentsService.Models;
using PaymentsService.Services;

namespace PaymentsService.Controllers
{
	/// <summary>
	/// Контроллер для управления платежными аккаунтами с уникальной логикой
	/// </summary>
	[ApiController]
	[Route("[controller]")]
	public class AccountsController(PaymentsServiceDbContext dbContext, ILogger<AccountsController> logger, AccountPaymentsService accountService) : ControllerBase
	{
		/// <summary>
		/// Инициализация нового платежного аккаунта
		/// </summary>
		[HttpPost]
		[ProducesResponseType(StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
		{
			try
			{
				var newAccount = await accountService.CreateAccountAsync(request);
				return CreatedAtAction(nameof(GetBalance), new { userId = request.UserId }, newAccount);
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(ex.Message);
			}
		}

		/// <summary>
		/// Выполнение операции пополнения баланса аккаунта
		/// </summary>
		[HttpPost("deposit")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
		{
			try
			{
				var updatedAccount = await accountService.DepositAsync(request);
				return Ok(updatedAccount);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(ex.Message);
			}
		}

		/// <summary>
		/// Получение текущего баланса аккаунта по идентификатору пользователя
		/// </summary>
		[HttpGet("balance/{userId:guid}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> GetBalance(Guid userId)
		{
			var accountData = await accountService.GetAccountBalanceAsync(userId);

			if (accountData == null)
			{
				return NotFound($"Аккаунт с идентификатором {userId} отсутствует.");
			}

			return Ok(new { accountData.UserId, accountData.Balance });
		}
	}
}
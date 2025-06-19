using Microsoft.EntityFrameworkCore;
using PaymentsService.Models;

namespace PaymentsService.Database;

public class PaymentsServiceDbContext(DbContextOptions<PaymentsServiceDbContext> options) : DbContext(options)
{
	private const string Schema = "payments";

	public DbSet<AccountModel> Accounts { get; set; }
	public DbSet<InboxMessageModel> InboxMessages { get; set; }
	public DbSet<OutboxMessageModel> OutboxMessages { get; set; }

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.HasDefaultSchema(Schema);

		modelBuilder.Entity<AccountModel>()
			.HasIndex(a => a.UserId)
			.IsUnique();

		modelBuilder.Entity<AccountModel>()
			.Property(p => p.Version)
			.IsRowVersion();

		base.OnModelCreating(modelBuilder);
	}
}
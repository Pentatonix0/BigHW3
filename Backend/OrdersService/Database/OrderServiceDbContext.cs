using Microsoft.EntityFrameworkCore;
using OrdersService.Models;

namespace OrdersService.Database;

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    private const string Schema = "orders";

    public DbSet<OrderModel> Orders { get; set; }
    public DbSet<OutboxMessageModel> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<OrderModel>()
            .Property(o => o.Status)
            .HasConversion<string>();

        base.OnModelCreating(modelBuilder);
    }
}
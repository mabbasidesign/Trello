using Microsoft.EntityFrameworkCore;
using order_service.Models;

namespace order_service.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure decimal precision
        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.UnitPrice)
            .HasPrecision(18, 2);

        // Configure relationships
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Order)
            .WithMany(o => o.OrderItems)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        modelBuilder.Entity<Order>().HasData(
            new Order
            {
                Id = 1,
                CustomerName = "John Doe",
                CustomerEmail = "john@example.com",
                ShippingAddress = "123 Main St, New York, NY 10001",
                TotalAmount = 149.97m,
                Status = "Delivered",
                OrderDate = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                ShippedDate = new DateTime(2025, 12, 2, 14, 30, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 12, 5, 16, 0, 0, DateTimeKind.Utc)
            },
            new Order
            {
                Id = 2,
                CustomerName = "Jane Smith",
                CustomerEmail = "jane@example.com",
                ShippingAddress = "456 Oak Ave, Los Angeles, CA 90001",
                TotalAmount = 79.99m,
                Status = "Processing",
                OrderDate = new DateTime(2025, 12, 10, 15, 30, 0, DateTimeKind.Utc),
                CreatedAt = new DateTime(2025, 12, 10, 15, 30, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2025, 12, 10, 15, 30, 0, DateTimeKind.Utc)
            }
        );

        modelBuilder.Entity<OrderItem>().HasData(
            new OrderItem
            {
                Id = 1,
                OrderId = 1,
                ProductId = 1,
                ProductName = "Laptop",
                Quantity = 1,
                UnitPrice = 999.99m
            },
            new OrderItem
            {
                Id = 2,
                OrderId = 1,
                ProductId = 2,
                ProductName = "Mouse",
                Quantity = 2,
                UnitPrice = 25.00m
            },
            new OrderItem
            {
                Id = 3,
                OrderId = 2,
                ProductId = 3,
                ProductName = "Keyboard",
                Quantity = 1,
                UnitPrice = 79.99m
            }
        );
    }
}

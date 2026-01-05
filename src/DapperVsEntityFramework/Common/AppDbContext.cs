using DapperVsEntityFramework.EfCoreModels;
using Microsoft.EntityFrameworkCore;

namespace DapperVsEntityFramework.Common;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products { get; init; }
    public DbSet<Order> Orders { get; init; }
    public DbSet<OrderItem> OrderItems { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(e =>
        {
            e.ToTable("products");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(p => p.Name).HasColumnName("name").IsRequired();
            e.Property(p => p.Price).HasColumnName("price").HasColumnType("NUMERIC(10,2)");
            e.Property(p => p.Description).HasColumnName("description");
            e.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(o => o.Id);
            e.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
            e.Property(o => o.CustomerId).HasColumnName("customer_id").IsRequired();
            e.Property(o => o.CreationDate).HasColumnName("creation_date").HasColumnType("TIMESTAMPTZ");
        });

        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("order_items");
            e.HasKey(oi => new { oi.OrderId, oi.ProductId });
            e.Property(oi => oi.OrderId).HasColumnName("order_id");
            e.Property(oi => oi.ProductId).HasColumnName("product_id");
            e.Property(oi => oi.Quantity).HasColumnName("quantity");

            e.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId);

            e.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId);
        });
    }
}
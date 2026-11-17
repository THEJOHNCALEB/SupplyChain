using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SupplyChain.Domain.Entities;

namespace SupplyChain.Infrastructure.Persistence;

public class SupplyChainDbContext : DbContext
{
    /// <summary>
    /// Currency is stored as integer kobo (1/100 of a Naira) so SQLite can sort
    /// and compare money exactly without floating point drift.
    /// </summary>
    private static readonly ValueConverter<decimal, long> Money = new(
        value => (long)Math.Round(value * 100m),
        value => value / 100m);

    public SupplyChainDbContext(DbContextOptions<SupplyChainDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<SupplyChainTransfer> Transfers => Set<SupplyChainTransfer>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(64).IsRequired();
            entity.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(32);
            entity.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
            entity.Property(x => x.Balance).HasConversion(Money);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(32);
            entity.Property(x => x.Unit).HasMaxLength(24);
            entity.Property(x => x.Price).HasConversion(Money);
            entity.Property(x => x.CostPrice).HasConversion(Money);
            entity.HasIndex(x => x.Reference);
            entity.HasIndex(x => x.Stage);
            entity.HasIndex(x => x.Category);
            entity.HasOne(x => x.Owner)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SupplyChainTransfer>(entity =>
        {
            entity.ToTable("Transfers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasConversion(Money);
            entity.HasIndex(x => x.ProductId);
            entity.HasOne(x => x.Product)
                .WithMany(x => x.Transfers)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.FromUser)
                .WithMany()
                .HasForeignKey(x => x.FromUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ToUser)
                .WithMany()
                .HasForeignKey(x => x.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.ToTable("CartItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasConversion(Money);
            entity.HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
            entity.HasOne(x => x.User)
                .WithMany(x => x.CartItems)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Product)
                .WithMany()
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reference).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Subtotal).HasConversion(Money);
            entity.Property(x => x.DeliveryFee).HasConversion(Money);
            entity.Property(x => x.Total).HasConversion(Money);
            entity.HasIndex(x => x.Reference).IsUnique();
            entity.HasOne(x => x.Customer)
                .WithMany(x => x.Orders)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("OrderItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UnitPrice).HasConversion(Money);
            entity.Property(x => x.ProductName).HasMaxLength(160);
            entity.Property(x => x.SellerName).HasMaxLength(160);
            entity.HasIndex(x => x.SellerId);
            entity.HasOne(x => x.Order)
                .WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

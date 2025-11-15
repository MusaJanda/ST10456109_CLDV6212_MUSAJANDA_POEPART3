using ABCRetails.Models;
using Microsoft.EntityFrameworkCore;

namespace ABCRetails.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets matching your database schema
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Customer configuration
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Role);

                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Surname).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20);
                entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
                entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("Customer");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Active");
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.OrdersCount).HasDefaultValue(0);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });

            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Products");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.StockAvailable).IsRequired();
                entity.Property(e => e.ImageUrl).HasMaxLength(500);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });

            // ShoppingCart configuration
            modelBuilder.Entity<ShoppingCart>(entity =>
            {
                entity.ToTable("ShoppingCarts");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CustomerId);

                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.CustomerId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.CreatedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.LastModified).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Customer)
                    .WithOne(c => c.ShoppingCart)
                    .HasForeignKey<ShoppingCart>(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CartItem configuration
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CartId);

                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.CartId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.AddedDate).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.ShoppingCart)
                    .WithMany(sc => sc.CartItems)
                    .HasForeignKey(e => e.CartId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany()
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Order configuration
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CustomerId);
                entity.HasIndex(e => e.Status);

                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.CustomerId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.OrderDate).IsRequired();
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Pending");
                entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();
                entity.Property(e => e.CustomerNotes).HasMaxLength(1000);
                entity.Property(e => e.ProcessedBy).HasMaxLength(50);
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(e => e.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // OrderItem configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id).HasMaxLength(50);
                entity.Property(e => e.OrderId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProductId).HasMaxLength(50).IsRequired();
                entity.Property(e => e.ProductName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Quantity).IsRequired();
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();

                entity.HasOne(e => e.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
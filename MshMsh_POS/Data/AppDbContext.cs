using Microsoft.EntityFrameworkCore;
using MshMsh.Domain;

namespace MshMsh.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;
        public DbSet<Customer> Customers { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;
        public DbSet<ReturnTransaction> ReturnTransactions { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=MshMshPosDb;Trusted_Connection=True;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.HasIndex(u => u.Username).IsUnique();
                entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
                entity.Property(u => u.Password).IsRequired().HasMaxLength(100);
                entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<User>().HasData(
                new { Id = 1, Username = "admin", Password = "123", FullName = "مدير النظام", Role = UserRole.Admin },
                new { Id = 2, Username = "cashier", Password = "123", FullName = "كاشير 1", Role = UserRole.Cashier }
            );

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(150);
                entity.Property(p => p.CostPrice).HasPrecision(18, 2);
                entity.Property(p => p.SellingPrice).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Customer>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Phone).IsRequired().HasMaxLength(20);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);
                entity.Property(o => o.Discount).HasPrecision(18, 2);

                entity.HasOne(o => o.Customer)
                      .WithMany()
                      .HasForeignKey(o => o.CustomerId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(o => o.Items)
                      .WithOne()
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(oi => oi.Id);
                entity.Property(oi => oi.UnitCostPrice).HasPrecision(18, 2);
                entity.Property(oi => oi.UnitSellingPrice).HasPrecision(18, 2);

                entity.HasOne(oi => oi.Product)
                      .WithMany()
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ReturnTransaction>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.RefundAmount).HasPrecision(18, 2);

                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Product)
                      .WithMany()
                      .HasForeignKey(r => r.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
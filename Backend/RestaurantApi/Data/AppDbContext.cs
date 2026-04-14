using Microsoft.EntityFrameworkCore;
using RestaurantApi.Models;

namespace RestaurantApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Admin> Admins { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<MenuItem> MenuItems { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // PostgreSQL 原生支援 decimal、text、大型字串，不需要 MySQL 的 longtext/decimal 指定
        modelBuilder.Entity<MenuItem>()
            .Property(m => m.Price)
            .HasColumnType("numeric(10,2)");

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasColumnType("numeric(10,2)");

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.ItemPrice)
            .HasColumnType("numeric(10,2)");

        modelBuilder.Entity<Admin>()
            .Property(a => a.Username)
            .HasMaxLength(50);

        modelBuilder.Entity<Admin>()
            .HasIndex(a => a.Username)
            .IsUnique();

        modelBuilder.Entity<Category>()
            .Property(c => c.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<MenuItem>()
            .Property(m => m.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<Table>()
            .Property(t => t.Name)
            .HasMaxLength(50);

        modelBuilder.Entity<Table>()
            .HasIndex(t => t.TableNumber)
            .IsUnique();

        modelBuilder.Entity<OrderItem>()
            .Property(oi => oi.ItemName)
            .HasMaxLength(100);

        // PostgreSQL 的 OrderStatus enum 儲存為整數
        modelBuilder.Entity<Order>()
            .Property(o => o.Status)
            .HasConversion<int>();
    }
}

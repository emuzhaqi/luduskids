using LudusKids.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LudusKids.Api.Persistence;

public class LudusKidsDbContext : DbContext
{
    public LudusKidsDbContext(DbContextOptions<LudusKidsDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<User> Users => Set<User>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
}

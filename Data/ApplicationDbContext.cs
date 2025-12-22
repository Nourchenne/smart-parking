using auth.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using auth.Models;

namespace auth.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Seed data
            builder.Entity<Product>().HasData(
                new Product
                {
                    Id = 1,
                    Name = "Ordinateur Portable",
                    Description = "Ordinateur portable haute performance",
                    Price = 999.99m,
                    StockQuantity = 50,
                    Category = "Électronique",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Product
                {
                    Id = 2,
                    Name = "Smartphone",
                    Description = "Smartphone dernier cri",
                    Price = 699.99m,
                    StockQuantity = 100,
                    Category = "Électronique",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}
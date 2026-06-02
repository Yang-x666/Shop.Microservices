using Microsoft.EntityFrameworkCore;
using ProductService.Models;
using System;

namespace ProductService.Data;

public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 种子数据：三个示例商品
        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "无线鼠标",
                Description = "人体工学设计，静音按键",
                Price = 79.99m,
                Stock = 100,
                Category = "电子产品"
            },
            new Product
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "机械键盘",
                Description = "青轴，RGB背光",
                Price = 299.99m,
                Stock = 50,
                Category = "电子产品"
            },
            new Product
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "笔记本支架",
                Description = "铝合金可折叠",
                Price = 159.99m,
                Stock = 200,
                Category = "办公用品"
            }
        );
    }
}
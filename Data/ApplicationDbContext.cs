using BeautyCareShop.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Product>().HasIndex(x => x.SKU).IsUnique();
        builder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        builder.Entity<Voucher>().HasIndex(x => x.Code).IsUnique();
        builder.Entity<Product>().Property(x => x.Price).HasPrecision(18, 2);
        builder.Entity<Product>().Property(x => x.SalePrice).HasPrecision(18, 2);
        builder.Entity<Product>().Property(x => x.CostPrice).HasPrecision(18, 2);
        builder.Entity<Order>().Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Entity<Order>().Property(x => x.Discount).HasPrecision(18, 2);
        builder.Entity<Order>().Property(x => x.ShippingFee).HasPrecision(18, 2);
        builder.Entity<Order>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        builder.Entity<OrderItem>().Property(x => x.UnitPrice).HasPrecision(18, 2);
        builder.Entity<OrderItem>().Property(x => x.LineTotal).HasPrecision(18, 2);
        builder.Entity<Voucher>().Property(x => x.DiscountValue).HasPrecision(18, 2);
        builder.Entity<Voucher>().Property(x => x.MinimumOrderValue).HasPrecision(18, 2);
        builder.Entity<Voucher>().Property(x => x.MaximumDiscount).HasPrecision(18, 2);
        builder.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProductImage>().HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProductReview>().HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<OrderItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InventoryTransaction>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

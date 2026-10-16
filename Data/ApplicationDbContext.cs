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

    // Bảng phục vụ phân quyền ma trận
    public DbSet<SystemPermission> SystemPermissions => Set<SystemPermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>(); // Phân quyền theo nhóm vai trò (Role)

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cấu hình Unique Index
        builder.Entity<Product>().HasIndex(x => x.SKU).IsUnique();
        builder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Order>().HasIndex(x => x.OrderCode).IsUnique();
        builder.Entity<Voucher>().HasIndex(x => x.Code).IsUnique();

        // Cấu hình độ chính xác số thập phân (Precision 18, 2)
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

        // Cấu hình quan hệ (Relationships) & Ràng buộc khóa ngoại
        builder.Entity<Product>().HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProductImage>().HasOne(x => x.Product).WithMany(x => x.Images).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<ProductReview>().HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<OrderItem>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InventoryTransaction>().HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);

        // ==========================================
        // CẤU HÌNH HỆ THỐNG PHÂN QUYỀN MA TRẬN
        // ==========================================

        // 1. Khóa chính phức hợp cho bảng quyền theo cá nhân
        builder.Entity<UserPermission>()
            .HasKey(up => new { up.UserId, up.PermissionId });

        // 2. Khóa chính phức hợp cho bảng quyền theo nhóm vai trò (Role)
        builder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // 3. Nạp sẵn danh mục chức năng mặc định
        builder.Entity<SystemPermission>().HasData(
            new SystemPermission { PermissionId = "MANAGE_DASHBOARD", PermissionName = "Xem thống kê Dashboard", ModuleGroup = "Báo cáo" },
            new SystemPermission { PermissionId = "MANAGE_PRODUCTS", PermissionName = "Quản lý sản phẩm", ModuleGroup = "Sản phẩm" },
            new SystemPermission { PermissionId = "MANAGE_CATEGORIES", PermissionName = "Quản lý danh mục", ModuleGroup = "Sản phẩm" },
            new SystemPermission { PermissionId = "MANAGE_INVENTORY", PermissionName = "Quản lý tồn kho", ModuleGroup = "Kho hàng" },
            new SystemPermission { PermissionId = "MANAGE_ORDERS", PermissionName = "Duyệt & Quản lý đơn hàng", ModuleGroup = "Bán hàng" },
            new SystemPermission { PermissionId = "MANAGE_VOUCHERS", PermissionName = "Quản lý mã Voucher", ModuleGroup = "Khuyến mãi" },
            new SystemPermission { PermissionId = "MANAGE_SUPPORT", PermissionName = "Hỗ trợ trực tuyến", ModuleGroup = "CSKH" }
        );
    }
}
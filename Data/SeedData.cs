using BeautyCareShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        string[] roles = ["Owner", "ProductManager", "InventoryManager", "CustomerService", "Customer"];
        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role)) await roleManager.CreateAsync(new IdentityRole(role));

        await EnsureUser(userManager, "tanloc270103@gmail.com", "duylam@#2910", "Chủ sở hữu LUMIPHUX", "Owner");
        await EnsureUser(userManager, "nguyenhoangphuc230406@gmail.com", "Babyimlit@06", "Chủ sở hữu LUMIPHUX", "Owner");
        await EnsureUser(userManager, "abc123@gmail.com", "Abc456@", "Quản lý sản phẩm", "ProductManager");
        await EnsureUser(userManager, "warehouse@lumiphux.com", "Warehouse@123456", "Quản lý kho", "InventoryManager");
        await EnsureUser(userManager, "cskh@lumiphux.com", "Cskh@123456", "CSKH LUMIPHUX", "CustomerService");
        await EnsureUser(userManager, "customer@lumiphux.com", "Customer@123456", "Khách hàng mẫu", "Customer");

        if (!await db.Categories.AnyAsync())
        {
            var categories = new[]
            {
                new Category { Name = "Chăm sóc da", Slug = "cham-soc-da", ImageUrl = "https://images.unsplash.com/photo-1556229010-6c3f2c9a0a6a?auto=format&fit=crop&w=900&q=85" },
                new Category { Name = "Trang điểm", Slug = "trang-diem", ImageUrl = "https://images.unsplash.com/photo-1522335789203-aabd1fc54bc9?auto=format&fit=crop&w=900&q=85" },
                new Category { Name = "Chăm sóc tóc & cơ thể", Slug = "toc-co-the", ImageUrl = "https://images.unsplash.com/photo-1556228578-8c89e6adf883?auto=format&fit=crop&w=900&q=85" },
                new Category { Name = "Vitamin & thực phẩm bổ sung", Slug = "vitamin-thuc-pham-bo-sung", ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?auto=format&fit=crop&w=900&q=85" },
                new Category { Name = "Nam giới", Slug = "nam-gioi", ImageUrl = "https://images.unsplash.com/photo-1517832207067-4db24a2ae47c?auto=format&fit=crop&w=900&q=85" }
            };
            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
        }

        if (!await db.Products.AnyAsync())
        {
            var cat = await db.Categories.ToDictionaryAsync(x => x.Slug);
            var products = new List<Product>
            {
                P("LPX-SER-001", "Serum Vitamin C 10% Glow", "serum-vitamin-c-10-glow", 329000, 279000, 26, cat["cham-soc-da"].Id, "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-CRM-002", "Kem dưỡng phục hồi Skin Barrier", "kem-duong-phuc-hoi-skin-barrier", 459000, 399000, 17, cat["cham-soc-da"].Id, "https://images.unsplash.com/photo-1611930022073-b7a4ba5fcccd?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-CLN-003", "Sữa rửa mặt dịu nhẹ 120ml", "sua-rua-mat-diu-nhe-120ml", 199000, null, 40, cat["cham-soc-da"].Id, "https://images.unsplash.com/photo-1556228720-195a672e8a03?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-SPF-004", "Kem chống nắng Daily Shield SPF50+", "kem-chong-nang-daily-shield-spf50", 389000, 329000, 22, cat["cham-soc-da"].Id, "https://images.unsplash.com/photo-1556228578-0d85b1a4d571?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-TNR-005", "Toner cấp ẩm Hyaluronic", "toner-cap-am-hyaluronic", 249000, 219000, 31, cat["cham-soc-da"].Id, "https://images.unsplash.com/photo-1608248597279-f99d160bfcbc?auto=format&fit=crop&w=900&q=85", false),
                P("LPX-LIP-006", "Son dưỡng môi Rose Glow", "son-duong-moi-rose-glow", 169000, null, 55, cat["trang-diem"].Id, "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?auto=format&fit=crop&w=900&q=85", false),
                P("LPX-SHM-007", "Dầu gội phục hồi tóc hư tổn", "dau-goi-phuc-hoi-toc-hu-ton", 269000, 239000, 24, cat["toc-co-the"].Id, "https://images.unsplash.com/photo-1527799820374-dcf8f7b3f5b4?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-BDY-008", "Body Lotion dưỡng ẩm 24H", "body-lotion-duong-am-24h", 299000, null, 20, cat["toc-co-the"].Id, "https://images.unsplash.com/photo-1601612628452-9e99ced43524?auto=format&fit=crop&w=900&q=85", false),
                P("LPX-VTC-009", "Vitamin C 1000mg", "vitamin-c-1000mg", 249000, 219000, 48, cat["vitamin-thuc-pham-bo-sung"].Id, "https://images.unsplash.com/photo-1607619056574-7b8d3ee536b2?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-COL-010", "Marine Collagen Beauty", "marine-collagen-beauty", 599000, 519000, 15, cat["vitamin-thuc-pham-bo-sung"].Id, "https://images.unsplash.com/photo-1550572017-edd951b55104?auto=format&fit=crop&w=900&q=85", true),
                P("LPX-OMG-011", "Omega 3 Premium", "omega-3-premium", 429000, 379000, 18, cat["vitamin-thuc-pham-bo-sung"].Id, "https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?auto=format&fit=crop&w=900&q=85", false),
                P("LPX-MEN-012", "Gel rửa mặt nam Daily Clean", "gel-rua-mat-nam-daily-clean", 219000, null, 19, cat["nam-gioi"].Id, "https://images.unsplash.com/photo-1621607512214-68297480165e?auto=format&fit=crop&w=900&q=85", true)
            };
            db.Products.AddRange(products);
            await db.SaveChangesAsync();

            foreach (var p in products)
                db.ProductImages.Add(new ProductImage { ProductId = p.Id, Url = p.ThumbnailUrl!, IsPrimary = true });

            db.Vouchers.AddRange(
                new Voucher { Code = "LPX10", DiscountType = "Percentage", DiscountValue = 10, MaximumDiscount = 100000, MinimumOrderValue = 300000, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddMonths(1), UsageLimit = 100 },
                new Voucher { Code = "LPX50K", DiscountType = "FixedAmount", DiscountValue = 50000, MaximumDiscount = 50000, MinimumOrderValue = 500000, StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddMonths(1), UsageLimit = 50 }
            );
            await db.SaveChangesAsync();
        }
    }

    private static Product P(string sku, string name, string slug, decimal price, decimal? sale, int stock, int categoryId, string image, bool featured) => new()
    {
        SKU = sku, Name = name, Slug = slug, Price = price, SalePrice = sale, CostPrice = price * .55m,
        StockQuantity = stock, MinimumStockLevel = 5, CategoryId = categoryId, ThumbnailUrl = image,
        ShortDescription = "Sản phẩm LUMIPHUX dành cho chăm sóc sức khỏe và sắc đẹp mỗi ngày.",
        Description = "Thông tin mô tả sản phẩm mẫu. Khi vận hành thực tế, Product Manager có thể chỉnh sửa nội dung này.",
        Ingredients = "Vui lòng xem bảng thành phần trên bao bì và hướng dẫn của nhà sản xuất.",
        Usage = "Sử dụng theo hướng dẫn trên nhãn sản phẩm.",
        Warnings = "Không sử dụng nếu bạn dị ứng với bất kỳ thành phần nào. Với thực phẩm bổ sung, không dùng thay thế thuốc chữa bệnh.",
        IsFeatured = featured, IsNew = true, IsActive = true
    };

    private static async Task EnsureUser(UserManager<ApplicationUser> userManager, string email, string password, string name, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = name };
            await userManager.CreateAsync(user, password);
        }
        if (!await userManager.IsInRoleAsync(user, role)) await userManager.AddToRoleAsync(user, role);
    }
}

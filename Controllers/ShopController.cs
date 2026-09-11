using BeautyCareShop.Data;
using BeautyCareShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Controllers;

public class ShopController(ApplicationDbContext db, UserManager<ApplicationUser> users) : Controller
{
    public async Task<IActionResult> Index(string? search, string? category, decimal? maxPrice)
    {
        var query = db.Products.Include(x => x.Category).Where(x => x.IsActive && x.DeletedAt == null).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(x => x.Name.Contains(search) || x.SKU.Contains(search));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category!.Slug == category);
        if (maxPrice.HasValue) query = query.Where(x => (x.SalePrice ?? x.Price) <= maxPrice.Value);
        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Categories = await db.Categories.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
        return View(await query.OrderByDescending(x => x.IsFeatured).ThenByDescending(x => x.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(string slug)
    {
        var p = await db.Products.Include(x => x.Category).Include(x => x.Images).Include(x => x.Reviews.Where(r => r.IsApproved)).FirstOrDefaultAsync(x => x.Slug == slug && x.IsActive && x.DeletedAt == null);
        return p == null ? NotFound() : View(p);
    }

    [HttpGet]
    public async Task<IActionResult> Suggestions(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return Json(Array.Empty<object>());
        var items = await db.Products.Where(p => p.IsActive && p.Name.Contains(term)).OrderBy(p => p.Name).Take(6)
            .Select(p => new { p.Name, p.Slug, Price = p.SalePrice ?? p.Price, p.ThumbnailUrl }).ToListAsync();
        return Json(items);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int productId, int rating, string title, string content)
    {
        var uid = users.GetUserId(User);
        if (rating < 1 || rating > 5 || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Đánh giá chưa hợp lệ.";
            return RedirectToAction(nameof(Details), new { slug = await db.Products.Where(x => x.Id == productId).Select(x => x.Slug).FirstOrDefaultAsync() });
        }
        var hasPurchased = await db.OrderItems.AnyAsync(oi => oi.ProductId == productId && oi.Order!.UserId == uid && oi.Order.Status == "Delivered");
        if (!hasPurchased)
        {
            TempData["Error"] = "Bạn chỉ có thể đánh giá sau khi đơn hàng đã giao thành công.";
            var slug = await db.Products.Where(x => x.Id == productId).Select(x => x.Slug).FirstOrDefaultAsync();
            return RedirectToAction(nameof(Details), new { slug });
        }
        db.ProductReviews.Add(new ProductReview { ProductId = productId, UserId = uid, CustomerName = User.Identity?.Name ?? "Khách hàng", Rating = rating, Title = title?.Trim() ?? "", Content = content.Trim(), IsApproved = true });
        await db.SaveChangesAsync();
        var avg = await db.ProductReviews.Where(r => r.ProductId == productId && r.IsApproved).AverageAsync(r => (decimal?)r.Rating) ?? 0m;
        var count = await db.ProductReviews.CountAsync(r => r.ProductId == productId && r.IsApproved);
        var product = await db.Products.FindAsync(productId);
        if (product != null) { product.RatingAverage = avg; product.ReviewCount = count; await db.SaveChangesAsync(); }
        TempData["Success"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
        return RedirectToAction(nameof(Details), new { slug = product!.Slug });
    }
}

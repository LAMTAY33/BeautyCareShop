using BeautyCareShop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles="Owner")]
public class DashboardController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.ProductCount = await db.Products.CountAsync(x=>x.IsActive);
        ViewBag.OrderCount = await db.Orders.CountAsync();
        ViewBag.Revenue = await db.Orders.Where(x=>x.Status != "Cancelled").SumAsync(x=>(decimal?)x.TotalAmount) ?? 0m;
        ViewBag.LowStock = await db.Products.CountAsync(x=>x.StockQuantity <= x.MinimumStockLevel && x.IsActive);
        return View();
    }
}

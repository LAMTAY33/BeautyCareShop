using BeautyCareShop.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Controllers;

public class HomeController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await db.Categories.Where(x => x.IsActive).ToListAsync();
        ViewBag.Featured = await db.Products.Include(x => x.Category).Where(x => x.IsActive && x.IsFeatured).OrderByDescending(x => x.CreatedAt).Take(8).ToListAsync();
        return View();
    }

    public IActionResult Error() => View();
}

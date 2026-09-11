using BeautyCareShop.Data;
using BeautyCareShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Areas.ProductManager.Controllers;

[Area("ProductManager"), Authorize(Roles = "ProductManager,Owner")]
public class ProductsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var q = db.Products.Include(p => p.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) q = q.Where(p => p.Name.Contains(search) || p.SKU.Contains(search));
        ViewBag.Search = search;
        ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(await q.OrderByDescending(p => p.CreatedAt).ToListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(new Product { IsActive = true, MinimumStockLevel = 5, Unit = "Sản phẩm" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product model)
    {
        if (await db.Products.AnyAsync(x => x.SKU == model.SKU)) ModelState.AddModelError(nameof(model.SKU), "SKU đã tồn tại.");
        if (await db.Products.AnyAsync(x => x.Slug == model.Slug)) ModelState.AddModelError(nameof(model.Slug), "Slug đã tồn tại.");
        if (!ModelState.IsValid) { ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync(); return View(model); }
        model.CreatedAt = DateTime.UtcNow; model.UpdatedAt = DateTime.UtcNow;
        db.Products.Add(model); await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync();
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product model)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        if (await db.Products.AnyAsync(x => x.Id != id && x.SKU == model.SKU)) ModelState.AddModelError(nameof(model.SKU), "SKU đã tồn tại.");
        if (await db.Products.AnyAsync(x => x.Id != id && x.Slug == model.Slug)) ModelState.AddModelError(nameof(model.Slug), "Slug đã tồn tại.");
        if (!ModelState.IsValid) { ViewBag.Categories = await db.Categories.OrderBy(c => c.Name).ToListAsync(); return View(model); }

        p.SKU = model.SKU.Trim(); p.Name = model.Name.Trim(); p.Slug = model.Slug.Trim(); p.ShortDescription = model.ShortDescription;
        p.Description = model.Description; p.Ingredients = model.Ingredients; p.Usage = model.Usage; p.Warnings = model.Warnings;
        p.Price = model.Price; p.SalePrice = model.SalePrice; p.CostPrice = model.CostPrice; p.MinimumStockLevel = model.MinimumStockLevel;
        p.Unit = model.Unit; p.Weight = model.Weight; p.ThumbnailUrl = model.ThumbnailUrl; p.IsActive = model.IsActive; p.IsFeatured = model.IsFeatured; p.IsNew = model.IsNew;
        p.CategoryId = model.CategoryId; p.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        p.IsActive = !p.IsActive; p.UpdatedAt = DateTime.UtcNow; await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}

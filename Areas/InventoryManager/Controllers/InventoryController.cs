using BeautyCareShop.Data;
using BeautyCareShop.Models;
using BeautyCareShop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Areas.InventoryManager.Controllers;

[Area("InventoryManager"), Authorize(Roles = "InventoryManager,Owner")]
public class InventoryController(ApplicationDbContext db, IHttpContextAccessor http) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Products.Where(p => p.IsActive).OrderBy(p => p.StockQuantity).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Adjust(InventoryAdjustmentVm model)
    {
        if (!ModelState.IsValid) { TempData["Error"] = "Thông tin điều chỉnh kho chưa hợp lệ."; return RedirectToAction(nameof(Index)); }
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var p = await db.Products.FirstOrDefaultAsync(x => x.Id == model.ProductId);
        if (p is null) return NotFound();
        var before = p.StockQuantity;
        var delta = model.Type == "OUT" ? -model.Quantity : model.Quantity;
        var after = before + delta;
        if (after < 0) { await tx.RollbackAsync(); TempData["Error"] = "Không thể xuất vượt tồn kho."; return RedirectToAction(nameof(Index)); }
        p.StockQuantity = after;
        db.InventoryTransactions.Add(new InventoryTransaction { ProductId = p.Id, Type = model.Type, Quantity = model.Quantity, BeforeStock = before, AfterStock = after, Reason = model.Reason.Trim(), UserId = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value });
        await db.SaveChangesAsync(); await tx.CommitAsync();
        TempData["Success"] = "Đã cập nhật tồn kho.";
        return RedirectToAction(nameof(Index));
    }
}

using BeautyCareShop.Data;
using BeautyCareShop.Models;
using BeautyCareShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrdersController(
    ApplicationDbContext db,
    IEmailService email,
    ILogger<OrdersController> logger) : Controller
{
    // GET: /Admin/Orders
    public async Task<IActionResult> Index(string? status)
    {
        var query = db.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
        }

        var orders = await query.ToListAsync();
        ViewBag.CurrentStatus = status;
        return View(orders);
    }

    // GET: /Admin/Orders/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return View(order);
    }

    // POST: /Admin/Orders/ManualConfirmPayment
    // Dành cho trường hợp Admin kiểm tra app ngân hàng thấy có tiền và bấm duyệt thủ công
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ManualConfirmPayment(int id, string? note)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.PaidAt != null || order.Status == "Cancelled" || order.Status == "PaymentExpired")
        {
            TempData["Error"] = "Đơn hàng này không ở trạng thái chờ thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        var nowUtc = DateTime.UtcNow;
        order.PaidAt = nowUtc;
        order.Status = "Confirmed";
        order.CancellationReason = string.IsNullOrWhiteSpace(note)
            ? "Admin duyệt thanh toán thủ công (đã nhận được tiền)"
            : $"Admin duyệt thủ công: {note.Trim()}";

        await db.SaveChangesAsync();
        await email.SendOrderConfirmationAsync(order);

        TempData["Success"] = $"Đã duyệt thanh toán thành công cho đơn {order.OrderCode}.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    // POST: /Admin/Orders/CancelAndRefundStock
    // Hủy đơn và hoàn tồn kho lại cho shop
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelAndRefundStock(int id, string reason)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        if (order.Status is "Shipping" or "Delivered" or "Cancelled" or "PaymentExpired")
        {
            TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái này.";
            return RedirectToAction(nameof(Index));
        }

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var nowUtc = DateTime.UtcNow;
            order.Status = "Cancelled";
            order.CancelledAt = nowUtc;
            order.CancellationReason = $"Admin hủy đơn: {reason}";

            // Hoàn lại tồn kho
            if (!order.InventoryReleasedAt.HasValue)
            {
                foreach (var item in order.Items)
                {
                    var product = await db.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        var beforeStock = product.StockQuantity;
                        product.StockQuantity += item.Quantity;

                        db.InventoryTransactions.Add(new InventoryTransaction
                        {
                            ProductId = product.Id,
                            Type = "RETURN",
                            Quantity = item.Quantity,
                            BeforeStock = beforeStock,
                            AfterStock = product.StockQuantity,
                            Reason = $"Admin hủy đơn #{order.OrderCode} - {reason}",
                            UserId = order.UserId,
                            CreatedAt = nowUtc
                        });
                    }
                }
                order.InventoryReleasedAt = nowUtc;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = $"Đã hủy đơn {order.OrderCode} và hoàn trả số lượng vào kho.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Lỗi khi admin hủy đơn {OrderId}", id);
            TempData["Error"] = "Không thể hủy đơn hàng lúc này.";
            return RedirectToAction(nameof(Index));
        }
    }
}
using BeautyCareShop.Data;
using BeautyCareShop.Models;
using BeautyCareShop.Services;
using BeautyCareShop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Controllers;

[Authorize]
public class OrdersController(
    ApplicationDbContext db,
    ICartService cart,
    UserManager<ApplicationUser> users,
    IPaymentService payment,
    IEmailService email,
    ILogger<OrdersController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var uid = users.GetUserId(User);
        var orders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == uid)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var items = await cart.GetAsync();
        if (items.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        return View(new CheckoutVm());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutVm model)
    {
        var items = await cart.GetAsync();
        if (items.Count == 0)
        {
            TempData["Error"] = "Giỏ hàng đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        var allowedMethods = new[] { "COD", "QR", "Card" };
        if (!allowedMethods.Contains(model.PaymentMethod))
            ModelState.AddModelError(nameof(model.PaymentMethod), "Phương thức thanh toán không hợp lệ.");

        var allowedProviders = new[] { "VietQR", "MoMo", "ZaloPay", "ShopeePay" };
        if (model.PaymentMethod == "QR" && !allowedProviders.Contains(model.PaymentProvider))
            ModelState.AddModelError(nameof(model.PaymentProvider), "Nhà cung cấp QR không hợp lệ.");

        foreach (var item in items)
        {
            if (item.Product is null || item.Quantity <= 0 || item.Quantity > item.Product.StockQuantity)
                ModelState.AddModelError(string.Empty, $"Sản phẩm {item.Product?.Name ?? "không xác định"} không đủ tồn kho.");
        }

        var subtotal = items.Sum(x => x.LineTotal);
        var discount = 0m;
        Voucher? usedVoucher = null;

        if (!string.IsNullOrWhiteSpace(model.VoucherCode))
        {
            usedVoucher = await db.Vouchers.FirstOrDefaultAsync(v =>
                v.Code == model.VoucherCode.Trim().ToUpper() &&
                v.IsActive &&
                v.StartDate <= DateTime.UtcNow &&
                v.EndDate >= DateTime.UtcNow &&
                v.UsedCount < v.UsageLimit);

            if (usedVoucher is null || subtotal < usedVoucher.MinimumOrderValue)
                ModelState.AddModelError(nameof(model.VoucherCode), "Voucher không hợp lệ hoặc chưa đủ điều kiện.");
            else
                discount = usedVoucher.DiscountType == "Percentage"
                    ? Math.Min(subtotal * usedVoucher.DiscountValue / 100m, usedVoucher.MaximumDiscount)
                    : Math.Min(usedVoucher.DiscountValue, subtotal);
        }

        if (!ModelState.IsValid)
            return View(model);

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            // Reload current stock inside the transaction to reduce overselling races.
            foreach (var item in items)
            {
                var current = await db.Products.FirstAsync(p => p.Id == item.ProductId);
                if (!current.IsActive || current.DeletedAt != null || current.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"Sản phẩm {current.Name} vừa thay đổi tồn kho. Vui lòng kiểm tra lại giỏ hàng.");
                    await tx.RollbackAsync();
                    return View(model);
                }
            }

            var order = new Order
            {
                OrderCode = $"LPX{DateTime.UtcNow:yyMMddHHmmssfff}{Random.Shared.Next(10, 99)}",
                UserId = users.GetUserId(User),
                CustomerName = model.CustomerName.Trim(),
                Phone = model.Phone.Trim(),
                ShippingAddress = model.ShippingAddress.Trim(),
                PaymentMethod = model.PaymentMethod,
                Status = model.PaymentMethod == "COD" ? "Confirmed" : "Pending",
                Subtotal = subtotal,
                Discount = discount,
                ShippingFee = subtotal - discount >= 500000 ? 0 : 30000,
                CreatedAt = DateTime.UtcNow,
                PaymentDeadline = model.PaymentMethod == "COD" ? null : DateTime.UtcNow.AddMinutes(10)
            };

            order.TotalAmount = order.Subtotal - order.Discount + order.ShippingFee;

            foreach (var item in items)
            {
                var p = await db.Products.FirstAsync(x => x.Id == item.ProductId);
                var unit = p.SalePrice ?? p.Price;
                var before = p.StockQuantity;
                p.StockQuantity -= item.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = unit,
                    Quantity = item.Quantity,
                    LineTotal = unit * item.Quantity
                });

                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = p.Id,
                    Type = "OUT",
                    Quantity = item.Quantity,
                    BeforeStock = before,
                    AfterStock = p.StockQuantity,
                    Reason = "Đặt đơn " + order.OrderCode,
                    UserId = order.UserId
                });
            }

            db.Orders.Add(order);
            if (usedVoucher is not null) usedVoucher.UsedCount++;

            await db.SaveChangesAsync();
            await tx.CommitAsync();
            await cart.ClearAsync();

            if (model.PaymentMethod == "COD")
            {
                await email.SendOrderConfirmationAsync(order);
                TempData["PaymentSuccess"] = $"Đặt hàng {order.OrderCode} thành công. Phương thức COD.";
                return RedirectToAction("Success", new { id = order.Id });
            }

            return RedirectToAction(nameof(Payment), new { id = order.Id, provider = model.PaymentMethod == "QR" ? model.PaymentProvider : "Card" });
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Checkout failed");
            ModelState.AddModelError(string.Empty, "Không thể tạo đơn hàng lúc này. Vui lòng thử lại.");
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Payment(int id, string? provider = null)
    {
        var uid = users.GetUserId(User);
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);
        if (order is null) return NotFound();

        if (order.PaidAt is null && order.PaymentDeadline.HasValue && DateTime.UtcNow >= order.PaymentDeadline.Value)
        {
            await ExpireAndReleaseInventoryAsync(order);
            TempData["Error"] = "Đơn hàng đã hết thời gian thanh toán 10 phút.";
            return RedirectToAction(nameof(Index));
        }

        if (order.PaymentMethod == "COD")
            return RedirectToAction(nameof(Success), new { id = order.Id });

        var selectedProvider = provider ?? (order.PaymentMethod == "QR" ? "VietQR" : "Card");
        ViewBag.PaymentProvider = selectedProvider;
        ViewBag.QrUrl = order.PaymentMethod == "QR" ? payment.BuildQrUrl(order.OrderCode, order.TotalAmount, selectedProvider) : null;
        ViewBag.DeepLink = order.PaymentMethod == "QR" ? payment.GetDeepLink(order.OrderCode, order.TotalAmount, selectedProvider) : null;
        ViewBag.Deadline = order.PaymentDeadline?.ToString("o");
        return View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PayByCard(CardPaymentVm model)
    {
        var uid = users.GetUserId(User);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == model.OrderId && o.UserId == uid);
        if (order is null) return NotFound();

        if (order.PaymentDeadline.HasValue && DateTime.UtcNow >= order.PaymentDeadline.Value)
        {
            await ExpireAndReleaseInventoryAsync(order);
            TempData["Error"] = "Đã hết 10 phút thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        if (!payment.ValidateDemoCard(model.CardNumber))
        {
            ModelState.AddModelError(nameof(model.CardNumber), "Số thẻ demo không hợp lệ. Ví dụ: 4242 4242 4242 4242");
            ViewBag.Deadline = order.PaymentDeadline?.ToString("o");
            return View("Payment", order);
        }

        order.PaidAt = DateTime.UtcNow;
        order.Status = "Confirmed";
        await db.SaveChangesAsync();
        await email.SendOrderConfirmationAsync(order);

        TempData["PaymentSuccess"] = $"Thanh toán thẻ cho đơn {order.OrderCode} thành công!";
        return RedirectToAction(nameof(Success), new { id = order.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmQrPayment(int id, string provider = "VietQR")
    {
        var uid = users.GetUserId(User);
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);
        if (order is null) return NotFound();

        if (order.PaymentDeadline.HasValue && DateTime.UtcNow >= order.PaymentDeadline.Value)
        {
            await ExpireAndReleaseInventoryAsync(order);
            TempData["Error"] = "Đã hết thời gian thanh toán QR.";
            return RedirectToAction(nameof(Index));
        }

        order.PaidAt = DateTime.UtcNow;
        order.Status = "Confirmed";
        await db.SaveChangesAsync();
        await email.SendOrderConfirmationAsync(order);

        TempData["PaymentSuccess"] = $"Thanh toán QR ({provider}) cho đơn {order.OrderCode} thành công!";
        return RedirectToAction(nameof(Success), new { id = order.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Success(int id)
    {
        var uid = users.GetUserId(User);
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string reason, string? otherReason)
    {
        var uid = users.GetUserId(User);
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);
        if (order is null) return NotFound();

        if (order.PaidAt is not null || order.Status is "Shipping" or "Delivered" or "Returned" or "Cancelled")
        {
            TempData["Error"] = "Đơn hàng hiện không thể hủy.";
            return RedirectToAction(nameof(Index));
        }

        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            order.Status = "Cancelled";
            order.CancelledAt = DateTime.UtcNow;
            order.CancellationReason = reason == "Lý do khác" && !string.IsNullOrWhiteSpace(otherReason)
                ? otherReason.Trim()
                : reason;

            ReleaseOrderInventory(order, "Hủy đơn " + order.OrderCode);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            TempData["Success"] = "Đơn hàng đã được hủy và tồn kho đã được hoàn lại.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            logger.LogError(ex, "Cancel order failed for {OrderId}", id);
            TempData["Error"] = "Không thể hủy đơn hàng lúc này.";
            return RedirectToAction(nameof(Index));
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var uid = users.GetUserId(User);
        var order = await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id && o.UserId == uid);
        return order is null ? NotFound() : View(order);
    }

    private async Task ExpireAndReleaseInventoryAsync(Order order)
    {
        if (order.PaidAt is not null || order.InventoryReleasedAt.HasValue) return;
        order.Status = "PaymentExpired";
        ReleaseOrderInventory(order, "Hết hạn thanh toán đơn " + order.OrderCode);
        await db.SaveChangesAsync();
    }

    private void ReleaseOrderInventory(Order order, string reason)
    {
        if (order.InventoryReleasedAt.HasValue) return;

        foreach (var item in order.Items)
        {
            var product = db.Products.Find(item.ProductId);
            if (product is null) continue;

            var before = product.StockQuantity;
            product.StockQuantity += item.Quantity;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                Type = "RETURN",
                Quantity = item.Quantity,
                BeforeStock = before,
                AfterStock = product.StockQuantity,
                Reason = reason,
                UserId = order.UserId
            });
        }

        order.InventoryReleasedAt = DateTime.UtcNow;
    }
}

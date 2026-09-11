using BeautyCareShop.Data;
using BeautyCareShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Services;

public record AssistantResult(string Reply, string? ActionUrl = null);

public interface IAssistantService
{
    Task<AssistantResult> AskAsync(string message);
}

public class SiteAssistantService(
    ApplicationDbContext db,
    ICartService cart,
    UserManager<ApplicationUser> users,
    IHttpContextAccessor http) : IAssistantService
{
    public async Task<AssistantResult> AskAsync(string message)
    {
        message = (message ?? string.Empty).Trim();
        if (message.Length == 0) return new("Bạn hãy nhập yêu cầu nhé.");

        var lower = message.ToLowerInvariant();
        var products = await db.Products.Include(p => p.Category).Where(p => p.IsActive && p.DeletedAt == null).ToListAsync();
        var terms = lower.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var match = products
            .Select(p => new { Product = p, Score = terms.Count(t => t.Length >= 3 && (p.Name + " " + p.SKU + " " + p.Category!.Name).ToLowerInvariant().Contains(t)) })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Product.Name)
            .Select(x => x.Product)
            .FirstOrDefault();

        if (lower.Contains("hết hàng") || lower.Contains("còn hàng") || lower.Contains("tồn kho"))
        {
            var low = products.Where(p => p.StockQuantity <= p.MinimumStockLevel).OrderBy(p => p.StockQuantity).Take(5).ToList();
            if (low.Count == 0) return new("Kho hiện đang ổn định, chưa có sản phẩm ở mức cảnh báo.", "/Shop");
            return new("Các sản phẩm sắp hết: " + string.Join("; ", low.Select(p => $"{p.Name} ({p.StockQuantity})")), "/Shop");
        }

        if ((lower.Contains("thêm") || lower.Contains("mua")) && match != null)
        {
            try
            {
                await cart.AddAsync(match.Id, 1);
                return new($"Đã thêm {match.Name} vào giỏ hàng. Bạn có muốn mở giỏ để kiểm tra không?", "/Cart");
            }
            catch (InvalidOperationException ex)
            {
                return new(ex.Message, $"/Shop/Details?slug={Uri.EscapeDataString(match.Slug)}");
            }
        }

        if (lower.Contains("xóa") || lower.Contains("bỏ khỏi giỏ"))
        {
            var items = await cart.GetAsync();
            var item = match != null ? items.FirstOrDefault(i => i.ProductId == match.Id) : items.FirstOrDefault();
            if (item?.Product != null)
            {
                await cart.RemoveAsync(item.ProductId);
                return new($"Đã xóa {item.Product.Name} khỏi giỏ hàng.", "/Cart");
            }
            return new("Giỏ hàng chưa có sản phẩm phù hợp để xóa.", "/Cart");
        }

        if (lower.Contains("giỏ hàng") || lower.Contains("cart"))
        {
            var items = await cart.GetAsync();
            if (items.Count == 0) return new("Giỏ hàng đang trống.", "/Shop");
            return new($"Giỏ hàng có {items.Sum(i => i.Quantity)} sản phẩm, tạm tính {items.Sum(i => i.LineTotal):N0}₫.", "/Cart");
        }

        if (lower.Contains("thanh toán") || lower.Contains("checkout"))
            return new("Mình mở trang thanh toán cho bạn. QR/thẻ được giữ tồn kho trong 10 phút.", "/Orders/Checkout");

        if (lower.Contains("hủy đơn") || lower.Contains("cancel order"))
        {
            var uid = users.GetUserId(http.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal());
            if (uid == null) return new("Bạn cần đăng nhập để mình hỗ trợ hủy đơn.", "/Account/Login");
            var order = await db.Orders.Where(o => o.UserId == uid && o.PaidAt == null && (o.Status == "Pending" || o.Status == "Confirmed"))
                .OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            return order == null ? new("Mình không tìm thấy đơn chưa thanh toán có thể hủy.", "/Orders") : new($"Đơn gần nhất có thể hủy là {order.OrderCode}. Hãy mở đơn hàng để chọn lý do hủy.", $"/Orders/Details/{order.Id}");
        }

        if (lower.Contains("đơn hàng") || lower.Contains("order"))
        {
            var uid = users.GetUserId(http.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal());
            if (uid == null) return new("Bạn cần đăng nhập để mình tra cứu đơn hàng.", "/Account/Login");
            var order = await db.Orders.Where(o => o.UserId == uid).OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            return order == null ? new("Bạn chưa có đơn hàng nào.", "/Orders") : new($"Đơn gần nhất: {order.OrderCode} — trạng thái {order.Status} — tổng {order.TotalAmount:N0}₫.", $"/Orders/Details/{order.Id}");
        }

        if (match != null)
            return new($"Mình tìm thấy {match.Name}, giá {(match.SalePrice ?? match.Price):N0}₫, tồn kho {match.StockQuantity}.", $"/Shop/Details?slug={Uri.EscapeDataString(match.Slug)}");

        if (lower.Contains("tìm") || lower.Contains("sản phẩm") || lower.Contains("serum") || lower.Contains("vitamin") || lower.Contains("collagen") || lower.Contains("kem"))
            return new("Mình có thể giúp bạn tìm serum, kem dưỡng, chống nắng, vitamin, collagen, hair care… Bạn hãy nói tên hoặc nhu cầu cụ thể.", "/Shop");

        return new("Mình là trợ lý LUMIPHUX. Mình có thể tìm sản phẩm, kiểm tra tồn kho, thêm/xóa giỏ hàng, mở checkout, tra cứu đơn và hướng dẫn hủy đơn. Bạn cũng có thể chuyển sang CSKH người thật.");
    }
}

using BeautyCareShop.Data;
using BeautyCareShop.Hubs;
using BeautyCareShop.Models;
using BeautyCareShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentWebhookController(
    ApplicationDbContext db,
    IEmailService email,
    IHubContext<SupportHub> hubContext,
    ILogger<PaymentWebhookController> logger,
    IConfiguration config) : ControllerBase
{
    // Model nhận dữ liệu biến động số dư (Chuẩn SePay / Webhook ngân hàng thông dụng)
    public class BankWebhookPayload
    {
        public long Id { get; set; }
        public string? Gateway { get; set; }
        public DateTime TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public decimal TransferAmount { get; set; }
        public string? Content { get; set; } // Nội dung chuyển khoản (chứa mã đơn ví dụ: LPX2610...)
        public string? ReferenceCode { get; set; }
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveBankTransaction([FromBody] BankWebhookPayload data)
    {
        // 1. Kiểm tra API Key / Token xác thực từ Header để chống giả mạo
        var apiKeyHeader = Request.Headers["Authorization"].ToString();
        var secureKey = config["Payment:WebhookApiKey"] ?? "LPX_SECRET_KEY_2026";

        if (!string.IsNullOrEmpty(secureKey) && !apiKeyHeader.Contains(secureKey))
        {
            logger.LogWarning("Webhook bị từ chối: Sai API Key.");
            return Unauthorized(new { success = false, message = "Invalid API Key" });
        }

        if (string.IsNullOrWhiteSpace(data.Content) || data.TransferAmount <= 0)
            return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ" });

        logger.LogInformation("Nhận biến động số dư: +{Amount} VNĐ, Nội dung: {Content}", data.TransferAmount, data.Content);

        // 2. Tìm đơn hàng có mã OrderCode xuất hiện trong nội dung chuyển khoản
        var pendingOrders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.PaidAt == null && o.Status == "Pending")
            .ToListAsync();

        var matchedOrder = pendingOrders.FirstOrDefault(o =>
            data.Content.Contains(o.OrderCode, StringComparison.OrdinalIgnoreCase));

        if (matchedOrder == null)
        {
            logger.LogInformation("Không tìm thấy đơn hàng khớp với nội dung: {Content}", data.Content);
            return Ok(new { success = true, message = "Đã nhận nhưng không khớp đơn" });
        }

        // 3. Kiểm tra số tiền chuyển có đủ không
        if (data.TransferAmount < matchedOrder.TotalAmount)
        {
            logger.LogWarning("Đơn #{OrderCode} chuyển thiếu tiền: Cần {Required}, thực nhận {Received}",
                matchedOrder.OrderCode, matchedOrder.TotalAmount, data.TransferAmount);
            return Ok(new { success = true, message = "Thiếu tiền thanh toán" });
        }

        // 4. Khớp lệnh thành công -> Chuyển trạng thái
        var nowUtc = DateTime.UtcNow;
        matchedOrder.PaidAt = nowUtc;
        matchedOrder.Status = "Confirmed";

        await db.SaveChangesAsync();

        // Gửi email xác nhận
        await email.SendOrderConfirmationAsync(matchedOrder);

        // 5. Bắn thông báo thời gian thực qua SignalR về trình duyệt của khách hàng
        await hubContext.Clients.All.SendAsync("OrderPaidSuccess", new
        {
            orderId = matchedOrder.Id,
            orderCode = matchedOrder.OrderCode
        });

        logger.LogInformation("Xác nhận thanh toán tự động thành công cho đơn #{OrderCode}", matchedOrder.OrderCode);
        return Ok(new { success = true, message = "Xác nhận thành công" });
    }
}
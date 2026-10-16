using BeautyCareShop.Data;
using BeautyCareShop.Models;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Services;

public class OrderExpirationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderExpirationWorker> _logger;

    public OrderExpirationWorker(IServiceProvider serviceProvider, ILogger<OrderExpirationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpirationWorker đã khởi động.");

        // Quét định kỳ mỗi 1 phút
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReleaseExpiredOrdersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi quét và hoàn kho đơn hàng hết hạn.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ReleaseExpiredOrdersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var nowUtc = DateTime.UtcNow;

        // Lấy các đơn hàng QR/Card chưa thanh toán và đã quá hạn thanh toán
        var expiredOrders = await db.Orders
            .Include(o => o.Items)
            .Where(o => o.Status == "Pending"
                     && o.PaidAt == null
                     && o.InventoryReleasedAt == null
                     && o.PaymentDeadline.HasValue
                     && o.PaymentDeadline.Value <= nowUtc)
            .ToListAsync();

        if (expiredOrders.Count == 0) return;

        foreach (var order in expiredOrders)
        {
            order.Status = "PaymentExpired";
            order.CancelledAt = nowUtc;
            order.CancellationReason = "Hết hạn thanh toán (tự động hoàn kho)";
            order.InventoryReleasedAt = nowUtc;

            foreach (var item in order.Items)
            {
                var product = await db.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    var beforeStock = product.StockQuantity;
                    product.StockQuantity += item.Quantity; // Hoàn trả tồn kho

                    db.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = product.Id,
                        Type = "RETURN",
                        Quantity = item.Quantity,
                        BeforeStock = beforeStock,
                        AfterStock = product.StockQuantity,
                        Reason = $"Hoàn kho tự động - Hết hạn thanh toán đơn #{order.OrderCode}",
                        UserId = order.UserId,
                        CreatedAt = nowUtc
                    });
                }
            }
        }

        await db.SaveChangesAsync();
        _logger.LogInformation("Đã hoàn kho tự động cho {Count} đơn hàng hết hạn thanh toán.", expiredOrders.Count);
    }
}
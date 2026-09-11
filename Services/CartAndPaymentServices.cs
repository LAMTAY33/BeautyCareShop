using System.Text.Json;
using BeautyCareShop.Data;
using BeautyCareShop.Models;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Services;

public interface ICartService
{
    Task<List<CartItem>> GetAsync();
    Task AddAsync(int productId, int quantity = 1);
    Task RemoveAsync(int productId);
    Task UpdateAsync(int productId, int quantity);
    Task ClearAsync();
}

public class SessionCartService(IHttpContextAccessor accessor, ApplicationDbContext db) : ICartService
{
    private const string Key = "LPX_CART";
    private ISession Session => accessor.HttpContext!.Session;

    public async Task<List<CartItem>> GetAsync()
    {
        var ids = JsonSerializer.Deserialize<Dictionary<int, int>>(Session.GetString(Key) ?? "{}") ?? [];
        if (ids.Count == 0) return [];

        var products = await db.Products
            .Include(p => p.Category)
            .Where(p => ids.Keys.Contains(p.Id) && p.IsActive && p.DeletedAt == null)
            .ToListAsync();

        foreach (var stale in ids.Keys.Except(products.Select(p => p.Id)).ToList())
            ids.Remove(stale);

        Session.SetString(Key, JsonSerializer.Serialize(ids));

        return products
            .Select(p => new CartItem
            {
                Id = p.Id,
                ProductId = p.Id,
                Product = p,
                Quantity = Math.Min(Math.Max(ids[p.Id], 1), p.StockQuantity)
            })
            .Where(x => x.Quantity > 0)
            .ToList();
    }

    public async Task AddAsync(int productId, int quantity = 1)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId && x.IsActive && x.DeletedAt == null)
            ?? throw new InvalidOperationException("Không tìm thấy sản phẩm.");

        if (p.StockQuantity <= 0)
            throw new InvalidOperationException("Sản phẩm hiện đã hết hàng.");

        quantity = Math.Max(1, quantity);
        var ids = JsonSerializer.Deserialize<Dictionary<int, int>>(Session.GetString(Key) ?? "{}") ?? [];
        ids.TryGetValue(productId, out var old);
        var next = old + quantity;

        if (next > p.StockQuantity)
            throw new InvalidOperationException($"Số lượng tối đa hiện có là {p.StockQuantity}.");

        ids[productId] = next;
        Session.SetString(Key, JsonSerializer.Serialize(ids));
    }

    public Task RemoveAsync(int productId)
    {
        var ids = JsonSerializer.Deserialize<Dictionary<int, int>>(Session.GetString(Key) ?? "{}") ?? [];
        ids.Remove(productId);
        Session.SetString(Key, JsonSerializer.Serialize(ids));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(int productId, int quantity)
    {
        var p = await db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId && x.IsActive && x.DeletedAt == null);
        if (p is null) throw new InvalidOperationException("Không tìm thấy sản phẩm.");
        var ids = JsonSerializer.Deserialize<Dictionary<int, int>>(Session.GetString(Key) ?? "{}") ?? [];
        if (!ids.ContainsKey(productId)) return;
        if (quantity <= 0) { ids.Remove(productId); }
        else { if (quantity > p.StockQuantity) throw new InvalidOperationException($"Chỉ còn {p.StockQuantity} sản phẩm."); ids[productId] = quantity; }
        Session.SetString(Key, JsonSerializer.Serialize(ids));
    }


    public Task ClearAsync()
    {
        Session.Remove(Key);
        return Task.CompletedTask;
    }
}

public interface IPaymentService
{
    string BuildQrUrl(string orderCode, decimal amount, string provider);
    string GetDeepLink(string orderCode, decimal amount, string provider);
    bool ValidateDemoCard(string cardNumber);
}

public sealed class DemoPaymentService(IConfiguration configuration) : IPaymentService
{
    public string BuildQrUrl(string orderCode, decimal amount, string provider)
    {
        var accountName = configuration["Payment:QrAccountName"] ?? "LUMIPHUX";
        var accountNo = configuration["Payment:QrAccountNumber"] ?? "0123456789";
        var bank = configuration["Payment:QrBankBin"] ?? "970436";
        var addInfo = Uri.EscapeDataString($"{orderCode} LUMIPHUX");
        var encodedName = Uri.EscapeDataString(accountName);

        // VietQR image API-style URL. Provider is retained in the payload for demo UX.
        return $"https://img.vietqr.io/image/{bank}-{accountNo}-compact2.png?amount={amount:0}&addInfo={addInfo}&accountName={encodedName}&provider={Uri.EscapeDataString(provider)}";
    }

    public string GetDeepLink(string orderCode, decimal amount, string provider)
    {
        var payload = Uri.EscapeDataString($"{orderCode}|{amount:0}|LUMIPHUX");
        return provider switch
        {
            "MoMo" => $"momo://app?action=transfer&note={payload}&amount={amount:0}",
            "ZaloPay" => $"zalopay://app/transfer?amount={amount:0}&note={payload}",
            "ShopeePay" => $"shopeepay://payment?amount={amount:0}&note={payload}",
            _ => $"bankapp://transfer?amount={amount:0}&note={payload}"
        };
    }

    public bool ValidateDemoCard(string cardNumber)
    {
        var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 13 || digits.Length > 19) return false;
        var sum = 0;
        var alternate = false;
        for (var i = digits.Length - 1; i >= 0; i--)
        {
            var n = digits[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }
        return sum % 10 == 0;
    }
}

public interface IEmailService
{
    Task SendOrderConfirmationAsync(Order order);
}

public class LoggingEmailService(ILogger<LoggingEmailService> logger) : IEmailService
{
    public Task SendOrderConfirmationAsync(Order order)
    {
        logger.LogInformation("LUMIPHUX demo email confirmation: order={OrderCode}, total={Total}, phone={Phone}", order.OrderCode, order.TotalAmount, order.Phone);
        return Task.CompletedTask;
    }
}

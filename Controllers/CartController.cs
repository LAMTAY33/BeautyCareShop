using BeautyCareShop.Services;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCareShop.Controllers;

public class CartController(ICartService cart) : Controller
{
    public async Task<IActionResult> Index() => View(await cart.GetAsync());

    [HttpGet]
    public async Task<IActionResult> Count()
    {
        var items = await cart.GetAsync();
        return Json(new { count = items.Sum(x => x.Quantity), total = items.Sum(x => x.LineTotal) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        try
        {
            await cart.AddAsync(productId, quantity);
            var items = await cart.GetAsync();
            if (Request.Headers.XRequestedWith == "XMLHttpRequest")
                return Json(new { ok = true, count = items.Sum(x => x.Quantity), message = "Đã thêm sản phẩm vào giỏ hàng." });
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            if (Request.Headers.XRequestedWith == "XMLHttpRequest") return BadRequest(new { ok = false, message = ex.Message });
            TempData["Error"] = ex.Message;
            return RedirectToAction("Index", "Shop");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        try { await cart.UpdateAsync(id, quantity); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        await cart.RemoveAsync(id);
        return RedirectToAction(nameof(Index));
    }
}

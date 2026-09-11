using BeautyCareShop.Services;
using BeautyCareShop.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCareShop.Controllers;

public class ChatController(IAssistantService assistant) : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(ChatVm model)
    {
        if (!ModelState.IsValid) return BadRequest(new { reply = "Nội dung chat không hợp lệ." });
        var result = await assistant.AskAsync(model.Message);
        return Json(result);
    }
}

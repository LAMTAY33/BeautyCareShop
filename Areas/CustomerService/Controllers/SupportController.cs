using BeautyCareShop.Data;
using BeautyCareShop.Services;
using BeautyCareShop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BeautyCareShop.Areas.CustomerService.Controllers;

[Area("CustomerService"), Authorize(Roles="CustomerService,Owner")]
public class SupportController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.SupportMessages.OrderByDescending(x=>x.CreatedAt).Take(100).ToListAsync());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(ChatVm model){ if(!string.IsNullOrWhiteSpace(model.Message)){db.SupportMessages.Add(new BeautyCareShop.Models.SupportMessage{SenderType="Human",Message=model.Message}); await db.SaveChangesAsync();} return RedirectToAction(nameof(Index)); }
}

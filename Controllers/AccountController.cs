using BeautyCareShop.Models;
using BeautyCareShop.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BeautyCareShop.Controllers;

public class AccountController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn) : Controller
{
    [HttpGet] public IActionResult Login(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await signIn.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
        if (result.Succeeded) return LocalRedirect(returnUrl ?? "/");
        ModelState.AddModelError("", "Email hoặc mật khẩu không đúng."); return View(model);
    }

    [HttpGet] public IActionResult Register() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = new ApplicationUser { UserName = model.Email, Email = model.Email, FullName = model.FullName, EmailConfirmed = true };
        var result = await users.CreateAsync(user, model.Password);
        if (result.Succeeded) { await users.AddToRoleAsync(user, "Customer"); await signIn.SignInAsync(user, false); return RedirectToAction("Index", "Home"); }
        foreach (var e in result.Errors) ModelState.AddModelError("", e.Description); return View(model);
    }

    [HttpGet] public IActionResult ForgotPassword() => View();
    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(string email)
    {
        TempData["Success"] = "Nếu email tồn tại, hướng dẫn đặt lại mật khẩu sẽ được gửi. Bản demo chưa kết nối SMTP.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpPost, ValidateAntiForgeryToken] public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction("Index", "Home"); }
    public IActionResult AccessDenied() => View();
}

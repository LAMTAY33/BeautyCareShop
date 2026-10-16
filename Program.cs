using BeautyCareShop.Data;
using BeautyCareShop.Hubs;
using BeautyCareShop.Models;
using BeautyCareShop.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(4);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<ICartService, SessionCartService>();
builder.Services.AddScoped<IAssistantService, SiteAssistantService>();
builder.Services.AddScoped<IPaymentService, DemoPaymentService>();
builder.Services.AddScoped<IEmailService, LoggingEmailService>();

// Đăng ký dịch vụ chạy ngầm tự động quét và hoàn tồn kho khi hết hạn thanh toán
builder.Services.AddHostedService<OrderExpirationWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();

    // Tự động kiểm tra và bổ sung các cột cần thiết cho Orders nếu chưa có
    await db.Database.ExecuteSqlRawAsync(@"
        IF COL_LENGTH('Orders', 'InventoryReleasedAt') IS NULL ALTER TABLE Orders ADD InventoryReleasedAt datetime2 NULL;
        IF COL_LENGTH('Orders', 'PaymentDeadline') IS NULL ALTER TABLE Orders ADD PaymentDeadline datetime2 NULL;
        IF COL_LENGTH('Orders', 'PaidAt') IS NULL ALTER TABLE Orders ADD PaidAt datetime2 NULL;
        IF COL_LENGTH('Orders', 'CancelledAt') IS NULL ALTER TABLE Orders ADD CancelledAt datetime2 NULL;
        IF COL_LENGTH('Orders', 'CancellationReason') IS NULL ALTER TABLE Orders ADD CancellationReason nvarchar(500) NULL;
    ");

    await SeedData.InitializeAsync(services);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapHub<SupportHub>("/hubs/support");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
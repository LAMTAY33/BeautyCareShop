using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BeautyCareShop.Data;
using BeautyCareShop.Models;
using BeautyCareShop.Models.ViewModels;

namespace BeautyCareShop.Controllers
{
    public class PermissionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public PermissionController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        private async Task<bool> IsSuperAdminAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return false;

            // Nhóm Owner luôn có toàn quyền
            if (await _userManager.IsInRoleAsync(currentUser, "Owner")) return true;

            // Email SuperAdmin chỉ định
            if (string.Equals(currentUser.Email, "tanloc270103@gmail.com", StringComparison.OrdinalIgnoreCase)) return true;

            var secretAdminEmail = _configuration["SuperAdmin:Email"];
            if (!string.IsNullOrEmpty(secretAdminEmail) &&
                string.Equals(currentUser.Email, secretAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(currentUser.Email, "superadmin@lumiphux.com", StringComparison.OrdinalIgnoreCase);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!await IsSuperAdminAsync()) return Forbid();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "permission.html");
            return PhysicalFile(filePath, "text/html");
        }

        // ==========================================
        // 1. PHÂN QUYỀN THEO TỪNG CÁ NHÂN (USER)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetMatrixData()
        {
            if (!await IsSuperAdminAsync()) return Forbid();

            var currentUser = await _userManager.GetUserAsync(User);

            var users = await _userManager.Users
                .Where(u => currentUser == null || u.Id != currentUser.Id)
                .Select(u => new { id = u.Id, userName = u.UserName, email = u.Email })
                .ToListAsync();

            var permissions = await _context.SystemPermissions
                .OrderBy(p => p.ModuleGroup)
                .ToListAsync();

            var userPermissions = await _context.UserPermissions.ToListAsync();

            return Json(new { users, permissions, userPermissions });
        }

        [HttpPost]
        public async Task<IActionResult> TogglePermission([FromBody] TogglePermissionRequest req)
        {
            if (!await IsSuperAdminAsync()) return Unauthorized();

            var currentPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == req.UserId && up.PermissionId == req.PermissionId);

            if (currentPermission == null)
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = req.UserId,
                    PermissionId = req.PermissionId,
                    IsGranted = req.IsGranted
                });
            }
            else
            {
                currentPermission.IsGranted = req.IsGranted;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ==========================================
        // 2. PHÂN QUYỀN THEO NHÓM VAI TRÒ (ROLE)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetRoleMatrixData()
        {
            if (!await IsSuperAdminAsync()) return Forbid();

            var roles = await _roleManager.Roles
                .Select(r => new { id = r.Id, name = r.Name })
                .ToListAsync();

            var permissions = await _context.SystemPermissions
                .OrderBy(p => p.ModuleGroup)
                .ToListAsync();

            var rolePermissions = await _context.RolePermissions
                .Select(rp => new
                {
                    roleId = rp.RoleId,
                    permissionId = rp.PermissionId,
                    isGranted = rp.IsGranted
                })
                .ToListAsync();

            return Json(new { roles, permissions, rolePermissions });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleRolePermission([FromBody] ToggleRolePermissionRequest req)
        {
            if (!await IsSuperAdminAsync()) return Unauthorized();

            if (req == null || string.IsNullOrEmpty(req.RoleId) || string.IsNullOrEmpty(req.PermissionId))
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            var item = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == req.RoleId && rp.PermissionId == req.PermissionId);

            if (item == null)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = req.RoleId,
                    PermissionId = req.PermissionId,
                    IsGranted = req.IsGranted
                });
            }
            else
            {
                item.IsGranted = req.IsGranted;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}
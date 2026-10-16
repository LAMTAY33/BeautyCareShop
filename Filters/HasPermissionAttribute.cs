using BeautyCareShop.Data;
using BeautyCareShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BeautyCareShop.Filters
{
    public class HasPermissionAttribute : TypeFilterAttribute
    {
        public HasPermissionAttribute(string permissionId) : base(typeof(HasPermissionFilter))
        {
            Arguments = new object[] { permissionId };
        }
    }

    public class HasPermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permissionId;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public HasPermissionFilter(
            string permissionId,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _permissionId = permissionId;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                context.Result = new ChallengeResult();
                return;
            }

            // 1. SuperAdmin cố định hoặc nhóm Owner luôn có toàn quyền
            var email = user.FindFirstValue(ClaimTypes.Email);
            if (string.Equals(email, "tanloc270103@gmail.com", StringComparison.OrdinalIgnoreCase) || user.IsInRole("Owner"))
            {
                return;
            }

            var appUser = await _userManager.GetUserAsync(user);
            if (appUser == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            // 2. Lấy danh sách ID các Role (Nhóm) mà tài khoản này tham gia
            var userRoles = await _userManager.GetRolesAsync(appUser);
            var roleIds = await _roleManager.Roles
                .Where(r => userRoles.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync();

            // 3. Kiểm tra xem bất kỳ Nhóm nào của user có quyền này hay không
            var hasRolePermission = await _context.RolePermissions
                .AnyAsync(rp => roleIds.Contains(rp.RoleId) && rp.PermissionId == _permissionId && rp.IsGranted);

            if (hasRolePermission)
            {
                return; // Cho phép truy cập theo quyền của nhóm
            }

            // 4. (Dự phòng) Kiểm tra quyền cấp riêng theo cá nhân
            var hasUserPermission = await _context.UserPermissions
                .AnyAsync(up => up.UserId == appUser.Id && up.PermissionId == _permissionId && up.IsGranted);

            if (hasUserPermission)
            {
                return;
            }

            // Nếu không thuộc nhóm nào có quyền -> Chặn truy cập
            context.Result = new ForbidResult();
        }
    }
}
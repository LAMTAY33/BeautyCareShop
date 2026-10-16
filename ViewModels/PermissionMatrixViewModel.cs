using Microsoft.AspNetCore.Identity;
using BeautyCareShop.Models;

namespace BeautyCareShop.Models.ViewModels
{
    public class PermissionMatrixViewModel
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public List<SystemPermission> Permissions { get; set; } = new();
        public List<UserPermission> UserPermissions { get; set; } = new();
    }

    public class TogglePermissionRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string PermissionId { get; set; } = string.Empty;
        public bool IsGranted { get; set; }
    }
}
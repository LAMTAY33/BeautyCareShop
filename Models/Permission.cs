using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeautyCareShop.Models
{
    public class SystemPermission
    {
        [Key]
        public string PermissionId { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string ModuleGroup { get; set; } = string.Empty;
    }

    public class UserPermission
    {
        public string UserId { get; set; } = string.Empty;
        public string PermissionId { get; set; } = string.Empty;
        public bool IsGranted { get; set; }

        public virtual SystemPermission? Permission { get; set; }
    }

    // Model phân quyền theo Nhóm / Vai trò (Role)
    public class RolePermission
    {
        public string RoleId { get; set; } = string.Empty;
        public string PermissionId { get; set; } = string.Empty;
        public bool IsGranted { get; set; }

        [ForeignKey("PermissionId")]
        public virtual SystemPermission? Permission { get; set; }
    }

    // DTO nhận dữ liệu khi click toggle quyền của Role từ giao diện
    public class ToggleRolePermissionRequest
    {
        public string RoleId { get; set; } = string.Empty;
        public string PermissionId { get; set; } = string.Empty;
        public bool IsGranted { get; set; }
    }
}